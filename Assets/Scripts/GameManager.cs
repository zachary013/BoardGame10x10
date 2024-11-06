using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public const int BOARD_SIZE = 10;
    public const int MAX_VALUE = 64;

    public GameObject cellPrefab;
    public Transform boardParent;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameStatusText;
    public Button resetButton;

    private Cell[,] board;
    private int currentValue = 1;
    private Vector2Int? currentPosition;
    private int score = 0;
    private bool gameStarted = false;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();

    // Special numbers that get bonus multipliers
    private readonly HashSet<int> specialNumbers = new HashSet<int> { 2, 4, 8, 16, 32, 64 };
    
    // Scoring system data
    private Dictionary<int, float> extraPercentages = new Dictionary<int, float>();
    private Dictionary<int, float> specialBonusPercentages = new Dictionary<int, float>()
    {
        {2, 64f}, {4, 256f}, {8, 512f}, {16, 1024f}, {32, 2048f}, {64, 64000f}
    };

    void Start()
    {
        InitializeExtraPercentages();
        
        if (cellPrefab == null || boardParent == null || scoreText == null || 
            gameStatusText == null || resetButton == null)
        {
            Debug.LogError("Missing references in GameManager. Please check the Inspector.");
            return;
        }

        InitializeBoard();
        resetButton.onClick.AddListener(ResetGame);
        UpdateUI();
    }

    void InitializeExtraPercentages()
    {
        // Initialize extra percentages for each number (1-64)
        for (int i = 1; i <= MAX_VALUE; i++)
        {
            extraPercentages[i] = i / 100f; // Convert to decimal (e.g., 1% = 0.01)
        }
    }

    void InitializeBoard()
    {
        foreach (Transform child in boardParent)
        {
            Destroy(child.gameObject);
        }

        board = new Cell[BOARD_SIZE, BOARD_SIZE];
        
        RectTransform boardRect = boardParent.GetComponent<RectTransform>();
        float cellSize = Mathf.Min(boardRect.rect.width, boardRect.rect.height) / BOARD_SIZE;
        
        var gridLayout = boardParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            gridLayout.spacing = new Vector2(5, 5);
        }

        for (int row = 0; row < BOARD_SIZE; row++)
        {
            for (int col = 0; col < BOARD_SIZE; col++)
            {
                GameObject cellObj = Instantiate(cellPrefab, boardParent);
                Cell cell = cellObj.GetComponent<Cell>();
                if (cell != null)
                {
                    cell.Initialize(row, col, OnCellClicked);
                    board[row, col] = cell;
                }
                else
                {
                    Debug.LogError($"Cell script missing on prefab at position [{row}, {col}]");
                }
            }
        }
    }

    void OnCellClicked(int row, int col)
    {
        if (!gameStarted)
        {
            InitializeGame(row, col);
        }
        else if (IsAvailableMove(row, col))
        {
            MakeMove(row, col);
        }
    }

    void InitializeGame(int row, int col)
    {
        board[row, col].SetValue(1);
        currentPosition = new Vector2Int(row, col);
        currentValue = 2;
        gameStarted = true;
        score = CalculateScore(1);
        
        UpdateAvailableMoves();
        UpdateUI();
    }

    void ClearAvailableMoves()
    {
        foreach (var move in availableMoves)
        {
            if (board[move.x, move.y] != null && board[move.x, move.y].IsEmpty())
            {
                board[move.x, move.y].SetAvailable(false);
            }
        }
        availableMoves.Clear();
    }

    int CalculateScore(int number)
    {
        // Base score is equal to the number itself
        float baseScore = number;
        
        // Calculate extra score based on percentage
        float extraPercentage = extraPercentages[number];
        float extraScore = number * extraPercentage;
        
        // Total score before special bonus
        float totalScore = baseScore + extraScore;
        
        // Apply special bonus if applicable
        if (specialNumbers.Contains(number) && specialBonusPercentages.ContainsKey(number))
        {
            float bonusPercentage = specialBonusPercentages[number] / 100f;
            float bonusScore = totalScore * bonusPercentage;
            totalScore += bonusScore;
        }
        
        return Mathf.RoundToInt(totalScore);
    }

    void MakeMove(int toRow, int toCol)
    {
        if (!currentPosition.HasValue) return;

        board[toRow, toCol].SetValue(currentValue);
        currentPosition = new Vector2Int(toRow, toCol);
        
        // Calculate and add score for this move
        int moveScore = CalculateScore(currentValue);
        score += moveScore;
        
        currentValue = Mathf.Min(currentValue + 1, MAX_VALUE);
        
        ClearAvailableMoves();
        UpdateAvailableMoves();
        UpdateUI();

        if (availableMoves.Count == 0)
        {
            gameStatusText.text = $"Game Over! Final Score: {score:N0}";
            gameStatusText.color = Color.red;
        }
    }

    bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (toRow < 0 || toRow >= BOARD_SIZE || toCol < 0 || toCol >= BOARD_SIZE)
            return false;

        int rowDiff = toRow - fromRow;
        int colDiff = toCol - fromCol;

        if (Mathf.Abs(rowDiff) == 2 && Mathf.Abs(colDiff) == 2)
        {
            return board[fromRow + rowDiff / 2, fromCol + colDiff / 2].IsEmpty();
        }

        if (Mathf.Abs(rowDiff) == 3 && colDiff == 0)
        {
            return board[fromRow + rowDiff / 3, fromCol].IsEmpty() && 
                   board[fromRow + (2 * rowDiff) / 3, fromCol].IsEmpty();
        }
        if (rowDiff == 0 && Mathf.Abs(colDiff) == 3)
        {
            return board[fromRow, fromCol + colDiff / 3].IsEmpty() && 
                   board[fromRow, fromCol + (2 * colDiff) / 3].IsEmpty();
        }

        return false;
    }

    void UpdateAvailableMoves()
    {
        if (!currentPosition.HasValue) return;

        int row = currentPosition.Value.x;
        int col = currentPosition.Value.y;

        for (int i = 0; i < BOARD_SIZE; i++)
        {
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                if (IsValidMove(row, col, i, j) && board[i, j].IsEmpty())
                {
                    availableMoves.Add(new Vector2Int(i, j));
                    board[i, j].SetAvailable(true);
                }
            }
        }
    }

    bool IsAvailableMove(int row, int col)
    {
        return availableMoves.Contains(new Vector2Int(row, col));
    }

    void ResetGame()
    {
        ClearAvailableMoves();
        
        for (int row = 0; row < BOARD_SIZE; row++)
        {
            for (int col = 0; col < BOARD_SIZE; col++)
            {
                board[row, col].Reset();
            }
        }
        
        currentValue = 1;
        currentPosition = null;
        score = 0;
        gameStarted = false;
        UpdateUI();
    }

    void UpdateUI()
    {
        scoreText.text = $"Score: {score:N0}"; // Format with thousand separators
        
        if (!gameStarted)
        {
            gameStatusText.text = "Click on any cell to start the game.";
            gameStatusText.color = Color.white;
        }
        else if (availableMoves.Count == 0)
        {
            gameStatusText.text = $"Game Over! Final Score: {score:N0}";
            gameStatusText.color = Color.red;
        }
        else
        {
            gameStatusText.text = "";
        }
    }

    void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetGame);
        }
    }
}