using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BoardState
{
    public CellState[][] board;
    public CellState enemyColor;
    public CellState myColor;

    public BoardState(CellState[][] board)
    {
        this.board = board;
    }

    public int GetScore()
    {
        int NumBlack = 0;
        int NumWhite = 0;

        bool TerminateState = true;

        foreach (CellState[] row in board)
        {
            foreach (CellState cell in row)
            {
                if (cell == CellState.Empty) TerminateState = false;
                else if (cell == CellState.Black) NumBlack++;
                else if (cell == CellState.White) NumWhite++;
            }
        }

        if (TerminateState)
        {
            if (NumBlack > NumWhite) return int.MaxValue;
            else if (NumBlack < NumWhite) return int.MinValue;
            else return 0; // Tie
        }

        return NumBlack - NumWhite;
    }

    public bool CanFlip(int x, int y)
    {
        if (board[x][y] != CellState.Empty) return false;

        for (HorizontalDirection horizontal = 0; horizontal < HorizontalDirection.MAX; ++horizontal)
        {
            for (VerticalDirection vertical = 0; vertical < VerticalDirection.MAX; ++vertical)
            {
                if (CanFlip(x, y, horizontal, vertical)) return true;
            }
        }

        return false;
    }

    public CellState[][] Flip(int x, int y)
    {
        CellState[][] currentBoard = board;
        currentBoard[x][y] = myColor;

        for (HorizontalDirection horizontal = 0; horizontal < HorizontalDirection.MAX; ++horizontal)
        {
            for (VerticalDirection vertical = 0; vertical < VerticalDirection.MAX; ++vertical)
            {
                currentBoard = Flip(x, y, horizontal, vertical, currentBoard);
            }
        }

        return currentBoard;
    }

    #region ------ Private Helpers ------
    private bool CanFlip(int x, int y, HorizontalDirection horizontal, VerticalDirection vertical)
    {
        if (horizontal == HorizontalDirection.None && vertical == VerticalDirection.None) return false;

        for (int i = 1; ; ++i)
        {
            int xCoordinate = 0;
            switch (horizontal)
            {
                case HorizontalDirection.Right: xCoordinate = x + i; break;
                case HorizontalDirection.Left: xCoordinate = x - i; break;
                case HorizontalDirection.None: xCoordinate = x; break;
            }

            int yCoordinate = 0;
            switch (vertical)
            {
                case VerticalDirection.Down: xCoordinate = y + i; break;
                case VerticalDirection.Up: xCoordinate = y - i; break;
                case VerticalDirection.None: xCoordinate = y; break;
            }

            if (xCoordinate < 0 || xCoordinate >= board.Length) break;
            if (yCoordinate < 0 || yCoordinate >= board.Length) break;

            if (board[xCoordinate][yCoordinate] == CellState.Empty) return false;
            if (board[xCoordinate][yCoordinate] == enemyColor) continue;
            if (board[xCoordinate][yCoordinate] == myColor)
            {
                if (i == 1) return false;
                else return true;
            }
        }
        return false;
    }

    private CellState[][] Flip(int x, int y, 
        HorizontalDirection horizontal, VerticalDirection vertical, CellState[][] currentBoard)
    {
        if (!CanFlip(x, y, horizontal, vertical)) return currentBoard;
        if (horizontal == HorizontalDirection.None && vertical == VerticalDirection.None) return currentBoard;

        for (int i = 1; ; ++i)
        {
            int xCoordinate = 0;
            switch (horizontal)
            {
                case HorizontalDirection.Right: xCoordinate = x + i; break;
                case HorizontalDirection.Left: xCoordinate = x - i; break;
                case HorizontalDirection.None: xCoordinate = x; break;
            }

            int yCoordinate = 0;
            switch (vertical)
            {
                case VerticalDirection.Down: xCoordinate = y + i; break;
                case VerticalDirection.Up: xCoordinate = y - i; break;
                case VerticalDirection.None: xCoordinate = y; break;
            }

            if (xCoordinate < 0 || xCoordinate >= board.Length) break;
            if (yCoordinate < 0 || yCoordinate >= board.Length) break;
            
            if (currentBoard[xCoordinate][yCoordinate] == enemyColor) currentBoard[xCoordinate][yCoordinate] = myColor;
            if (currentBoard[xCoordinate][yCoordinate] == myColor)
            {
                return currentBoard;
            }
        }
        return currentBoard;
    }
    #endregion
}

public class BoardNextBlack : BoardState
{
    public BoardNextBlack(CellState[][] board) : base(board)
    {
        enemyColor = CellState.White;
        myColor = CellState.Black;
    }

    public List<BoardNextWhite> GetNextStates()
    {
        List<BoardNextWhite> nextStates = new List<BoardNextWhite>();

        for (int x = 0; x < board.Length; ++x)
        {
            for (int y = 0; y < board.Length; ++y)
            {
                if (CanFlip(x, y))
                {
                    CellState[][] nextState = Flip(x, y);
                    nextStates.Add(new BoardNextWhite(nextState));
                }
            }
        }

        return nextStates;
    }
}

public class BoardNextWhite : BoardState
{
    public BoardNextWhite(CellState[][] board) : base(board)
    {
        enemyColor = CellState.Black;
        myColor = CellState.White;
    }

    public List<BoardNextBlack> GetNextStates()
    {
        List<BoardNextBlack> nextStates = new List<BoardNextBlack>();

        for (int x = 0; x < board.Length; ++x)
        {
            for (int y = 0; y < board.Length; ++y)
            {
                if (CanFlip(x, y))
                {
                    CellState[][] nextState = Flip(x, y);
                    nextStates.Add(new BoardNextBlack(nextState));
                }
            }
        }

        return nextStates;
    }
}
