using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class MineSweeper : MonoBehaviour
{
    public GameObject TilePrefab;
    public GameObject TilePanel;
    public Text GameOverLabel, BombLabel, TimeLabel;
    public Sprite TileCovered, TileDiscovered, Bomb, Flag;
    public Sprite[] AdjacentBombCountMap;

    const int totalBombs = 99;
    const int width = 32, height = 16;

    bool gameOver = false;
    bool firstClick = true;
    int flagged = 0;
    float startTime = 0;
    Tile[][] tiles;

    public class Tile
    {
        public int XPos { get; set; }
        public int YPos { get; set; }
        public GameObject Visual { get; set; }
        public bool IsBomb { get; set; } = false;
        public bool IsDiscovered { get; set; } = false;
        public bool IsFlagged { get; set; } = false;
        public int AdjacentBombCount { get; set; } = 0;
    }

    public void OnTileClicked(GameObject tileVisual)
    {
        if (gameOver)
            return;

        Tile tile = FindTileFromVisual(tileVisual);
        if (tile.IsFlagged)
            return;

        if (firstClick)
        {
            PlaceBombsRandomly(new(tile.XPos, tile.YPos));
            UpdateAdjacentBombCounts();
            firstClick = false;
            startTime = Time.time;
        }


        if (tile.IsDiscovered)
            return;

        tile.IsDiscovered = true;
        CascadeDiscover(tile, null);

        if (tile.IsBomb)
        {
            gameOver = true;
            GameOverLabel.color = new(1, 0, 0, .5f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x][y].IsDiscovered = true;
                }
            }
            UpdateAllTileVisuals();
        }

        UpdateTileVisual(tile);

    }

    public void OnTileFlagged(GameObject tileVisual)
    {
        if (gameOver)
            return;

        Tile tile = FindTileFromVisual(tileVisual);

        if (tile.IsDiscovered)
            return;

        if (tile.IsFlagged)
        {
            tile.IsFlagged = false;
            flagged--;
        }
        else
        {
            tile.IsFlagged = true;
            flagged++;
        }

        UpdateTileVisual(tile);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }

        if (!firstClick && !gameOver)
        {
            var gameDuration = Time.time - startTime;
            int minutes = (int)gameDuration / 60;
            int seconds = (int)gameDuration % 60;
            if (seconds < 10)
                TimeLabel.text = $"{minutes}:0{seconds}";
            else
                TimeLabel.text = $"{minutes}:{seconds}";

            BombLabel.text = $"{totalBombs - flagged}";
        }
    }

    void Start()
    {
        Restart();
    }
    public void Restart()
    {
        GameOverLabel.color = new(0, 0, 0, 0);
        gameOver = false;
        firstClick = true;
        flagged = 0;
        TimeLabel.GetComponent<Text>().text = $"00:00";
        InitialiseTileArray();
        DeleteAllTileVisuals();
        InitialiseTileVisuals();
    }
    private void InitialiseTileArray()
    {
        tiles = new Tile[width][];
        for (int x = 0; x < width; x++)
        {
            tiles[x] = new Tile[height];
            for (int y = 0; y < height; y++)
            {
                tiles[x][y] = new Tile
                {
                    AdjacentBombCount = 0,
                    IsDiscovered = false,
                    IsBomb = false,
                    XPos = x,
                    YPos = y
                };
            }
        }
    }
    private void PlaceBombsRandomly(Vector2Int startPos)
    {
        List<Vector2Int> emptyTiles = new List<Vector2Int>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isAdjToStart = false;
                foreach (var adj in AdjacentTiles(tiles[x][y]))
                {
                    if (Mathf.Abs(adj.XPos - startPos.x) <= 1
                        && Mathf.Abs(adj.YPos - startPos.y) <= 1)
                    {
                        isAdjToStart = true;
                        break;
                    }
                }

                if (isAdjToStart)
                    continue;

                emptyTiles.Add(new Vector2Int(x, y));
            }
        }

        int bombsToAllocate = totalBombs;

        while (bombsToAllocate > 0)
        {
            //Get random empty tile
            int positionIndex = Random.Range(0, emptyTiles.Count);
            Vector2Int bombPos = emptyTiles[positionIndex];
            emptyTiles.RemoveAt(positionIndex);

            tiles[bombPos.x][bombPos.y].IsBomb = true;
            bombsToAllocate--;
        }
    }
    private void UpdateAdjacentBombCounts()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (InBounds(x - 1, y - 1) && tiles[x - 1][y - 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x, y - 1) && tiles[x][y - 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x + 1, y - 1) && tiles[x + 1][y - 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x - 1, y) && tiles[x - 1][y].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x + 1, y) && tiles[x + 1][y].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x - 1, y + 1) && tiles[x - 1][y + 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x, y + 1) && tiles[x][y + 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;

                if (InBounds(x + 1, y + 1) && tiles[x + 1][y + 1].IsBomb)
                    tiles[x][y].AdjacentBombCount++;
            }
        }
    }
    private void InitialiseTileVisuals()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tiles[x][y].Visual = Instantiate(TilePrefab, TilePanel.transform);
                UpdateTileVisual(tiles[x][y]);
            }
        }
    }
    private void UpdateTileVisual(Tile tile)
    {
        var image = tile.Visual.GetComponent<Image>();
        image.color = new(1, 1, 1, 1);
        Image childImage = image.transform.GetChild(0).GetComponentInChildren<Image>();

        if (tile.IsDiscovered)
        {
            
            image.sprite = TileDiscovered;
            if (tile.IsBomb)
            {
                childImage.sprite = Bomb;
                childImage.color = Color.white;
            }
            else if (tile.AdjacentBombCount > 0)
            {
                childImage.sprite = AdjacentBombCountMap[tile.AdjacentBombCount];
                childImage.color = Color.white;
            }
            else
            {
                childImage.sprite = null;
                childImage.color = new(0, 0, 0, 0);
            }

        }
        else //Not discovered yet
        {
            image.sprite = TileCovered;
            childImage.color = new Color(1, 1, 1, 0);

            if (tile.IsFlagged)
            {
                childImage.sprite = Flag;
                childImage.color = Color.white;
            }
            else
            {
                childImage.color = new(1, 1, 1, 0);
            }
        }

    }
    private void UpdateAllTileVisuals()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                UpdateTileVisual(tiles[x][y]);
    }
    
    private void CascadeDiscover(Tile tile)
    {
        CascadeDiscover(tile, new HashSet<Tile>());
    }

    private void CascadeDiscover(Tile tile, HashSet<Tile> checkedTiles)
    {
        if (checkedTiles == null)
            checkedTiles = new HashSet<Tile>();

        if (checkedTiles.Contains(tile))
            return;

        checkedTiles.Add(tile);

        tile.IsDiscovered = true;
        if (tile.IsFlagged)
        {
            tile.IsFlagged = false;
            flagged--;
        }

        UpdateTileVisual(tile);

        if (tile.AdjacentBombCount > 0)
            return;

        foreach (var adjacent in AdjacentTiles(tile))
        {
            CascadeDiscover(adjacent, checkedTiles);
        }

    }
    private void DeleteAllTileVisuals()
    {
        for (int i = TilePanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(TilePanel.transform.GetChild(i).gameObject);
        }
    }
    IEnumerable<Tile> AdjacentTiles(Tile tile)
    {
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int adjX = tile.XPos + x;
                int adjY = tile.YPos + y;
                if (InBounds(adjX, adjY))
                {
                    yield return tiles[adjX][adjY];
                }
            }
        }
    }
    private Tile FindTileFromVisual(GameObject tileVisual)
    {
        Tile tile = null;
        for (int x = 0; x < width; x++)
        {
            bool found = false;
            for (int y = 0; y < height; y++)
            {
                if (tiles[x][y].Visual == tileVisual)
                {
                    tile = tiles[x][y];
                    found = true;
                    break;
                }
            }

            if (found)
                break;
        }

        return tile;
    }
    bool InBounds(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return true;

        return false;
    }

}
