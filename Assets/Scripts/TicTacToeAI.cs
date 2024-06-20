using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[Flags]
public enum TicTacToeState { none = 0, cross = 1, circle = 2, all = 3 }

[System.Serializable]
public class WinnerEvent : UnityEvent<int>{}

public class TicTacToeAI : MonoBehaviour
{
	public UnityEvent onGameStarted;

	public WinnerEvent onPlayerWin;


	[SerializeField]
	private bool _isPlayerTurn;

	[SerializeField]
	private TicTacToeState _aiState = TicTacToeState.circle;

	[SerializeField]
	private TicTacToeState _playerState = TicTacToeState.cross;

	[SerializeField]
	private int _gridSize = 3;

	[SerializeField]
	private GameObject _xPrefab;

	[SerializeField]
	private GameObject _oPrefab;


	private string _aiMethod = "AIEasyMove";

	private TicTacToeState[,] _boardState;

	private int _counters = 0;

	private ClickTrigger[,] _triggers;

	private void Awake()
	{
		if (onPlayerWin == null)
		{
			onPlayerWin = new WinnerEvent();
		}
	}

	public void RegisterTransform(int row, int col, ClickTrigger clickTrigger)
	{
		_triggers[row, col] = clickTrigger;
	}

	public void StartAI(int AILevel)
	{
		_aiMethod = AILevel == 0 ? "AIEasyMove" : "AIHardMove";

		InitializeBoard();
		StartGame();
	}

	private void InitializeBoard()
	{
		_boardState = new TicTacToeState[_gridSize, _gridSize];

		for (int row = 0; row < _gridSize; row++)
		{
			for (int col = 0; col < _gridSize; col++)
			{
				_boardState[row, col] = TicTacToeState.none;
			}
		}
	}

	private void StartGame()
	{
		_triggers = new ClickTrigger[3, 3];
		onGameStarted.Invoke();
	}

	private void AIEasyMove()
	{
		AIRandomMove();
	}

	private void AIHardMove()
	{
		Minimax(0, _boardState, _aiState);
	}

	private void AIRandomMove()
	{
		while (!PlayPiece(UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(0, 3), _aiState)) {}
		CheckWin();
		_isPlayerTurn = true;
	}

	private int Minimax(int depth, TicTacToeState[,] board, TicTacToeState turn)
	{
		List<int> scores = new List<int>(_gridSize*_gridSize);
		List<int> moves = new List<int>(_gridSize*_gridSize);

		int score = MinimaxScore(board);
		if(score != 0) return score; // No-one has won yet, or it's a draw

		for(int row = 0; row < _gridSize; row++) {
			for(int col = 0; col < _gridSize; col++) {
				if(board[row,col] == TicTacToeState.none) {
					board[row,col] = turn;
					scores.Add(Minimax(depth+1, board, turn == _aiState ? _playerState : _aiState));
					moves.Add((row*_gridSize)+col); // store row,col as index
					board[row,col] = TicTacToeState.none;
				}
			}
		}

		// Time to make a move
		if(depth == 0) {
			int moveIndex = scores.IndexOf(scores.Max());
			PlayPiece(moves[moveIndex]/_gridSize, moves[moveIndex]%_gridSize, _aiState);
			CheckWin();
			_isPlayerTurn = true;
			return 0;
		// Draw - end game
		} else if(scores.Count == 0) {
			return 0;
		} else if(turn == _aiState) {
			return scores.Max();
		} else {
			return scores.Min();
		}
	}

	private int MinimaxScore(TicTacToeState[,] board)
	{
		TicTacToeState[] winlines = new TicTacToeState[(_gridSize*2)+2];

		// Initialize
		for(int i = 0; i < (_gridSize*2)+2; i++) winlines[i] = TicTacToeState.all;

		// Do some bin ops
		// Essentially if a win line has the same items its value would be aiState or playerState or 0
		for(int i = 0; i < _gridSize; i++) {
			for(int j = 0; j < _gridSize; j++) {
				winlines[i] &= board[i,j]; // row
				winlines[i+_gridSize] &= board[j,i]; // column
				if(i == j) winlines[_gridSize*2] &= board[i,j]; // major diagonal
				if(i+j == _gridSize-1) winlines[(_gridSize*2)+1] &= board[i,j]; // minor diagonal
			}
		}

		// Winner would be ai, player or no-one
		TicTacToeState winner = winlines.Max();
		if(winner == _aiState) return 10;
		else if(winner == _playerState) return -10;
		else return 0;
	}

	public void PlayerSelects(int row, int col)
	{
		if(_isPlayerTurn) {
			if (PlayPiece(row, col, _playerState))
			{
				_isPlayerTurn = false;
				if(!CheckWin()) {
					// Add some faux thinking time
					Invoke(_aiMethod, 1.0f);
				}
			}
		}
	}

	public void AiSelects(int row, int col)
	{
		SetVisual(row, col, _aiState);
	}

	private bool PlayPiece(int row, int col, TicTacToeState targetState)
	{
		bool isValidMove = IsValidMove(row, col);
		if(isValidMove) {
			SetVisual(row, col, targetState);
		}
		return isValidMove;
	}

	private bool CheckWin()
	{
		bool ended = false;

		// Reuse minmaxscore to determine if board is won
		int score = MinimaxScore(_boardState);
		if(score == 10) {
			ended = true;
			onPlayerWin.Invoke(1);
		} else if(score == -10) {
			ended = true;
			onPlayerWin.Invoke(0);
		} else if(AllPiecesPlayed()) {
			ended = true;
			onPlayerWin.Invoke(-1);
		}

		return ended;
	}

	private void SetVisual(int row, int col, TicTacToeState targetState)
	{
		_boardState[row, col] = targetState;
		Instantiate(
			targetState == TicTacToeState.circle ? _oPrefab : _xPrefab,
			_triggers[row, col].transform.position,
			Quaternion.identity
		);
	}

	private bool IsValidMove(int row, int col)
	{
		return _boardState[row, col] == TicTacToeState.none;
	}

	private bool AllPiecesPlayed()
	{
		return ++_counters == _gridSize * _gridSize;
	}
}
