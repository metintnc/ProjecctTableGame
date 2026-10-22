using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 4;
    public int columns = 4;
    public float cellSize = 1f;
    public float spacing = 0.08f;

    [Header("Colors")]
    public Color boardColor = new Color32(16, 23, 34, 255);
    public Color cellColor = new Color32(41, 58, 80, 255);
    public Color borderColor = new Color32(98, 122, 150, 255);
    public Color numberColor = new Color32(66, 230, 196, 255);

    [Header("Cell Border")]
    [Min(0f)] public float borderThickness = 0.055f;

    [Header("Target Settings")]
    public int minTargetNumber = 4;
    public int maxTargetNumber = 10;

    [Header("Spawn Settings")]
    public int minSpawnNumber = 1;
    public int maxSpawnNumber = 9;

    [Header("Swipe Settings")]
    public float swipeThreshold = 40f;

    [Header("Swipe Animation")]
    [Min(0.01f)] public float swipeAnimationDuration = 0.14f;

    [Header("Gravity Animation")]
    [Min(0.01f)] public float gravityAnimationDuration = 0.18f;

    private const int DesiredNumberCount = 4;

    // 0 = empty cell.
    private int[,] board;
    private TextMesh[,] numberTexts;

    private int targetNumber;
    private int previousTargetNumber = int.MinValue;
    private TextMesh targetText;

    private Vector2 pointerStart;
    private bool trackingPointer;
    private bool isAnimating;

    private Camera mainCamera;
    private Sprite squareSprite;

    private int validMoveCount;
    private bool pendingSpawn;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
            mainCamera.backgroundColor = boardColor;
        else
            Debug.LogWarning("MainCamera tag'ine sahip kamera bulunamadý.");

        board = new int[rows, columns];
        numberTexts = new TextMesh[rows, columns];

        squareSprite = CreateSquareSprite();
        CreateBoard();
        GenerateNewTarget();
        CreateTargetText();
        FillBoardWithStartingNumbers();

        UpdateTargetText();
    }

    private void Update()
    {
        if (isAnimating)
            return;

        HandleSwipeInput();
    }

    // -----------------------------
    // TARGET NUMBER
    // -----------------------------

    private void GenerateNewTarget()
    {
        List<int> availableTargets = new List<int>();

        for (int number = minTargetNumber; number <= maxTargetNumber; number++)
        {
            if (!IsNumberOnBoard(number) && number != previousTargetNumber)
                availableTargets.Add(number);
        }

        // If the configured range has no available target, widen it.
        if (availableTargets.Count == 0)
        {
            int upperLimit = Mathf.Max(maxTargetNumber, minTargetNumber);

            for (int number = 1; number <= upperLimit; number++)
            {
                if (!IsNumberOnBoard(number) && number != previousTargetNumber)
                    availableTargets.Add(number);
            }
        }

        if (availableTargets.Count == 0)
        {
            int candidate = 1;
            while (IsNumberOnBoard(candidate))
                candidate++;

            availableTargets.Add(candidate);
        }

        int oldTarget = targetNumber;
        targetNumber = availableTargets[Random.Range(0, availableTargets.Count)];

        // Prevent the next target from matching the previous target.
        if (oldTarget > 0)
            previousTargetNumber = oldTarget;

        UpdateTargetText();

        Debug.Log("Yeni hedef sayý: " + targetNumber);
    }

    private bool IsNumberOnBoard(int number)
    {
        if (board == null)
            return false;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] == number)
                    return true;
            }
        }

        return false;
    }

    private void CreateTargetText()
    {
        GameObject targetObject = new GameObject("TargetNumber");
        targetObject.transform.SetParent(transform, false);

        float topY = ((rows - 1) * (cellSize + spacing)) / 2f
                     + cellSize * 0.85f + 0.2f;

        targetObject.transform.localPosition = new Vector3(0f, topY, -0.2f);

        targetText = targetObject.AddComponent<TextMesh>();
        targetText.text = "HEDEF: " + targetNumber;
        targetText.fontSize = 80;
        targetText.characterSize = 0.12f;
        targetText.anchor = TextAnchor.MiddleCenter;
        targetText.alignment = TextAlignment.Center;
        targetText.color = numberColor;
        targetText.fontStyle = FontStyle.Bold;

        targetObject.GetComponent<MeshRenderer>().sortingOrder = 5;
    }

    private void UpdateTargetText()
    {
        if (targetText != null)
            targetText.text = "HEDEF: " + targetNumber;
    }

    // -----------------------------
    // BOARD AND CELL VISUALS
    // -----------------------------

    private void CreateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 position = GetCellPosition(row, col);

                // Larger, lighter square behind the cell creates a clear border.
                GameObject border = new GameObject($"Border_{row}_{col}");
                border.transform.SetParent(transform, false);
                border.transform.localPosition = new Vector3(
                    position.x, position.y, position.z + 0.01f);
                border.transform.localScale = new Vector3(
                    cellSize + borderThickness * 2f,
                    cellSize + borderThickness * 2f,
                    1f);

                SpriteRenderer borderRenderer = border.AddComponent<SpriteRenderer>();
                borderRenderer.sprite = squareSprite;
                borderRenderer.color = borderColor;
                borderRenderer.sortingOrder = 0;

                GameObject cell = new GameObject($"Cell_{row}_{col}");
                cell.transform.SetParent(transform, false);
                cell.transform.localPosition = position;
                cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                SpriteRenderer cellRenderer = cell.AddComponent<SpriteRenderer>();
                cellRenderer.sprite = squareSprite;
                cellRenderer.color = cellColor;
                cellRenderer.sortingOrder = 1;
            }
        }
    }

    private Vector3 GetCellPosition(int row, int col)
    {
        float x = col * (cellSize + spacing)
                  - ((columns - 1) * (cellSize + spacing)) / 2f;

        float y = row * (cellSize + spacing)
                  - ((rows - 1) * (cellSize + spacing)) / 2f;

        return new Vector3(x, y, -0.1f);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f);
    }

    // -----------------------------
    // STARTING NUMBERS
    // -----------------------------

    private void FillBoardWithStartingNumbers()
    {
        int totalCells = rows * columns;
        int numberCount = Mathf.Min(DesiredNumberCount, totalCells);

        List<Vector2Int> emptyCells = GetEmptyCells();

        // Include a pair that can reach the target by subtraction.
        int smallNumber = Random.Range(1, 4);
        int bigNumber = targetNumber + smallNumber;

        List<int> initialNumbers = new List<int> { bigNumber, smallNumber };

        while (initialNumbers.Count < numberCount)
        {
            int candidate = Random.Range(1, 7);
            if (!initialNumbers.Contains(candidate))
                initialNumbers.Add(candidate);
        }

        // Shuffle initial numbers.
        for (int i = 0; i < initialNumbers.Count; i++)
        {
            int randomIndex = Random.Range(i, initialNumbers.Count);
            int temp = initialNumbers[i];
            initialNumbers[i] = initialNumbers[randomIndex];
            initialNumbers[randomIndex] = temp;
        }

        for (int i = 0; i < numberCount && emptyCells.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, emptyCells.Count);
            Vector2Int cell = emptyCells[randomIndex];
            emptyCells.RemoveAt(randomIndex);

            board[cell.x, cell.y] = initialNumbers[i];
            CreateNumberText(cell.x, cell.y, initialNumbers[i]);
        }
    }

    private void CreateNumberText(int row, int col, int value)
    {
        GameObject numberObject = new GameObject($"Number_{row}_{col}");
        numberObject.transform.SetParent(transform, false);
        numberObject.transform.localPosition = GetCellPosition(row, col);

        TextMesh textMesh = numberObject.AddComponent<TextMesh>();
        textMesh.text = value.ToString();
        textMesh.fontSize = 64;
        textMesh.characterSize = 0.15f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = numberColor;
        textMesh.fontStyle = FontStyle.Bold;

        numberObject.GetComponent<MeshRenderer>().sortingOrder = 5;
        numberTexts[row, col] = textMesh;
    }

    private void SetNumberTextPosition(int row, int col)
    {
        if (numberTexts[row, col] != null)
            numberTexts[row, col].transform.localPosition = GetCellPosition(row, col);
    }

    // -----------------------------
    // SWIPE INPUT
    // -----------------------------

    private void HandleSwipeInput()
    {
        Vector2 currentPosition;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                pointerStart = touch.position;
                trackingPointer = true;
            }
            else if (touch.phase == TouchPhase.Ended && trackingPointer)
            {
                currentPosition = touch.position;
                trackingPointer = false;
                ProcessSwipe(pointerStart, currentPosition);
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                trackingPointer = false;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            pointerStart = Input.mousePosition;
            trackingPointer = true;
        }

        if (Input.GetMouseButtonUp(0) && trackingPointer)
        {
            currentPosition = Input.mousePosition;
            trackingPointer = false;
            ProcessSwipe(pointerStart, currentPosition);
        }
    }

    private void ProcessSwipe(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;

        if (delta.magnitude < swipeThreshold || mainCamera == null)
            return;

        Vector2 worldStart = ScreenToWorld(start);

        if (!TryGetCell(worldStart, out int row, out int col))
            return;

        if (board[row, col] == 0)
            return;

        int rowDirection = 0;
        int colDirection = 0;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            colDirection = delta.x > 0 ? 1 : -1;
        else
            rowDirection = delta.y > 0 ? 1 : -1;

        TryMoveNumber(row, col, rowDirection, colDirection);
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        float distance = -mainCamera.transform.position.z;

        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance));

        return new Vector2(world.x, world.y);
    }

    private bool TryGetCell(Vector2 worldPosition, out int row, out int col)
    {
        float step = cellSize + spacing;

        float xOffset = worldPosition.x + ((columns - 1) * step) / 2f;
        float yOffset = worldPosition.y + ((rows - 1) * step) / 2f;

        col = Mathf.RoundToInt(xOffset / step);
        row = Mathf.RoundToInt(yOffset / step);

        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return false;

        Vector3 cellPosition = GetCellPosition(row, col);
        float maxDistance = cellSize / 2f;

        return Mathf.Abs(worldPosition.x - cellPosition.x) <= maxDistance
               && Mathf.Abs(worldPosition.y - cellPosition.y) <= maxDistance;
    }

    // -----------------------------
    // MOVE, MERGE, AND VALID-MOVE COUNT
    // -----------------------------

    private void TryMoveNumber(int row, int col, int rowDirection, int colDirection)
    {
        int targetRow = row + rowDirection;
        int targetCol = col + colDirection;

        if (targetRow < 0 || targetRow >= rows ||
            targetCol < 0 || targetCol >= columns)
            return;

        if (board[row, col] == 0)
            return;

        StartCoroutine(AnimateSwipeMove(row, col, targetRow, targetCol));
    }

    private IEnumerator AnimateSwipeMove(int row, int col, int targetRow, int targetCol)
    {
        isAnimating = true;

        int movingNumber = board[row, col];
        int targetCellNumber = board[targetRow, targetCol];
        TextMesh movingText = numberTexts[row, col];

        // Animate the swiped number toward the neighboring cell before applying the move.
        if (movingText != null)
        {
            Vector3 startPosition = movingText.transform.localPosition;
            Vector3 destination = GetCellPosition(targetRow, targetCol);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, swipeAnimationDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t * (3f - 2f * t);
                movingText.transform.localPosition = Vector3.Lerp(startPosition, destination, easedT);
                yield return null;
            }

            movingText.transform.localPosition = destination;
        }

        if (targetCellNumber == 0)
        {
            board[targetRow, targetCol] = movingNumber;
            board[row, col] = 0;

            numberTexts[targetRow, targetCol] = movingText;
            numberTexts[row, col] = null;

            isAnimating = false;
            CompleteValidMove(targetRow, targetCol, row, col);
            yield break;
        }

        int result = movingNumber > targetCellNumber
            ? movingNumber - targetCellNumber
            : movingNumber + targetCellNumber;

        TextMesh targetTextMesh = numberTexts[targetRow, targetCol];
        bool reachedTarget = result == targetNumber;

        board[row, col] = 0;
        numberTexts[row, col] = null;

        if (reachedTarget)
        {
            // Remove the destination cell's old number immediately. Otherwise
            // its TextMesh and the moving result are both visible during the
            // success animation, making it look like two numbers appeared.
            if (targetTextMesh != null)
                Destroy(targetTextMesh.gameObject);

            // Show the final arithmetic result on the board and keep the old
            // target label visible briefly, so the player can recognize the match.
            board[targetRow, targetCol] = result;
            numberTexts[targetRow, targetCol] = movingText;

            if (movingText == null)
            {
                CreateNumberText(targetRow, targetCol, result);
                movingText = numberTexts[targetRow, targetCol];
            }
            else
            {
                movingText.text = result.ToString();
            }

            Vector3 originalScale = movingText != null
                ? movingText.transform.localScale : Vector3.one;
            Color originalColor = movingText != null
                ? movingText.color : numberColor;
            float celebrationDuration = 0.48f;
            float elapsed = 0f;

            // Pop/bounce animation: enlarge, then shrink away.
            while (elapsed < celebrationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / celebrationDuration);
                float scale;
                if (t < 0.45f)
                {
                    float popT = t / 0.45f;
                    scale = Mathf.Lerp(1f, 1.45f, popT);
                }
                else
                {
                    float vanishT = (t - 0.45f) / 0.55f;
                    scale = Mathf.Lerp(1.45f, 0.05f, vanishT);
                }

                if (movingText != null)
                {
                    movingText.transform.localScale = originalScale * scale;
                    movingText.color = Color.Lerp(originalColor, Color.white,
                        Mathf.Sin(t * Mathf.PI));
                }

                // Briefly emphasize the target label, then fade it out at the end.
                if (targetText != null)
                {
                    targetText.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
                    Color labelColor = numberColor;
                    labelColor.a = t > 0.7f ? 1f - ((t - 0.7f) / 0.3f) : 1f;
                    targetText.color = labelColor;
                }

                yield return null;
            }

            if (movingText != null)
                Destroy(movingText.gameObject);

            board[targetRow, targetCol] = 0;
            numberTexts[targetRow, targetCol] = null;

            // Only switch to the next target after the success animation.
            if (targetText != null)
            {
                targetText.transform.localScale = Vector3.one;
                targetText.color = numberColor;
            }
            GenerateNewTarget();
            Debug.Log("Hedefe ulaþýldý! Yeni hedef: " + targetNumber);
        }
        else
        {
            if (targetTextMesh != null)
                Destroy(targetTextMesh.gameObject);

            board[targetRow, targetCol] = result;
            numberTexts[targetRow, targetCol] = movingText;

            if (movingText != null)
                movingText.text = result.ToString();
            else
                CreateNumberText(targetRow, targetCol, result);
        }

        isAnimating = false;
        if (reachedTarget)
            CompleteValidMove(-1, -1, row, col);
        else
            CompleteValidMove(targetRow, targetCol, row, col);
    }

    private void CompleteValidMove(
        int protectedRow,
        int protectedCol,
        int swipedRow,
        int swipedCol)
    {
        validMoveCount++;

        // Swipe and math are resolved first. Every second valid move, run
        // gravity as an animation and block new input until it finishes.
        if (validMoveCount % 2 == 0)
        {
            StartCoroutine(ApplyGravityAnimated());
            return;
        }

        EnsureDesiredNumberCount();
        RefreshNumberTexts();
    }

    private IEnumerator ApplyGravityAnimated()
    {
        isAnimating = true;

        int[,] oldBoard = (int[,])board.Clone();
        int[,] newBoard = (int[,])board.Clone();
        TextMesh[,] oldTexts = (TextMesh[,])numberTexts.Clone();
        TextMesh[,] newTexts = new TextMesh[rows, columns];
        Vector3[,] startPositions = new Vector3[rows, columns];

        // Record each number's current position before changing the board.
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (oldTexts[row, col] != null)
                    startPositions[row, col] = oldTexts[row, col].transform.localPosition;
            }
        }

        // Row 0 is the bottom row. Each number falls at most one cell,
        // based on the board state at the start of this gravity step.
        for (int row = 1; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (oldBoard[row, col] != 0 && oldBoard[row - 1, col] == 0)
                {
                    newBoard[row - 1, col] = oldBoard[row, col];
                    newBoard[row, col] = 0;
                }
            }
        }

        // Keep TextMesh objects attached to their numbers, even when values repeat.
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (oldBoard[row, col] == 0)
                    continue;

                bool canFall = row > 0 && oldBoard[row - 1, col] == 0;
                if (canFall)
                    newTexts[row - 1, col] = oldTexts[row, col];
                else
                    newTexts[row, col] = oldTexts[row, col];
            }
        }

        board = newBoard;
        numberTexts = newTexts;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, gravityAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth start and stop for a more natural falling motion.
            float easedT = t * t * (3f - 2f * t);

            for (int oldRow = 0; oldRow < rows; oldRow++)
            {
                for (int col = 0; col < columns; col++)
                {
                    TextMesh text = oldTexts[oldRow, col];
                    if (text == null || oldBoard[oldRow, col] == 0)
                        continue;

                    int destinationRow = oldRow;
                    if (oldRow > 0 && oldBoard[oldRow - 1, col] == 0)
                        destinationRow = oldRow - 1;

                    Vector3 destination = GetCellPosition(destinationRow, col);
                    text.transform.localPosition = Vector3.Lerp(
                        startPositions[oldRow, col], destination, easedT);
                }
            }

            yield return null;
        }

        // Snap precisely to final cell positions when the animation completes.
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (numberTexts[row, col] != null)
                    numberTexts[row, col].transform.localPosition = GetCellPosition(row, col);
            }
        }

        EnsureDesiredNumberCount();
        RefreshNumberTexts();
        isAnimating = false;
    }

    // -----------------------------
    // KEEP FOUR NUMBERS ON THE BOARD
    // -----------------------------

    private void EnsureDesiredNumberCount()
    {
        int currentCount = CountBoardNumbers();

        while (currentCount < DesiredNumberCount)
        {
            // Spawn in the highest row that has an empty cell, so new numbers
            // enter from above whenever possible.
            int spawnRow = -1;
            List<int> availableColumns = new List<int>();

            for (int row = rows - 1; row >= 0; row--)
            {
                availableColumns.Clear();

                for (int col = 0; col < columns; col++)
                {
                    if (board[row, col] == 0)
                        availableColumns.Add(col);
                }

                if (availableColumns.Count > 0)
                {
                    spawnRow = row;
                    break;
                }
            }

            // Safety check: a full board cannot accept another number.
            if (spawnRow < 0)
                break;

            int chosenColumn = availableColumns[Random.Range(0, availableColumns.Count)];
            int newNumber = Random.Range(minSpawnNumber, maxSpawnNumber + 1);

            board[spawnRow, chosenColumn] = newNumber;
            CreateNumberText(spawnRow, chosenColumn, newNumber);
            currentCount++;
        }

        pendingSpawn = false;
    }

    private int CountBoardNumbers()
    {
        int count = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] != 0)
                    count++;
            }
        }

        return count;
    }

    private void RefreshNumberTexts()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] == 0)
                {
                    if (numberTexts[row, col] != null)
                        Destroy(numberTexts[row, col].gameObject);

                    numberTexts[row, col] = null;
                }
                else if (numberTexts[row, col] == null)
                {
                    CreateNumberText(row, col, board[row, col]);
                }
                else
                {
                    numberTexts[row, col].text = board[row, col].ToString();
                    SetNumberTextPosition(row, col);
                }
            }
        }

        UpdateTargetText();
    }

    // -----------------------------
    // HELPERS
    // -----------------------------

    private List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] == 0)
                    emptyCells.Add(new Vector2Int(row, col));
            }
        }

        return emptyCells;
    }
}
