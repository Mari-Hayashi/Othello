using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

enum State { Menu, PlayerTurn, AITurn, Notify, End, MAX }
public class UI : MonoBehaviour
{
    [Header("Transform")]
    [SerializeField]
    private RectTransform boardRectTransform = null;

    [Header("Canvas")]
    [SerializeField]
    private Canvas canvas = null;

    [Header("Sounds")]
    [SerializeField]
    private AudioClip button;
    [SerializeField]
    private AudioClip place;
    [SerializeField]
    private AudioClip notificationSound;

    [Header("UIs")]
    [SerializeField]
    private GameObject notification;
    [SerializeField]
    private Text notificationText;
    [SerializeField]
    private Animator notificationAnimator;
    [SerializeField]
    private Text topLabel;
    [SerializeField]
    private GameObject menu;
    [SerializeField]
    private GameObject endMenu;
    [SerializeField]
    private Text endMenuText;
    [SerializeField]
    private Sprite disk;

    private Rect adjustedRect;
    private float boardAreaSize;

    private const float diskSizeRatio = 0.6f;
    private const int level = 20;
    private const float timeTakenForAI = 0.5f;
    private const float timeNotification = 0.5f;

    private State currentState;
    private BoardState currentBoard;

    private Image[][] boardImage;

    private int boardSize;
    private AudioSource audioSource;
    private bool clickDisabled = false;

    Disk playerColor;
    Disk AIColor;

    private void Start()
    {
        currentState = State.Menu;

        adjustedRect = RectTransformUtility.PixelAdjustRect(boardRectTransform, canvas);
        boardAreaSize = adjustedRect.height;
        audioSource = gameObject.AddComponent<AudioSource>();
        
        Disk[][] initialBoard = new Disk[][] {
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Black, Disk.White, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.White, Disk.Black, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty},
            new Disk[]{ Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty, Disk.Empty}};

        boardSize = initialBoard.Length;

        CreateBoard();
        PrintBoard(initialBoard);

        currentBoard = new BoardState(initialBoard, Disk.Black);
        notification.SetActive(false);
    }

    private void StartGame()
    {
        if (playerColor == Disk.Black)
        {
            currentState = State.PlayerTurn;
            PlayerTurn();
        }
        else
        {
            currentState = State.AITurn;
            StartCoroutine(AITurn());
        }
    }

    // Create a board game object.
    private void CreateBoard()
    {
        boardImage = new Image[boardSize][];
        for (int i = 0; i < boardSize; ++i)
        {
            boardImage[i] = new Image[boardSize];
            for (int j = 0; j < boardSize; ++j)
            {
                GameObject newCell = new GameObject($"Cell ({i},{j})");

                RectTransform cellRectTransform = newCell.AddComponent<RectTransform>();
                cellRectTransform.SetParent(boardRectTransform);
                cellRectTransform.anchorMin = new Vector2((i + 0.5f) / boardSize, (j + 0.5f) / boardSize);
                cellRectTransform.anchorMax = new Vector2((i + 0.5f) / boardSize, (j + 0.5f) / boardSize);
                cellRectTransform.anchoredPosition = Vector3.zero;
                cellRectTransform.localScale = Vector3.one;
                cellRectTransform.sizeDelta = new Vector2(boardAreaSize / boardSize,
                    boardAreaSize / boardSize);

                Button cellButton = newCell.AddComponent<Button>();
                int xCoordinate = i;
                int yCoordinate = j;
                cellButton.onClick.AddListener(
                    delegate
                    {
                        ButtonClick(xCoordinate, yCoordinate);
                    }
                );

                GameObject cellChild = new GameObject("Image");

                Image cellImage = cellChild.AddComponent<Image>();
                cellImage.sprite = disk;
                boardImage[i][j] = cellImage;

                RectTransform cellChildRectTransform = cellChild.GetComponent<RectTransform>();
                cellChildRectTransform.SetParent(newCell.transform, false);
                cellChildRectTransform.anchoredPosition = Vector3.zero;
                cellChildRectTransform.localScale = Vector3.one;
                cellChildRectTransform.sizeDelta = new Vector2(boardAreaSize / boardSize * diskSizeRatio, boardAreaSize / boardSize * diskSizeRatio);
            }
        }
    }

    // Print the board in the argument on the screen.
    private void PrintBoard(Disk[][] board)
    {
        for (int i = 0; i < boardSize; ++i)
        {
            for (int j = 0; j < boardSize; ++j)
            {
                boardImage[i][j].color = GetColor(board[i][j]);
            }
        }
        audioSource.PlayOneShot(place);
    }

    private void GameEnd()
    {
        audioSource.PlayOneShot(notificationSound);
        endMenu.SetActive(true);
        currentState = State.End;

        float score = currentBoard.GetScore(0, 0, 0);
        Disk winner = score > 0 ? Disk.Black : Disk.White;
        string message = (playerColor == winner) ? "Player won!" : "AI won!";
        
        endMenuText.text = message;
    }

    private void PlayerTurn()
    {
        currentState = State.PlayerTurn;
        topLabel.text = "Waiting for: Player";

        if (!currentBoard.CanMakeMove())
        {
            StartCoroutine(Notify("Skipping player's turn."));
            currentBoard = new BoardState(currentBoard.board, AIColor);
            currentState = State.AITurn;
            StartCoroutine(AITurn());
        }
    }

    IEnumerator AITurn()
    {
        currentState = State.AITurn;
        topLabel.text = "Waiting for: AI";
        yield return new WaitForSeconds(timeTakenForAI);

        if (!currentBoard.CanMakeMove())
        {
            StartCoroutine(Notify("Skipping AI's turn."));
            currentBoard = new BoardState(currentBoard.board, playerColor);
            PlayerTurn();
        }
        else
        {
            currentBoard = new BoardState(currentBoard.GetNextOptimalBoard(level), playerColor);
            PrintBoard(currentBoard.board);
            if (currentBoard.TerminateBoard()) GameEnd();
            else PlayerTurn();
        }
    }

    IEnumerator Notify(string message)
    {
        notification.SetActive(true);
        audioSource.PlayOneShot(notificationSound);
        notificationText.text = message;
        notificationAnimator.SetTrigger("Notify");
        clickDisabled = true;
        yield return new WaitForSeconds(timeNotification);
        notification.SetActive(false);
        clickDisabled = false;
    }

    public void MenuButtonPressed(int color)
    {
        audioSource.PlayOneShot(button);
        if (color == 0)
        {
            playerColor = Disk.Black;
            AIColor = Disk.White;
        }
        else
        {
            playerColor = Disk.White;
            AIColor = Disk.Black;
        }

        menu.SetActive(false);
        StartGame();
    }

    public void PlayAgain()
    {
        audioSource.PlayOneShot(button);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ButtonClick(int x, int y)
    {
        if (currentState == State.PlayerTurn && !clickDisabled)
        {
            if (currentBoard.CanPlaceDisk(x, y))
            {
                currentBoard = new BoardState(currentBoard.PlaceDisk(x, y), AIColor);
                PrintBoard(currentBoard.board);

                if (currentBoard.TerminateBoard())
                {
                    GameEnd();
                }
                else
                {
                    StartCoroutine(AITurn());
                }
            }
            else
            {
                StartCoroutine(Notify("You cannot place the disk here."));
            }
        }
    }
    
    private Color GetColor(Disk cell)
    {
        switch (cell)
        {
            case Disk.Black:
                return new Color(0f, 0f, 0f);
            case Disk.White:
                return new Color(1f, 1f, 1f);
            case Disk.Empty:
                return new Color(0f, 0f, 0f, 0f);
        }
        return new Color(1f, 0f, 0f);
    }
}
