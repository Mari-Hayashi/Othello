using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Disk { Black, White, Empty, MAX }
public enum HorizontalDirection { Right, Left, None, MAX }
public enum VerticalDirection { Up, Down, None, MAX }

public class BoardState
{
    public Disk[][] board;
    public Disk enemyColor;
    public Disk myColor;

    public BoardState(Disk[][] board, Disk myColor)
    {
        this.board = board;
        this.myColor = myColor;
        enemyColor = (myColor == Disk.Black) ? Disk.White : Disk.Black;
    }

    // Returns true if the player can place a disk at the coordinate (x, y).
    public bool CanPlaceDisk(int x, int y)
    {
        if (board[x][y] != Disk.Empty) return false;
        
        // Check if the placing disk at (x, y) flips other disks for any of the 8 directions.
        for (HorizontalDirection horizontal = 0; horizontal < HorizontalDirection.MAX; ++horizontal)
        {
            for (VerticalDirection vertical = 0; vertical < VerticalDirection.MAX; ++vertical)
            {
                if (CanFlipDisk(x, y, horizontal, vertical)) return true;
            }
        }
        return false;
    }

    // Place disk at the coordinate (x, y) and returns the board with disks placed and flipped accordingly.
    public Disk[][] PlaceDisk(int x, int y)
    {
        Disk[][] flippedBoard = new Disk[board.Length][];
        for (int i = 0; i < board.Length; ++i)
        {
            flippedBoard[i] = new Disk[board.Length];
            for (int j = 0; j < board.Length; ++j)
            {
                flippedBoard[i][j] = board[i][j];
            }
        }
        
        flippedBoard[x][y] = myColor;

        for (HorizontalDirection horizontal = 0; horizontal < HorizontalDirection.MAX; ++horizontal)
        {
            for (VerticalDirection vertical = 0; vertical < VerticalDirection.MAX; ++vertical)
            {
                flippedBoard = Flip(x, y, horizontal, vertical, flippedBoard);
            }
        }

        return flippedBoard;
    }

    // Returns a board that is the most optimal to the player.
    // NumLevels is the depth of iterative deepening.
    public Disk[][] GetNextOptimalBoard(int numLevels)
    {
        List<BoardState> nextStates = GetNextStates();

        if (nextStates.Count == 0) return null;
        BoardState bestState = nextStates[0];

        if (myColor == Disk.Black) // Return the state with the maximum score.
        {
            float bestScore = float.MinValue;
            foreach (BoardState state in nextStates)
            {
                float currentScore = state.GetScore(numLevels, int.MaxValue, bestScore);
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestState = state;
                }
            }
        }
        else // Return the state with the minimum score.
        {
            float bestScore = float.MaxValue;
            foreach (BoardState state in nextStates)
            {
                float currentScore = state.GetScore(numLevels, bestScore, int.MinValue);
                if (currentScore < bestScore)
                {
                    bestScore = currentScore;
                    bestState = state;
                }
            }
        }
        return bestState.board;
    }

    // Returns true if the board is at a terminating state.
    // (Either the board is full or no one can make move.)
    public bool TerminateBoard()
    {
        if (CanMakeMove()) return false;
        if (new BoardState(board, enemyColor).CanMakeMove()) return false;

        return true;
    }

    // Get score of the current state recursively.
    public float GetScore(int level, float betaValue, float alphaValue)
    {
        if (TerminateBoard()) // Terminal State. Board is filled.
        {
            float finalScore = UtilityFunction();
            if (finalScore > 0) return int.MaxValue;
            else if (finalScore < 0) return int.MinValue;
            else return 0;
        };
        
        List<BoardState> nextStates = GetNextStates();

        if (level == 0) return EvaluationFunction();

        if (myColor == Disk.Black) // Return maximum score
        {
            float maximumScore = float.MinValue;
            foreach (BoardState board in nextStates)
            {
                maximumScore = Mathf.Max(maximumScore, board.GetScore(level - 1, betaValue, alphaValue));
                if (maximumScore <= alphaValue || maximumScore >= betaValue) break;
                alphaValue = maximumScore;
            }

            return maximumScore;
        }
        else // Return minimum score
        {
            float minimumScore = float.MaxValue;
            foreach (BoardState board in nextStates)
            {
                minimumScore = Mathf.Min(minimumScore, board.GetScore(level - 1, betaValue, alphaValue));
                if (minimumScore <= alphaValue || minimumScore >= betaValue) break;
                betaValue = minimumScore;
            }

            return minimumScore;
        }
    }

    // Returns true if a player can make move.
    public bool CanMakeMove()
    {
        for (int x = 0; x < board.Length; ++x)
        {
            for (int y = 0; y < board.Length; ++y)
            {
                if (CanPlaceDisk(x, y))
                {
                    return true;
                }
            }
        }
        return false;
    }
    #region ------ Private Helpers ------
    // Returns the list of all next possible states.
    private List<BoardState> GetNextStates()
    {
        List<BoardState> nextStates = new List<BoardState>();

        if (CanMakeMove())
        {
            for (int x = 0; x < board.Length; ++x)
            {
                for (int y = 0; y < board.Length; ++y)
                {
                    if (CanPlaceDisk(x, y))
                    {
                        Disk[][] nextState = PlaceDisk(x, y);
                        nextStates.Add(new BoardState(nextState, enemyColor));
                    }
                }
            }
        }
        else
        {
            BoardState oppositeState = new BoardState(board, enemyColor);
            nextStates.Add(oppositeState);
        }
        
        return nextStates;
    }

    private float UtilityFunction()
    {
        return EvaluationFunction();
    }

    // Evaluation function is calculated as: Number of black disk - Number of white disk.
    protected float EvaluationFunction()
    {
        Dictionary<Disk, int> numDisks = new Dictionary<Disk, int>();
        for (Disk cell = 0; cell < Disk.MAX; ++cell)
        {
            numDisks[cell] = 0;
        }

        foreach (Disk[] row in board)
        {
            foreach (Disk cell in row)
            {
                numDisks[cell]++;
            }
        }

        return numDisks[Disk.Black] - numDisks[Disk.White];
    }

    // Returns true if the placing a disk at the coordinate (x, y) flips any other disks in the direction
    // specified in the argument.
    private bool CanFlipDisk(int x, int y, HorizontalDirection horizontal, VerticalDirection vertical)
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
                case VerticalDirection.Down: yCoordinate = y + i; break;
                case VerticalDirection.Up: yCoordinate = y - i; break;
                case VerticalDirection.None: yCoordinate = y; break;
            }

            if (xCoordinate < 0 || xCoordinate >= board.Length) break;
            if (yCoordinate < 0 || yCoordinate >= board.Length) break;

            if (board[xCoordinate][yCoordinate] == Disk.Empty) return false;
            if (board[xCoordinate][yCoordinate] == enemyColor) continue;
            if (board[xCoordinate][yCoordinate] == myColor)
            {
                if (i == 1) return false;
                else return true;
            }
        }
        return false;
    }

    // Modify the board by placing a disk at the coordinate (x, y) and flipping other disks in the
    // direction specified in the argument.
    private Disk[][] Flip(int x, int y, 
        HorizontalDirection horizontal, VerticalDirection vertical, Disk[][] currentBoard)
    {
        if (!CanFlipDisk(x, y, horizontal, vertical)) return currentBoard;
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
                case VerticalDirection.Down: yCoordinate = y + i; break;
                case VerticalDirection.Up: yCoordinate = y - i; break;
                case VerticalDirection.None: yCoordinate = y; break;
            }

            if (xCoordinate < 0 || xCoordinate >= board.Length) break;
            if (yCoordinate < 0 || yCoordinate >= board.Length) break;
            
            if (currentBoard[xCoordinate][yCoordinate] == enemyColor) currentBoard[xCoordinate][yCoordinate] = myColor;
            else if (currentBoard[xCoordinate][yCoordinate] == myColor)
            {
                return currentBoard;
            }
        }
        return currentBoard;
    }
    #endregion
}