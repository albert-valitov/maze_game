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
using static UnityEngine.GraphicsBuffer;


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
    public float waypointThreshold = 0.1f;
    public float cellSize = 1f;
    private PathFinder pathFinder;
    private Cell lastWayPoint;

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

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
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
        pathFinder = new PathFinder(mazeGrid);

        FindPathForPlayers();
    }

    private Player FindPathForPlayers()
    {
        int shortestPathCount = int.MaxValue;

        for(int i = 0; i < players.Count; i++)
        {
            
            Cell start = GetCurrentCellOfPlayer(players[i]);
            Cell goalCell = GetGoalCell();

            List<Cell> path = pathFinder.FindPath(start, goalCell);
            players[i].SetPathToGoal(path);

            if (path.Count < shortestPathCount)
            {
                shortestPathCount = path.Count;
                focusedPlayer = players[i];
            }
        }
        return focusedPlayer;
    }

    private Cell GetGoalCell() {
        return mazeGrid[((int)goal.transform.position.x), ((int)goal.transform.position.z)];
    }

    void Update()
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
            focusedPlayer = FindPathForPlayers();
            Debug.Log("NEW FOCUSED PLAYER FOUND");
        }
        
        List<Cell> path = pathFinder.FindPath(lastWayPoint ?? GetCurrentCellOfPlayer(focusedPlayer), GetGoalCell());
        focusedPlayer.SetPathToGoal(path);

        // if there are no waypoints or the player has reached the final one, stop moving
        if (path == null)
        {
            return;
        }

        Vector3 targetPosition = path.Count < 2 ? path.First().transform.position : path[1].transform.position;
        Vector3 direction = (targetPosition - focusedPlayer.transform.position).normalized;

        // move focused player to target position and all other players in that direction
        MoveAllPlayers(direction);
        
        //Vector3 moveDirection = (targetPosition - focusedPlayer.transform.position).normalized;

        //// check if focused player is stuck
        //if (IsPlayerStuck(moveDirection))
        //{
        //    SnapPlayerToAxis(moveDirection * -1);
        //}

        // check if the player has reached the waypoint
        
        if (focusedPlayer.transform.position == targetPosition)
        {
            // go to the next waypoint
            
            lastWayPoint = path.Count < 2 ? path.First() : path[1]; 
        }
    }

    /*
     * Change the player the AI should focus on. Returns true if player was changed, false if player is already the focused player. 
     */
    public bool ChangeFocusedPlayer(Player player)
    {
        if (focusedPlayer != player)
        {
            //Debug.Log("NEW FOCUSED PLAYER CHANGE DUE TO UPGRADE ACTIVATION");

            Cell currentCell = GetCurrentCellOfPlayer(player);

            List<Cell> path = pathFinder.FindPath(currentCell, GetGoalCell());
            player.SetPathToGoal(path);

            focusedPlayer = player;

            return true;
        }

        return false; 
    }

    private List<Player> GetEndangeredPlayers(Vector3 direction)
    {
        List<Player> endangeredPlayers = new List<Player>();

        foreach (Player player in players)
        {
            if (IsNextMoveDangerous(direction, player))
            {
                endangeredPlayers.Add(player);
            }
        }

        return endangeredPlayers;
    }

    private Cell GetNeighbourCell(Cell cell, Vector3Int direction)
    {
        int x = ((int)cell.transform.position.x) + direction.x;
        int z = ((int)cell.transform.position.z) + direction.z;

        int mazeWidth = mazeGrid.GetLength(0);
        int mazeHeight = mazeGrid.GetLength(1);

        if (x < 0 || x > mazeWidth - 1 || z < 0 || z > mazeHeight - 1)
        {
            return null;
        }

        return mazeGrid[x, z];
    }

    private bool IsEnemyInCell(Enemy enemy, Cell cell)
    {
        Vector3 position = SnapToGrid(enemy.transform.position);

        return position == cell.transform.position;
    }

    private bool IsEnemyAround(Cell cell, Player player)
    {
        foreach (Enemy enemy in enemies)
        {
            if (IsEnemyInCell(enemy, cell))
            {
                return true;
            }
        }

        foreach (var dir in possibleDirections)
        {
            if (cell.CanWalk(dir.Value))
            {
                Cell neighbour = mazeGrid[((int)cell.transform.position.x) + dir.Value.x, ((int)cell.transform.position.z) + dir.Value.z];

                foreach (Enemy enemy in enemies)
                {
                    if (IsEnemyInCell(enemy, neighbour))
                    {
                        if (enemy.CanSeePlayer(player))
                        {
                            // enemy is chasing player
                            return true;
                        }
                        if (WillEnemySeePlayer(enemy, player))
                        {
                            // enemy is walking towards player and will see him if player walks into that direction
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool WillEnemySeePlayer(Enemy enemy, Player player)
    {
        Vector3 dirTowardsPlayer = GetCurrentCellOfPlayer(player).transform.position - enemy.transform.position;
        if (enemy.getCurrentDirection() == dirTowardsPlayer)
        {
            // enemy is walking towards player
            return true;
        }

        return false;
    }

    private bool IsNextMoveDangerous(Vector3 direction, Player player)
    {
        bool isDangerous = false;        

        Vector3Int cellToCellDirection = Vector3Int.FloorToInt((focusedPlayer.GetPathToGoal().First().transform.position - GetCurrentCellOfPlayer(focusedPlayer).transform.position).normalized);

        Cell currentCell = GetCurrentCellOfPlayer(player);
        Cell target = currentCell;

        if (cellToCellDirection == Vector3.zero)
        {
            return false;
        }

        if (currentCell.CanWalk(cellToCellDirection))
        {
            target = GetNeighbourCell(currentCell, cellToCellDirection);

            if (target == null)
            {
                // should not happen but safety first 
                target = currentCell;
            }            
        }

        if (IsEnemyAround(target, player))
        {
            // enemy is around the target location
            isDangerous = true;
        }

        if (GameManager.instance.IsPlayerSafeSpace(target))
        {
            // player is in safe space (spawn location)
            isDangerous = false;
        }        

        return isDangerous;
    }

    private Cell GetCurrentCellOfPlayer(Player player)
    {
        Vector3 position = SnapToGrid(player.transform.position);
        
        return mazeGrid[((int)position.x), ((int)position.z)];
    }

    //private Player GetNewFocusedPlayer()
    //{
    //    FindPathForPlayers();
    //    //Player newFocusedPlayer = null;

    //    //int shortestPathCount = int.MaxValue;

    //    //foreach (var player in players)
    //    //{
    //    //    List<Cell> path = FindPathForPlayer(player);
    //    //    player.SetPathToGoal(path);

    //    //    if (player.GetPathToGoal().Count < shortestPathCount)
    //    //    {
    //    //        shortestPathCount = player.GetPathToGoal().Count;
    //    //        newFocusedPlayer = player;
    //    //    }
    //    //}

    //    nextWaypointIndex = 1;

    //    return newFocusedPlayer;
    //}

    private List<Cell> FindPathForPlayer(Player player)
    {
        Vector3 playerPostion = SnapToGrid(player.transform.position);
        Cell currentCell = mazeGrid[((int)playerPostion.x), ((int)playerPostion.z)];

        return pathFinder.FindPath(currentCell, GetGoalCell());
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        // snap position to the nearest cell center
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;

        return new Vector3(x, 0, z);
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
     * snaps player back to the middle of the cell if he is somehow stuck
     */
    private void SnapPlayerToAxis(Vector3 direction)
    {
        Vector3 currentPosition = focusedPlayer.transform.position;
        Vector3 targetPosition = currentPosition + direction * 0.3f;
        focusedPlayer.MoveToTarget(targetPosition);
    }

    private void ChangePlayer(List<Player> endangeredPlayers)
    {
        //foreach (Player player in endangeredPlayers)
        //{
        //    if (ChangeFocusedPlayer(player))
        //    {
        //        return;
        //    }
        //}

    }

    private void MoveAllPlayers(Vector3 direction)
    {
        //List<Player> endangeredPlayers = GetEndangeredPlayers(direction);

        //if (endangeredPlayers.Count > 0)
        //{
        //    ChangePlayer(endangeredPlayers);
        //}
        //else
        //{ 
        //    focusedPlayer.MoveToTarget(direction);

        //    foreach (var player in players)
        //    {
        //        if (player == focusedPlayer)
        //        {
        //            continue;
        //        }

        //        player.Move(direction);
        //    }
        //}
        
        //focusedPlayer.MoveToTarget(direction);

        foreach (var player in players)
        {
            //if (player == focusedPlayer)
            //{
            //    continue;
            //}

            player.Move(direction);
        }
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
