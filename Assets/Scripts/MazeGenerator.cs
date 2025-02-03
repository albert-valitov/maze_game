using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.AI.Navigation;
using static Cell;

public class MazeGenerator : MonoBehaviour
{
    public Camera mainCamera;
    
    public Cell cellPrefab;

    public Player player;

    public Upgrade upgrade;

    public Enemy enemy;

    public Goal goal;

    public int mazeWidth;

    public int mazeHeight;

    public int enemyCounter;

    public int upgradeCounter;

    public bool isGoalPlaced;

    public Cell goalCell;

    private Cell[,] mazeGrid;

    private List<Player> players;

    void Awake()
    {
        DontDestroyOnLoad(this);
        InitDifficultyParameters();

        float centerX = (mazeWidth / 2f) - 0.5f;
        float CenterZ = (mazeHeight / 2f) - 0.5f;

        mainCamera = Camera.main;
        mainCamera.transform.position = new Vector3(centerX, mainCamera.transform.position.y, CenterZ);
        mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        float mazeHalfHeight = mazeHeight / 2f;
        float mazeHalfWidth = mazeWidth / 2f;

        mainCamera.orthographicSize = Mathf.Max(mazeHalfHeight, mazeHalfWidth / mainCamera.aspect);

        mazeGrid = new Cell[mazeWidth, mazeHeight];

        // init grid (x,z)
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeHeight; z++)
            {
                Cell cell = Instantiate(cellPrefab, new Vector3(x, 0, z), Quaternion.identity);
                cell.SetPosition(new Vector3(x, 0, z));
                mazeGrid[x, z] = cell;
            }
        }

        BuildMaze();
        GenerateMaze(null, mazeGrid[0, 0]);
        PlaceGoal(); 
        PlacePlayer();
        PlaceUpgrades();
        PlaceEnemy();

        GameManager.instance.SetMazeGrid(mazeGrid);
        GameManager.instance.InitAiController();
    }

    private void BuildMaze()
    {
        GenerateMaze(null, mazeGrid[0, 0]);
        AlterMaze((mazeWidth * 2) - mazeWidth/2);
    }

    private void AlterMaze(int numWallsToBreak)
    {
        List<Cell> candidates = new List<Cell>();

        // Collect all possible walls that can be broken (not on the outer edge)
        for (int x = 1; x < mazeWidth - 1; x++)
        {
            for (int z = 1; z < mazeHeight - 1; z++)
            {
                Cell cell = mazeGrid[x, z];

                // Find valid walls that are not on the outer border
                if (!cell.CanWalkInFrontDirection() && z > 0) candidates.Add(cell);
                if (!cell.CanWalkInBackDirection() &&z < mazeHeight - 1) candidates.Add(cell);
                if (!cell.CanWalkInLeftDirection() && x > 0) candidates.Add(cell);
                if (!cell.CanWalkInRightDirection() && x < mazeWidth - 1) candidates.Add(cell);
            }
        }

        // Randomly break walls
        int wallsBroken = 0;
        System.Random rand = new System.Random();

        while (wallsBroken < numWallsToBreak && candidates.Count > 0)
        {
            int index = rand.Next(candidates.Count);
            Cell cell = candidates[index];
            candidates.RemoveAt(index);

            // Randomly choose a wall to break
            List<WallType> possibleWalls = cell.GetIntactWalls();
            if (possibleWalls.Count > 0)
            {
                WallType wallToBreak = possibleWalls[rand.Next(possibleWalls.Count)];
                Cell neighbour = GetNeighbourCell(cell, wallToBreak);

                if (neighbour != null)
                {
                    BreakWalls(neighbour, cell);
                    wallsBroken++;
                }
            }
        }
    }

    private Cell GetNeighbourCell(Cell cell, WallType wallToBreak)
    {
        int x = ((int)cell.transform.position.x);
        int z = ((int)cell.transform.position.z);

        if (wallToBreak.Equals(WallType.FrontWall))
        {
            return mazeGrid[x, z + 1];
        }
        if (wallToBreak == WallType.BackWall)
        {
            return mazeGrid[x, z - 1];
        }
        if (wallToBreak == WallType.LeftWall)
        {
            return mazeGrid[x - 1, z];
        }
        if (wallToBreak == WallType.RightWall)
        {
            return mazeGrid[x + 1, z];
        }

        return null;
    }

    private void InitDifficultyParameters()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty");
        Debug.Log($"Generating maze with difficulty: {difficulty}");
        // TODO: set width, height, upgrades & enemies according do difficulty setting

        if (difficulty == 0)
        {
            mazeWidth = 5;
            mazeHeight = 5;
            enemyCounter = 0;
            upgradeCounter = 8;
        }
        if (difficulty == 1 || true)
        {
            mazeWidth = 10;
            mazeHeight = 10;
            enemyCounter = 0;
            upgradeCounter = 15;
        }
        if (difficulty == 2 )
        {
            mazeWidth = 15;
            mazeHeight = 15;
            enemyCounter = 6;
            upgradeCounter = 10;
        }
    }

    private void PlacePlayer()
    {
        float distance = Vector3.Distance(mazeGrid[0,0].transform.position, mazeGrid[0, 1].transform.position);

        players = new List<Player>();

        players.Add(Instantiate(player, new Vector3(0, 0, 0), Quaternion.identity));
        players.Add(Instantiate(player, new Vector3(mazeWidth - 1, 0, 0), Quaternion.identity));
        players.Add(Instantiate(player, new Vector3(0, 0, mazeHeight - 1), Quaternion.identity));
        players.Add(Instantiate(player, new Vector3(mazeWidth - 1, 0, mazeHeight - 1), Quaternion.identity));

        Debug.Log("Players on init: " + players.Count);
        GameManager.instance.SetPlayers(players);
    }

    private void PlaceEnemy()
    {
        List<Enemy> enemies = new List<Enemy>();
        while (enemyCounter > 0)
        {
            int randomZ = Random.Range(1, mazeHeight);
            int randomX = Random.Range(1, mazeWidth);

            if (IsValidEnemyPlacement(randomX, randomZ))
            {
                Vector3 position = new Vector3(randomX, 0, randomZ);

                enemies.Add(Instantiate(enemy, position, Quaternion.identity));

                enemyCounter--;
            }
        }
        
        GameManager.instance.SetEnemies(enemies);
    }

    private bool IsValidEnemyPlacement(int x, int z)
    {
        if (IsValidPlacement(mazeGrid[x, z]))
        {
            return ValidateArea(x, z);
        }
        return false;
    }

    // checks if an enemy is already placed in the surrounding cells
    private bool ValidateArea(int x, int z)
    {
        if (x + 1 < mazeWidth && x != 0 && z + 1 < mazeHeight && z != 0)
        {
            if (mazeGrid[x + 1, z - 1].isEnemyPlaced())
            {
                return false;
            }
            if (mazeGrid[x - 1, z + 1].isEnemyPlaced())
            {
                return false;
            }
        }

        if (x + 1 < mazeWidth && z + 1 < mazeHeight)
        {
            // only check when within boundries
            if (mazeGrid[x + 1, z].isEnemyPlaced())
            {
                return false;
            }
            if (mazeGrid[x, z + 1].isEnemyPlaced())
            {
                return false;
            }
            if (mazeGrid[x + 1, z + 1].isEnemyPlaced())
            {
                return false;
            }
        }
        
        if (x - 1 > 0 && z - 1 > 0)
        {
            if (mazeGrid[x - 1, z].isEnemyPlaced())
            {
                return false;
            }
            if (mazeGrid[x, z - 1].isEnemyPlaced())
            {
                return false;
            }
            if (mazeGrid[x - 1, z - 1].isEnemyPlaced())
            {
                return false;
            }

        }
      
        return true;
    }

    private bool IsNeighbourEnemy(Cell cell)
    {
        throw new System.NotImplementedException();
    }

    private void PlaceUpgrades()
    {
        List<Upgrade> upgrades = new List<Upgrade>();

        while (upgradeCounter > 0)
        {
            int randomZ = Random.Range(0, mazeHeight);
            int randomX = Random.Range(0, mazeWidth);

            if (IsValidPlacement(mazeGrid[randomX, randomZ]))
            {
                Vector3 position = new Vector3(randomX, 0, randomZ);
                Upgrade u = Instantiate(upgrade, position, Quaternion.identity);
                upgrades.Add(u);
                //mazeGrid[randomX, randomZ].PlaceUpgrades(Instantiate(upgrade, position, Quaternion.identity));
                mazeGrid[randomX, randomZ].setUpgrade(u);

                upgradeCounter--;
            }
        }

        GameManager.instance.SetUpgrades(upgrades);
    }

    private bool IsValidPlacement(Cell cell)
    {
       if (cell.isUpgradePlaced())
        {
            // there is already an upgrade placed
            return false;
        }
       if (cell.isEnemyPlaced())
        {
            // enemy already placed
            return false;
        }
        if (goalCell.Equals(cell))
        {
            // the gaol is already placed in this cell
            return false;
        }
        if (IsCornerCell(((int)cell.transform.position.x), ((int)cell.transform.position.z)))
        {
            // player start position is in this cell
            return false;
        }
        
        return true;
    }

    private void GenerateMaze(Cell previousCell, Cell currentCell)
    {
        currentCell.SetVisited();
        BreakWalls(previousCell, currentCell);

        Cell nextCell;

        do
        {
            nextCell = GetNextRandomNeighbour(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);
    }

    private Vector3 GetRandomGoalPosition()
    {
        int x, z;
        do
        {
            x = Random.Range(0, mazeWidth); 
            z = Random.Range(0, mazeHeight);
        }
        while (IsCornerCell(x, z));

        return new Vector3(x, 0, z);
    }

    bool IsCornerCell(int x, int z)
    {
        return (x == 0 && z == 0) ||
               (x == 0 && z == mazeHeight - 1) ||
               (x == mazeWidth - 1 && z == 0) ||
               (x == mazeWidth - 1 && z == mazeHeight - 1);
    }

    private void PlaceGoal()
    {
        Vector3 position = GetRandomGoalPosition();
        goalCell = mazeGrid[((int)position.x), ((int)position.z)];

        Goal g = Instantiate(goal, position, Quaternion.identity);
        GameManager.instance.SetGoal(g);

        goalCell.goal = g;
    }

    // returns a random neighbour
    private Cell GetNextRandomNeighbour(Cell currentCell)
    {
        List<Cell> unvisitedCells = GetAllUnvisitedCells(currentCell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    // checks which neighbouring cells are unvisited and returns them
    private List<Cell> GetAllUnvisitedCells(Cell currentCell)
    {
        List<Cell> unvisitedCells = new List<Cell>();

        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < mazeWidth)
        {
            Cell rightCell = mazeGrid[x + 1, z];

            if (!rightCell.isVisited())
            {
                unvisitedCells.Add(rightCell);
            }
        }

        if (x - 1 >= 0)
        {
            Cell leftCell = mazeGrid[x - 1, z];

            if (!leftCell.isVisited())
            {
                unvisitedCells.Add(leftCell);
            }
        }

        if (z + 1 < mazeHeight)
        {
            Cell frontCell = mazeGrid[x, z + 1];

            if (!frontCell.isVisited())
            {
                unvisitedCells.Add(frontCell);
            }
        }

        if (z - 1 >= 0)
        {
            Cell backCell = mazeGrid[x, z - 1];

            if (!backCell.isVisited())
            {
                unvisitedCells.Add(backCell);
            }
        }

        return unvisitedCells;
    }

    // breaks the walls between cells, depending on the path taken from previous to current cell
    private void BreakWalls(Cell previousCell, Cell currentCell)
    {
        if (previousCell == null)
        {
            // starting point
            return;
        }

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            // went from left to right cell -> break right wall of previous cell & left wall of current cell
            previousCell.BreakWall(Cell.WallType.RightWall);
            currentCell.BreakWall(Cell.WallType.LeftWall);
            return;
        }

        if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            // went from right to left cell -> break left wall of previous cell & right wall of current cell
            previousCell.BreakWall(Cell.WallType.LeftWall);
            currentCell.BreakWall(Cell.WallType.RightWall);
            return;
        }

        if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            // went up -> break front wall of previous cell & back wall of current cell
            previousCell.BreakWall(Cell.WallType.FrontWall);
            currentCell.BreakWall(Cell.WallType.BackWall);
            return;
        }

        if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            // went down -> break back wall of previous cell & front wall of current cell
            previousCell.BreakWall(Cell.WallType.BackWall);
            currentCell.BreakWall(Cell.WallType.FrontWall);
            return;
        }
    }

}
