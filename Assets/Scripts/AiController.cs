using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using static Cell;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.UIElements;
using System.Linq;
using System.IO;
using TMPro;


public class AIController : MonoBehaviour
{
    public List<Player> players;
    public List<Enemy> enemies;
    public List<Upgrade> upgrades;
    public Goal goal;
    public Cell[,] mazeGrid;
    private int width;
    private int height;
    private Player focusedPlayer;
    private int nextWaypointIndex;
    public float waypointThreshold = 0.1f;
    private float moveSpeed = 1.5f;
    public float cellSize = 1f;
    private PathFinder pathFinder;

    Dictionary<WallType, Vector3Int> possibleDirections = new Dictionary<WallType, Vector3Int>
        {
            {WallType.FrontWall, new Vector3Int(0, 0, 1)},
            {WallType.BackWall, new Vector3Int(0, 0, -1)},
            {WallType.LeftWall, new Vector3Int(-1, 0, 0)},
            {WallType.RightWall, new Vector3Int(1, 0, 0)}
        };

    Dictionary<Vector3Int, WallType> directionToWallType = new Dictionary<Vector3Int, WallType>
        {
            {new Vector3Int(0, 0, 1), WallType.FrontWall},
            {new Vector3Int(0, 0, -1), WallType.BackWall},
            {new Vector3Int(-1, 0, 0), WallType.LeftWall},
            {new Vector3Int(1, 0, 0), WallType.RightWall}
        };

    private void Start()
    {
        
    }

    public void Init(List<Player> players, List<Enemy> enemies, List<Upgrade> upgrades, Goal goal, Cell[,] mazeGrid)
    {
        this.players = players;
        this.enemies = enemies;
        this.upgrades = upgrades;
        this.goal = goal;
        this.mazeGrid = mazeGrid;

        width = mazeGrid.GetLength(0);
        height = mazeGrid.GetLength(1);
        pathFinder = new PathFinder(mazeGrid, enemies);
        nextWaypointIndex = 1;

        FindPathForPlayers();
    }

    private void FindPathForPlayers()
    {
        int shortestPathCount = int.MaxValue;

        for(int i = 0; i < players.Count; i++)
        {
            Cell start = mazeGrid[((int)players[i].startPosition.x), ((int)players[i].startPosition.z)];
            Cell goalCell = GetGoalCell();

            List<Cell> path = pathFinder.FindShortestPath(start, goalCell);
            //List<Cell> path = FindPathForPlayer(players[i]);
            //path = OptimizePathWithUpgrades(path, players[i]);
            players[i].SetPathToGoal(path);

            if (path.Count < shortestPathCount)
            {
                shortestPathCount = path.Count;
                focusedPlayer = players[i];
            }
        }
    }

    private Cell GetGoalCell() {
        return mazeGrid[((int)goal.transform.position.x), ((int)goal.transform.position.z)];
    }

    void FixedUpdate()
    {
        if (players == null || players.Count == 0) 
        {
            // no players left - game over
            return;
        }

        if (focusedPlayer == null)
        {
            // player was either destroyed by an enemy or reached the goal
            Debug.Log("LOOKING FOR NEW FOCUSED PLAYER");
            focusedPlayer = GetNewFocusedPlayer();
            Debug.Log("NEW FOCUSED PLAYER FOUND");
        }

        List<Cell> path = focusedPlayer.GetPathToGoal();

        // if there are no waypoints or the player has reached the final one, stop moving
        if (path == null || nextWaypointIndex >= path.Count)
        {
            return;
        }

        //CheckForUpgradeMoves(path[nextWaypointIndex]);        

        Vector3 targetPosition = path[nextWaypointIndex].transform.position;

        // move focused player to target position and all other players in that direction
        MoveAllPlayers(targetPosition);
        
        Vector3 moveDirection = (targetPosition - focusedPlayer.transform.position).normalized;

        // check if focused player is stuck
        if (IsPlayerStuck(moveDirection))
        {
            SnapPlayerToAxis(moveDirection * -1);
        }

        // Check if the player has reached the waypoint
        if (focusedPlayer.transform.position == targetPosition)
        {
            // Go to the next waypoint
            nextWaypointIndex++;
        }
    }

    /*
     * When a player, who is not the focused player walks into an upgrade the ai has to focus on that player, since all others can not move
     */
    public void ChangeFocusedPlayer(Player player)
    {
        if (focusedPlayer != player)
        {
            Debug.Log("NEW FOCUSED PLAYER CHANGE DUE TO UPGRADE ACTIVATION");
            FindNearestWayPoint(player);
            focusedPlayer = player;
        }
    }

    /*
     * Check if target cell is part of a path to an upgrade and if upgrade is present. 
     * If not, the path to the upgrade is removed and player continues on the main path to goal 
     */
    private void CheckForUpgradeMoves(Cell targetCell)
    {
        List<List<Cell>> cells = focusedPlayer.GetUpgradeCells();
        foreach (List<Cell> cell in cells)
        {
            if (cell.Contains(targetCell))
            {
                if (!cell.Last().isUpgradePlaced() && focusedPlayer.GetPathToGoal()[nextWaypointIndex].transform.position != cell.Last().transform.position)
                {
                    // upgrade was already picked up by other player - skip that path
                    List<Cell> pathToGoal = focusedPlayer.GetPathToGoal();
                    focusedPlayer.GetPathToGoal().RemoveAll(x => cell.Contains(x));
                    nextWaypointIndex++;
                }
            }
        }
    }

    private List<Player> GetEndangeredPlayers(Vector3 targetPosition)
    {
        List<Player> endangeredPlayers = new List<Player>();

        Vector3Int direction = Vector3Int.FloorToInt((targetPosition - focusedPlayer.transform.position).normalized);

        foreach (Player player in players)
        {
            //Cell currentCell = GetCurrentCellOfPlayer(player);

            //if (!currentCell.CanWalk(directionToWallType[direction]))
            //{
            //    // can not walk in that direction - no danger
            //    continue;
            //}
            //if (IsEnemyPatrollingInCell(currentCell))
            //{

            //}
        }

        return endangeredPlayers;
    }

    private bool IsNextMoveDangerous()
    {
        foreach (Player player in players)
        {
            Cell currentCell = GetCurrentCellOfPlayer(player);
            if (IsEnemyPatrollingInCell(currentCell))
            {

            }
        }
        return false;
    }

    private bool IsEnemyPatrollingInCell(Cell cell)
    {
        foreach (Enemy enemy in enemies)
        {
            List<Cell> patrollingCells = GetEnemyPatrollingCells(enemy);
            if (patrollingCells.Contains(cell)) 
            {
                return true;
            }
        }
        return false;
    }

    private List<Cell> GetEnemyPatrollingCells(Enemy enemy)
    {
        List<Cell> cells = new List<Cell>();
        List<Vector3> waypoints = enemy.GetPatrollingWayPoints();

        foreach (Vector3 waypoint in waypoints)
        {
            cells.Add(mazeGrid[((int)waypoint.x), ((int)waypoint.z)]);
        }

        return cells;
    }

    private Cell GetCurrentCellOfPlayer(Player player)
    {
        Vector3 position = SnapToGrid(player.transform.position);
        
        return mazeGrid[((int)position.x), ((int)position.z)];
    }

    private Player GetNewFocusedPlayer()
    {
        Player newFocusedPlayer = null;

        int shortestPathCount = int.MaxValue;

        //RemoveDestroyedPlayers();

        foreach (var player in players)
        {            
            if (player.GetPathToGoal().Count < shortestPathCount)
            {
                shortestPathCount = player.GetPathToGoal().Count;
                newFocusedPlayer = player;
            }
        }

        //FindNearestWayPoint(newFocusedPlayer);
        Vector3 playerPostion = SnapToGrid(newFocusedPlayer.transform.position);
        Cell start = mazeGrid[((int)playerPostion.x), ((int)playerPostion.z)];
        List<Cell> path = pathFinder.FindShortestPath(start, GetGoalCell());

        newFocusedPlayer.SetPathToGoal(path);
        nextWaypointIndex = 0;

        return newFocusedPlayer;
    }

    private void FindNearestWayPoint(Player player)
    {
        Vector3 position = SnapToGrid(player.transform.position);

        List<Cell> cells = player.GetPathToGoal();
        Cell newTarget = null;

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].transform.position == position)
            {
                // next cell is the target
                if (i > 0 && cells[i - 1].isUpgradePlaced())
                {
                    // take upgrade if cell before current cell contains one
                    newTarget = cells[i - 1];
                    nextWaypointIndex = i - 1;
                    break;
                } else
                {
                    newTarget = cells[i];
                    nextWaypointIndex = i + 1;
                    break;
                }
                
            }
        }

        if (newTarget == null)
        {
            // player has no viable path to the goal from this position - need to calculate path to spawnlocation
            List<Cell> pathToSpawn = new List<Cell>();
            Vector3 a = cells[0].transform.position;
            FindPath(mazeGrid[((int)position.x), ((int)position.z)], new HashSet<Cell>(), pathToSpawn, cells[0]);
            for (int i = 1; i < cells.Count; i++)
            {
                // add path to goal to the new path
                pathToSpawn.Add(cells[i]);
            }

            player.SetPathToGoal(pathToSpawn);

            // players path got longer - get a new focused player if there is one with a shorter path to the goal
            GetNewFocusedPlayer();
        }
    }
    private Vector3 SnapToGrid(Vector3 position)
    {
        // snap position to the nearest cell center
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;

        return new Vector3(x, position.y, z);
    }

    private bool IsPlayerStuck(Vector3 direction)
    {
        bool wallAhead = false;
        Vector3 position = focusedPlayer.transform.position;

        RaycastHit hit;
        Vector3 rayOrigin = position + Vector3.up * 0.5f;

        if (Physics.Raycast(position, direction, out hit, 0.1f))
        {
            wallAhead = hit.collider.CompareTag("Wall");
        }
        
        return wallAhead;
    }

    /*
     * Checks if an upgrade exists within 2 cells along the path
     */
    private bool IsUpgradeAlongThePath(List<Cell> path, int i)
    {
        if (i < path.Count - 1)
        {
            Cell firstCell = path[i + 1];
            if (firstCell != null && firstCell.isUpgradePlaced())
            {
                return true;
            }

            if (i < path.Count - 2)
            {
                Cell secondCell = path[i + 2];
                if (secondCell != null && secondCell.isUpgradePlaced())
                {
                    return true;
                }
            }

        }
        // no upgrades within the 2 next cells
        return false;
    }

    /*
     * Look for upgrades along the way to the goal. 
     * Max 2 cells off the path and only until there are more than 3 cells left to the goal.
     * 
     */
    private List<Cell> OptimizePathWithUpgrades(List<Cell> path, Player player)
    {
        List<Cell> optimizedPath = new List<Cell>();

        for (int i = 0; i < path.Count; i++)
        {
            Cell cell = path[i];
            optimizedPath.Add(cell);

            if (i >= path.Count - 3)
            {
                // do not look for upgrades when near the goal - time waste
                continue;
            }

            if (IsUpgradeAlongThePath(path, i))
            {
                // do not search for upgrades if there is an upgrade within the next two cells along the main path
                continue;
            }

            List<Cell> nearestUpgradePath = FindNearbyUpgrade(cell, optimizedPath, path);

            if (nearestUpgradePath != null && nearestUpgradePath.Count > 0)
            {
                optimizedPath.Add(nearestUpgradePath[0]);

                if (nearestUpgradePath.Count == 2)
                {
                    // add path to second cell and the path back to the first cell
                    optimizedPath.Add(nearestUpgradePath[1]);
                    optimizedPath.Add(nearestUpgradePath[0]);
                }

                // add the destination (where upgrade is placed) to the list
                player.AddUpgradeCell(nearestUpgradePath);
                // return to the main path
                optimizedPath.Add(cell); 
            }
        }
        if (optimizedPath.Count == 0)
        {
            return path;
        }
        // optimised path with potential extra upgrades included along the way
        return optimizedPath;
    }

    private List<Cell> FindNearbyUpgrade(Cell cell, List<Cell> optimizedPath, List<Cell> originalPath)
    {
        if (cell.isUpgradePlaced())
        {
            return null;
        } 

        List<Cell> pathToUpgrade = new List<Cell>();

        foreach (var dir in possibleDirections)
        {
            int x = ((int)cell.transform.position.x);
            int z = ((int)cell.transform.position.z);

            if (IsValidCell(dir.Value, cell) && cell.CanWalk(dir.Key))
            {
                int nextX = x + dir.Value.x;
                int nextZ = z + dir.Value.z;

                Cell nextCell = mazeGrid[nextX, nextZ];

                if (optimizedPath.Contains(nextCell) || originalPath.Contains(nextCell))
                {
                    // do not go back to an already visited cell
                    continue;
                }

                if (nextCell.isUpgradePlaced())
                {
                    pathToUpgrade.Add(nextCell);

                    // do not need to look for further upgrades, one is enough
                    return pathToUpgrade;
                }
                foreach (var nextDir in possibleDirections)
                {
                    if (IsValidCell(nextDir.Value, nextCell) && nextCell.CanWalk(nextDir.Key))
                    {
                        int nextX2 = nextX + nextDir.Value.x;
                        int nextZ2 = nextZ + nextDir.Value.z;

                        if (originalPath.Contains(nextCell))
                        {
                            // path to upgrade is through the main path - search in next cycle or else duplicate paths would exist
                            continue;
                        }

                        Cell nextCell2 = mazeGrid[nextX2, nextZ2];

                        if (optimizedPath.Contains(nextCell2) || originalPath.Contains(nextCell2))
                        {
                            // do not go back to an already visited cell
                            continue;
                        }

                        if (nextCell2.isUpgradePlaced())
                        {
                            pathToUpgrade.Add(nextCell);
                            pathToUpgrade.Add(nextCell2);

                            return pathToUpgrade;
                        }
                    }
                }
            }
        }
        // no upgrade found
        return pathToUpgrade; 
    }

    /*
     * snaps player back to the middle of the cell if he is somehow stuck
     */
    private void SnapPlayerToAxis(Vector3 direction)
    {
        Vector3 currentPosition = focusedPlayer.transform.position;
        Vector3 targetPosition = currentPosition + direction * 0.3f;
        focusedPlayer.MoveToTarget(targetPosition);
    }

    private List<Cell> FindPathForPlayer(Player player)
    {
        int x = ((int)player.transform.position.x);
        int z = ((int)player.transform.position.z);

        List<Cell> path = new List<Cell>();
        FindPath(mazeGrid[x, z], new HashSet<Cell>(), path);

        return path;
    }

    private void MoveAllPlayers(Vector3 targetPosition)
    {
        GetEndangeredPlayers(targetPosition);
        if (IsNextMoveDangerous())
        {

        }
        Vector3 direction = (targetPosition - focusedPlayer.transform.position).normalized;

        focusedPlayer.MoveToTarget(targetPosition);

        foreach (var player in players)
        {
            if (player == focusedPlayer)
            {
                continue;
            }

            player.Move(direction);
        }
    }

    private bool FindPath(Cell currentCell, HashSet<Cell> visited, List<Cell> path, Cell targetCell)
    {
        int x = ((int)currentCell.transform.position.x);
        int z = ((int)currentCell.transform.position.z);

        if (currentCell.transform.position == targetCell.transform.position)
        {
            path.Add(mazeGrid[x, z]);

            return true;
        }

        visited.Add(currentCell);

        path.Add(mazeGrid[x, z]);

        foreach (var dir in possibleDirections)
        {
            if (IsValidCell(dir.Value, currentCell))
            {
                int nextX = x + dir.Value.x;
                int nextZ = z + dir.Value.z;

                Cell nextCell = mazeGrid[nextX, nextZ];

                if (!visited.Contains(nextCell) && currentCell.CanWalk(dir.Key))
                {
                    if (FindPath(nextCell, visited, path, targetCell))
                    {
                        return true;
                    }
                }
            }
        }

        path.Remove(mazeGrid[x, z]);

        return false;
    }

    private bool FindPath(Cell currentCell, HashSet<Cell> visited, List<Cell> path)
    {
        int x = ((int)currentCell.transform.position.x);
        int z = ((int)currentCell.transform.position.z);

        if (currentCell.isGoalPlaced())
        {
            path.Add(mazeGrid[x, z]);

            return true;
        }

        visited.Add(currentCell);

        path.Add(mazeGrid[x, z]);

        Dictionary<WallType, Vector3Int> nextPossibleDirection = new Dictionary<WallType, Vector3Int>
        {
            {WallType.FrontWall, new Vector3Int(0, 0, 1)},
            {WallType.BackWall, new Vector3Int(0, 0, -1)},
            {WallType.LeftWall, new Vector3Int(-1, 0, 0)},
            {WallType.RightWall, new Vector3Int(1, 0, 0)}
        };

        foreach (var dir in nextPossibleDirection)
        {
            if (IsValidCell(dir.Value, currentCell)) {
                int nextX = x + dir.Value.x;
                int nextZ = z + dir.Value.z;

                Cell nextCell = mazeGrid[nextX, nextZ];

                if (!visited.Contains(nextCell) && currentCell.CanWalk(dir.Key))
                {
                    if (FindPath(nextCell, visited, path))
                    {
                        return true;
                    }
                }                
            }
        }

        path.Remove(mazeGrid[x, z]);

        return false;
    }
    private bool IsValidCell(Vector3Int direction, Cell currentCell)
    {
        int currentX = ((int)currentCell.transform.position.x);
        int currentZ = ((int)currentCell.transform.position.z);

        currentX += direction.x;
        currentZ += direction.z;

        if (currentZ < 0 || currentX < 0)
        {
            return false;
        }

        return currentX >= 0 && currentX < width && currentZ >= 0 && currentZ < height;
    }
    public void SetPlayers(List<Player> players)
    {
        this.players = players;
    }
    public void SetEnemies(List<Enemy> enemies)
    {
        this.enemies = enemies;
    }
    public void SetGoal(Goal goal)
    {
        this.goal = goal;
    }

    public void SetMazeGrid(Cell[,] mazeGrid)
    {
        this.mazeGrid = mazeGrid;
    }

    public void SetUpgrades(List<Upgrade> upgrade)
    {
        this.upgrades = upgrade;
    }
}
