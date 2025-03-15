using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    //private float m_timeAfterFill;
    //private bool m_hintIsShown;
    //private bool m_isDragging;
    //private Collider2D m_hitCollider;


    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;
    private GameManager.eLevelMode m_currentMode;
    private Camera m_cam;
    
    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private bool m_gameOver;

    public void StartGame(GameManager gameManager, GameSettings gameSettings,GameManager.eLevelMode mode)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;
        m_currentMode = mode;
        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();

        Auto(mode);
    }
    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
    }
    private void Auto(GameManager.eLevelMode _mode)
    {
        if (_mode == GameManager.eLevelMode.AUTOPLAY)
        {
            StartCoroutine(AutoplayCoroutine());
        }
        else if (_mode == GameManager.eLevelMode.AUTOLOSE)
        {
            StartCoroutine(AutoloseCoroutine());
        }
    }
    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.LOSE:
                m_gameOver = true;
                //StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        CheckExtraRowFull();

        /*if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }*/
        if(m_currentMode != GameManager.eLevelMode.AUTOPLAY && m_currentMode != GameManager.eLevelMode.AUTOLOSE)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                Cell clickedCell = hit.collider.GetComponent<Cell>();
                if (clickedCell != null)
                {
                    if (!clickedCell.IsEmpty)
                    {
                        // Kiểm tra xem ô được click có thuộc hàng phụ không
                        if (System.Array.IndexOf(m_board.ExtraRowCells, clickedCell) >= 0 && m_currentMode == GameManager.eLevelMode.TIMER)
                        {
                            ReturnItemToMainBoard(clickedCell);
                        }
                        else
                        {
                            MoveItemToExtraRow(clickedCell);
                        }
                    }
                }
            }
        }
        

        /*if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                if (m_hitCollider != null && m_hitCollider != hit.collider)
                {
                    StopHints();

                    Cell c1 = m_hitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        m_board.Swap(c1, c2, () =>
                        {
                            FindMatchesAndCollapse(c1, c2);
                        });

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
        }*/
    }
    /*private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }*/
    private void ClearMatchedCells(List<Cell> cellsToClear) 
    {
        foreach (Cell cell in cellsToClear)
        {
            cell.ExplodeItem();
        }
    }
    private void CheckAndClearMatchingItems()
    {
        Cell[] extraCells = m_board.ExtraRowCells;
        int matchCount = 1;
        List<Cell> cellsToClear = new List<Cell>();
        bool hasMatch = false;

        Debug.Log("Checking extra row for matches...");
        for (int i = 0; i < extraCells.Length - 1; i++)
        {
            if (extraCells[i].IsEmpty || extraCells[i + 1].IsEmpty)
            {
                if (matchCount >= 3)
                {
                    Debug.Log($"Found match of {matchCount} items at position {i - matchCount + 1}");
                    ClearMatchedCells(cellsToClear);
                    hasMatch = true;
                }
                matchCount = 1;
                cellsToClear.Clear();
                continue;
            }

            if (extraCells[i].IsSameType(extraCells[i + 1]))
            {
                if (matchCount == 1) cellsToClear.Add(extraCells[i]);
                cellsToClear.Add(extraCells[i + 1]);
                matchCount++;
            }
            else
            {
                if (matchCount >= 3)
                {
                    Debug.Log($"Found match of {matchCount} items at position {i - matchCount + 1}");
                    ClearMatchedCells(cellsToClear);
                    hasMatch = true;
                }
                matchCount = 1;
                cellsToClear.Clear();
            }
        }

        if (matchCount >= 3)
        {
            Debug.Log($"Found match of {matchCount} items at end of row");
            ClearMatchedCells(cellsToClear);
            hasMatch = true;
        }

        // Log trạng thái hàng phụ sau khi xóa
        Debug.Log("Extra row state after checking:");
        for (int i = 0; i < extraCells.Length; i++)
        {
            Debug.Log($"Position {i}: {(extraCells[i].IsEmpty ? "Empty" : extraCells[i].Item.GetType().Name)}");
        }

        // Chỉ kiểm tra thắng nếu không còn vật phẩm trên bảng chính
        if (hasMatch && !m_board.HasItemsOnMainBoard())
        {
            m_gameOver = true;
            m_gameManager.GameWin();
            Debug.Log("Bạn đã thắng: Tất cả vật phẩm trên bảng chính đã được xóa!");
        }
    }
    private void CheckExtraRowFull()
    {
        bool isExtraRowFull = true;
        foreach (Cell cell in m_board.ExtraRowCells)
        {
            if (cell.IsEmpty)
            {
                isExtraRowFull = false;
                break;
            }
        }

        if (isExtraRowFull && m_board.HasItemsOnMainBoard() && m_currentMode != GameManager.eLevelMode.TIMER)
        {
            m_gameOver = true;
            m_gameManager.GameOver();
        }
    }
    private void MoveItemToExtraRow(Cell sourceCell)
    {
        Cell targetCell = null;
        bool isExtraRowFull = true;
        foreach (Cell cell in m_board.ExtraRowCells)
        {
            if (cell.IsEmpty)
            {
                targetCell = cell;
                isExtraRowFull = false;
                break; 
            }
        }

        if (targetCell != null) 
        {
            IsBusy = true;

            Item item = sourceCell.Item;
            if (item != null)
            {

                sourceCell.Free();
                targetCell.Assign(item);
                item.SetViewRoot(m_board.root.transform);
                item.AnimationMoveToPosition();


                DOVirtual.DelayedCall(0.2f, () =>
                {
                    CheckAndClearMatchingItems();
                    //StartCoroutine(ShiftDownItemsCoroutine());
                    IsBusy = false;
                });
            }
        }
        else if (isExtraRowFull)
        {
            if (m_board.HasItemsOnMainBoard()) 
            {
                m_gameOver = true;
                m_gameManager.GameOver();
                
            }
            else 
            {
                CheckAndClearMatchingItems();   
                //m_gameOver = true;
                //m_gameManager.GameWin();
            }
        }
    }
    #region Autoplay
    private IEnumerator AutoplayCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (!m_gameOver)
        {
            if (m_board.HasItemsOnMainBoard())
            {
                // Bước 1: Chọn ngẫu nhiên một vật phẩm trên bảng chính
                Cell initialCell = FindRandomItemOnMainBoard();
                if (initialCell != null)
                {
                    NormalItem.eNormalType targetType = (initialCell.Item as NormalItem)?.ItemType ?? NormalItem.eNormalType.TYPE_ONE;
                    Debug.Log($"Autoplay: Starting with item of type {targetType} at ({initialCell.BoardX}, {initialCell.BoardY})");

                    // Bước 2: Di chuyển vật phẩm cùng loại cho đến khi tạo chuỗi và xóa
                    yield return StartCoroutine(MoveMatchingItems(initialCell, targetType));
                }
                else
                {
                    Debug.LogWarning("No items found on main board!");
                    break;
                }
            }
            else
            {
                Debug.Log("Main board is empty, game should win!");
                break;
            }

            yield return new WaitForSeconds(0.5f); // Độ trễ giữa các chu kỳ

            while (IsBusy)
            {
                yield return null;
            }
        }
    }
    private Cell FindRandomItemOnMainBoard()
    {
        List<Cell> potentialCells = new List<Cell>();
        for (int x = 0; x < m_board.BoardSizeX; x++)
        {
            for (int y = 0; y < m_board.BoardSizeY; y++)
            {
                Cell cell = m_board.Cells[x, y];
                if (!cell.IsEmpty)
                {
                    potentialCells.Add(cell);
                }
            }
        }

        if (potentialCells.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, potentialCells.Count);
            return potentialCells[randomIndex];
        }
        return null;
    }
    private IEnumerator MoveMatchingItems(Cell initialCell, NormalItem.eNormalType targetType)
    {
        bool hasMoreMatches = true;

        while (hasMoreMatches && !m_gameOver)
        {
            // Di chuyển vật phẩm ban đầu (nếu chưa di chuyển)
            if (initialCell != null && !initialCell.IsEmpty)
            {
                MoveItemToExtraRow(initialCell);
                yield return new WaitUntil(() => !IsBusy); // Chờ đến khi di chuyển hoàn tất
                initialCell = null; // Đánh dấu đã di chuyển vật phẩm ban đầu
            }

            // Tìm và di chuyển các vật phẩm cùng loại khác
            List<Cell> matchingCells = FindMatchingItemsOnMainBoard(targetType);
            foreach (Cell cell in matchingCells)
            {
                if (!cell.IsEmpty)
                {
                    MoveItemToExtraRow(cell);
                    yield return new WaitUntil(() => !IsBusy); // Chờ đến khi di chuyển hoàn tất
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // Kiểm tra xem có còn vật phẩm cùng loại trên bảng chính không
            matchingCells = FindMatchingItemsOnMainBoard(targetType);
            hasMoreMatches = matchingCells.Count > 0;

            // Chờ một chút để xử lý xóa trong hàng phụ
            yield return new WaitForSeconds(0.25f);
        }
    }
    private List<Cell> FindMatchingItemsOnMainBoard(NormalItem.eNormalType targetType)
    {
        List<Cell> matchingCells = new List<Cell>();
        for (int x = 0; x < m_board.BoardSizeX; x++)
        {
            for (int y = 0; y < m_board.BoardSizeY; y++)
            {
                Cell cell = m_board.Cells[x, y];
                if (!cell.IsEmpty && cell.Item is NormalItem normalItem && normalItem.ItemType == targetType)
                {
                    matchingCells.Add(cell);
                }
            }
        }
        return matchingCells;
    }
    #endregion
    # region Autolose
    private IEnumerator AutoloseCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (!m_gameOver)
        {
            if (m_board.HasItemsOnMainBoard())
            {
                // Chọn ngẫu nhiên một vật phẩm trên bảng chính
                Cell randomCell = FindRandomItemOnMainBoard();
                if (randomCell != null)
                {
                    Debug.Log($"Autolose: Moving random item from ({randomCell.BoardX}, {randomCell.BoardY})");
                    MoveItemToExtraRow(randomCell);
                    yield return new WaitUntil(() => !IsBusy); // Chờ đến khi di chuyển hoàn tất
                    yield return new WaitForSeconds(0.5f); // Độ trễ 0.5 giây giữa mỗi autoclick
                }
                else
                {
                    Debug.LogWarning("No items found on main board!");
                    break;
                }
            }
            else
            {
                Debug.Log("Main board is empty, but autolose should have triggered game over earlier!");
                break;
            }
        }
    }
    #endregion 
    private void ReturnItemToMainBoard(Cell extraCell)
    {
        if (extraCell.IsEmpty || !(extraCell.Item is Item item)) return;

        // Tìm một ô trống trên bảng chính
        Cell targetCell = FindEmptyCellOnMainBoard();
        if (targetCell != null)
        {
            IsBusy = true;
            extraCell.Free();
            targetCell.Assign(item);
            item.SetViewRoot(m_board.root.transform);
            item.AnimationMoveToPosition();

            DOVirtual.DelayedCall(0.2f, () =>
            {
                CheckAndClearMatchingItems();
                IsBusy = false;
            });
        }
    }
    private Cell FindEmptyCellOnMainBoard()
    {
        for (int x = 0; x < m_board.BoardSizeX; x++)
        {
            for (int y = 0; y < m_board.BoardSizeY; y++)
            {
                Cell cell = m_board.Cells[x, y];
                if (cell.IsEmpty)
                {
                    return cell;
                }
            }
        }
        return null;
    }
    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                //m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }
    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if (matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }
    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }
    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    /*private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }*/
    /*private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }*/
    /*private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }*/

    /*private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }*/

    /*private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }*/

    /*private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }*/
}
