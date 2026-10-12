
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
    public Color boardColor =
        new Color32(16, 23, 34, 255);

    public Color cellColor =
        new Color32(38, 52, 73, 255);

    public Color numberColor =
        new Color32(66, 230, 196, 255);

    [Header("Target Settings")]
    public int minTargetNumber = 4;
    public int maxTargetNumber = 10;

    [Header("Swipe Settings")]
    public float swipeThreshold = 40f;

    private const int DesiredNumberCount = 4;

    // 0 = boþ hücre.
    private int[,] board;

    // Sayýlarýn ekrandaki yazýlarý.
    private TextMesh[,] numberTexts;

    // Hedef sayý ve ekrandaki yazýsý.
    private int targetNumber;
    private TextMesh targetText;

    private Vector2 pointerStart;
    private bool trackingPointer;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = boardColor;
        }

        board = new int[rows, columns];
        numberTexts = new TextMesh[rows, columns];

        GenerateNewTarget();
        CreateBoard();
        CreateTargetText();
        FillBoardWithStartingNumbers();
    }

    private void Update()
    {
        HandleSwipeInput();
    }

    // -----------------------------
    // HEDEF SAYI
    // -----------------------------


    void GenerateNewTarget()
    {
        List<int> availableTargets = new List<int>();

        // Hedef olarak 4-10 arasýndaki sayýlarý deðerlendir.
        for (int number = minTargetNumber;
             number <= maxTargetNumber;
             number++)
        {
            bool existsOnBoard = false;

            // Sayý tahtada var mý kontrol et.
            if (board != null)
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        if (board[row, col] == number)
                        {
                            existsOnBoard = true;
                            break;
                        }
                    }

                    if (existsOnBoard)
                        break;
                }
            }

            // Tahtada bulunmayan sayýlarý aday olarak ekle.
            if (!existsOnBoard)
            {
                availableTargets.Add(number);
            }
        }

        if (availableTargets.Count > 0)
        {
            targetNumber = availableTargets[
                Random.Range(0, availableTargets.Count)
            ];
        }
        else
        {
            // Uygun hedef yoksa aralýðý geniþlet.
            targetNumber = maxTargetNumber + 1;

            while (true)
            {
                bool existsOnBoard = false;

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        if (board[row, col] == targetNumber)
                        {
                            existsOnBoard = true;
                            break;
                        }
                    }

                    if (existsOnBoard)
                        break;
                }

                if (!existsOnBoard)
                    break;

                targetNumber++;
            }
        }

        Debug.Log("Yeni hedef sayý: " + targetNumber);
    }

    void CreateTargetText()
    {
        GameObject targetObject =
            new GameObject("TargetNumber");

        targetObject.transform.SetParent(transform, false);

        float topY =
            ((rows - 1) * (cellSize + spacing)) / 2f
            + cellSize * 0.85f
            + 0.2f;

        targetObject.transform.localPosition =
            new Vector3(0f, topY, -0.2f);

        targetText =
            targetObject.AddComponent<TextMesh>();

        targetText.text = "HEDEF: " + targetNumber;
        targetText.fontSize = 80;
        targetText.characterSize = 0.12f;
        targetText.anchor = TextAnchor.MiddleCenter;
        targetText.alignment = TextAlignment.Center;
        targetText.color = numberColor;
        targetText.fontStyle = FontStyle.Bold;

        MeshRenderer meshRenderer =
            targetObject.GetComponent<MeshRenderer>();

        meshRenderer.sortingOrder = 5;
    }

    void UpdateTargetText()
    {
        if (targetText != null)
        {
            targetText.text = "HEDEF: " + targetNumber;
        }
    }

    // -----------------------------
    // TAHTAYI OLUÞTUR
    // -----------------------------

    void CreateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject cell = new GameObject(
                    $"Cell_{row}_{col}"
                );

                cell.transform.SetParent(transform, false);
                cell.transform.localPosition =
                    GetCellPosition(row, col);

                SpriteRenderer renderer =
                    cell.AddComponent<SpriteRenderer>();

                renderer.sprite = CreateSquareSprite();
                renderer.color = cellColor;
                renderer.sortingOrder = 1;

                cell.transform.localScale =
                    new Vector3(cellSize, cellSize, 1f);
            }
        }
    }

    Vector3 GetCellPosition(int row, int col)
    {
        float x =
            col * (cellSize + spacing)
            - ((columns - 1) * (cellSize + spacing)) / 2f;

        float y =
            row * (cellSize + spacing)
            - ((rows - 1) * (cellSize + spacing)) / 2f;

        return new Vector3(x, y, -0.1f);
    }

    // -----------------------------
    // BAÞLANGIÇ SAYILARI
    // -----------------------------

    void FillBoardWithStartingNumbers()
    {
        int totalCells = rows * columns;
        int numberCount = Mathf.Min(
            DesiredNumberCount,
            totalCells
        );

        List<Vector2Int> emptyCells =
            GetEmptyCells();

        // Hedefe ulaþabilecek bir baþlangýç çifti üret.
        int smallNumber = Random.Range(1, 4);
        int bigNumber = targetNumber + smallNumber;

        List<int> initialNumbers = new List<int>();

        initialNumbers.Add(bigNumber);
        initialNumbers.Add(smallNumber);

        // Diðer baþlangýç sayýlarý da birbirinden farklý olsun.
        while (initialNumbers.Count < numberCount)
        {
            int candidate = Random.Range(1, 7);

            if (!initialNumbers.Contains(candidate))
            {
                initialNumbers.Add(candidate);
            }
        }

        // Sayýlarýn yerleþim sýrasýný karýþtýr.
        for (int i = 0; i < initialNumbers.Count; i++)
        {
            int randomIndex = Random.Range(
                i,
                initialNumbers.Count
            );

            int temp = initialNumbers[i];
            initialNumbers[i] = initialNumbers[randomIndex];
            initialNumbers[randomIndex] = temp;
        }

        // Sayýlarý rastgele boþ hücrelere yerleþtir.
        for (int i = 0; i < numberCount; i++)
        {
            int randomIndex = Random.Range(
                0,
                emptyCells.Count
            );

            Vector2Int cell = emptyCells[randomIndex];
            emptyCells.RemoveAt(randomIndex);

            board[cell.x, cell.y] = initialNumbers[i];

            CreateNumberText(
                cell.x,
                cell.y,
                initialNumbers[i]
            );
        }
    }

    void CreateNumberText(int row, int col, int value)
    {
        GameObject numberObject = new GameObject(
            $"Number_{value}"
        );

        numberObject.transform.SetParent(transform, false);
        numberObject.transform.localPosition =
            GetCellPosition(row, col);

        TextMesh textMesh =
            numberObject.AddComponent<TextMesh>();

        textMesh.text = value.ToString();
        textMesh.fontSize = 64;
        textMesh.characterSize = 0.15f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = numberColor;
        textMesh.fontStyle = FontStyle.Bold;

        MeshRenderer meshRenderer =
            numberObject.GetComponent<MeshRenderer>();

        meshRenderer.sortingOrder = 5;

        numberTexts[row, col] = textMesh;
    }

    // -----------------------------
    // SWIPE KONTROLLERÝ
    // -----------------------------

    void HandleSwipeInput()
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
            else if (
                touch.phase == TouchPhase.Ended
                && trackingPointer
            )
            {
                currentPosition = touch.position;
                trackingPointer = false;

                ProcessSwipe(pointerStart, currentPosition);
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            pointerStart = Input.mousePosition;
            trackingPointer = true;
        }

        if (
            Input.GetMouseButtonUp(0)
            && trackingPointer
        )
        {
            currentPosition = Input.mousePosition;
            trackingPointer = false;

            ProcessSwipe(pointerStart, currentPosition);
        }
    }

    void ProcessSwipe(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;

        if (delta.magnitude < swipeThreshold)
        {
            return;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector2 worldStart = ScreenToWorld(start);

        if (!TryGetCell(worldStart, out int row, out int col))
        {
            return;
        }

        if (board[row, col] == 0)
        {
            return;
        }

        int rowDirection = 0;
        int colDirection = 0;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            colDirection = delta.x > 0 ? 1 : -1;
        }
        else
        {
            rowDirection = delta.y > 0 ? 1 : -1;
        }

        TryMoveNumber(
            row,
            col,
            rowDirection,
            colDirection
        );
    }

    Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        float distance =
            -mainCamera.transform.position.z;

        Vector3 world =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    distance
                )
            );

        return new Vector2(world.x, world.y);
    }

    bool TryGetCell(
        Vector2 worldPosition,
        out int row,
        out int col
    )
    {
        float step = cellSize + spacing;

        float xOffset =
            worldPosition.x + ((columns - 1) * step) / 2f;

        float yOffset =
            worldPosition.y + ((rows - 1) * step) / 2f;

        col = Mathf.RoundToInt(xOffset / step);
        row = Mathf.RoundToInt(yOffset / step);

        if (
            row < 0 || row >= rows ||
            col < 0 || col >= columns
        )
        {
            return false;
        }

        Vector3 cellPosition =
            GetCellPosition(row, col);

        float maxDistance = cellSize / 2f;

        if (
            Mathf.Abs(worldPosition.x - cellPosition.x)
                > maxDistance ||
            Mathf.Abs(worldPosition.y - cellPosition.y)
                > maxDistance
        )
        {
            return false;
        }

        return true;
    }

    // -----------------------------
    // SAYIYI HAREKET ETTÝR VE ÝÞLEM YAP
    // -----------------------------

    void TryMoveNumber(
        int row,
        int col,
        int rowDirection,
        int colDirection
    )
    {
        int targetRow = row + rowDirection;
        int targetCol = col + colDirection;

        if (
            targetRow < 0 || targetRow >= rows ||
            targetCol < 0 || targetCol >= columns
        )
        {
            return;
        }

        int movingNumber = board[row, col];
        int targetCellNumber = board[targetRow, targetCol];

        if (movingNumber == 0)
        {
            return;
        }

        // Hedef boþsa sadece taþý.
        if (targetCellNumber == 0)
        {
            board[targetRow, targetCol] = movingNumber;
            board[row, col] = 0;

            numberTexts[targetRow, targetCol] =
                numberTexts[row, col];

            numberTexts[row, col] = null;

            numberTexts[targetRow, targetCol]
                .transform.localPosition =
                GetCellPosition(targetRow, targetCol);

            return;
        }

        int result;

        if (movingNumber > targetCellNumber)
        {
            result = movingNumber - targetCellNumber;
        }
        else
        {
            result = movingNumber + targetCellNumber;
        }

        // Birleþtirme sonucunda hedefteki eski yazýyý kaldýr.
        if (numberTexts[targetRow, targetCol] != null)
        {
            Destroy(
                numberTexts[targetRow, targetCol].gameObject
            );
        }

        TextMesh movingText = numberTexts[row, col];

        board[row, col] = 0;
        numberTexts[row, col] = null;

        if (result == targetNumber)
        {
            // Hedefe ulaþan sayý silinir.
            if (movingText != null)
            {
                Destroy(movingText.gameObject);
            }

            board[targetRow, targetCol] = 0;
            numberTexts[targetRow, targetCol] = null;

            // Yeni hedef seç.
            GenerateNewTarget();
            UpdateTargetText();

            Debug.Log("Hedefe ulaþýldý! Yeni hedef: " + targetNumber);
        }
        else
        {
            // Sonucu hedef hücreye yerleþtir.
            board[targetRow, targetCol] = result;
            numberTexts[targetRow, targetCol] = movingText;

            if (movingText != null)
            {
                movingText.text = result.ToString();

                movingText.transform.localPosition =
                    GetCellPosition(targetRow, targetCol);
            }
        }

        // Her birleþtirmeden sonra tahtayý doldur.
        RefillNumbers();
    }

    // -----------------------------
    // YENÝ SAYI ÜRETME
    // -----------------------------

    void RefillNumbers()
    {
        int desiredCount = Mathf.Min(
            DesiredNumberCount,
            rows * columns
        );

        while (CountNumbersOnBoard() < desiredCount)
        {
            List<Vector2Int> emptyCells = GetEmptyCells();

            if (emptyCells.Count == 0)
            {
                return;
            }

            int newNumber = GenerateUniqueNumber();

            if (newNumber == -1)
            {
                Debug.LogWarning(
                    "Yeni sayý üretilemedi: kullanýlabilir farklý sayý kalmadý."
                );
                return;
            }

            Vector2Int cell =
                emptyCells[Random.Range(0, emptyCells.Count)];

            board[cell.x, cell.y] = newNumber;

            CreateNumberText(
                cell.x,
                cell.y,
                newNumber
            );
        }
    }

    int GenerateUniqueNumber()
    {
        HashSet<int> usedNumbers = new HashSet<int>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] != 0)
                {
                    usedNumbers.Add(board[row, col]);
                }
            }
        }

        List<int> availableNumbers = new List<int>();

        // Ýlk turdaki küçük sayýlar.
        for (int number = 1; number <= 6; number++)
        {
            if (!usedNumbers.Contains(number))
            {
                availableNumbers.Add(number);
            }
        }

        if (availableNumbers.Count == 0)
        {
            return -1;
        }

        return availableNumbers[
            Random.Range(0, availableNumbers.Count)
        ];
    }

    int CountNumbersOnBoard()
    {
        int count = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] != 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> emptyCells =
            new List<Vector2Int>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] == 0)
                {
                    emptyCells.Add(new Vector2Int(row, col));
                }
            }
        }

        return emptyCells;
    }

    // -----------------------------
    // KARE SPRITE
    // -----------------------------

    Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }
}
