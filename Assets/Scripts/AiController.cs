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
        pathFinder = new PathFinder(mazeGrid);
        nextWaypointIndex = 1;

        FindPathForPlayers();
    }

    private Player FindPathForPlayers()
    {
        int shortestPathCount = int.MaxValue;

        for(int i = 0; i < players.Count; i++)
        {
            Cell start = mazeGrid[((int)players[i].transform.position.x), ((int)players[i].transform.position.z)];
            Cell goalCell = GetGoalCell();

            List<Cell> path = pathFinder.FindPath(start, goalCell);
            players[i].SetPathToGoal(path);

            if (path.Count < shortestPathCount)
            {
                shortestPathCount = path.Count;
                focusedPlayer = players[i];
            }
        }
        nextWaypointIndex = 0;
        return focusedPlayer;
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
            focusedPlayer = FindPathForPlayers();
            Debug.Log("NEW FOCUSED PLAYER FOUND");
        }

        List<Cell> path = focusedPlayer.GetPathToGoal();

        // if there are no waypoints or the player has reached the final one, stop moving
        if (path == null || nextWaypointIndex >= path.Count)
        {
            return;
        }
    
        Vector3 targetPosition = path[nextWaypointIndex].transform.position;

        // move focused player to target position and all other players in that direction
        MoveAllPlayers(targetPosition);
        
        Vector3 moveDirection = (targetPosition - focusedPlayer.transform.position).normalized;

        // check if focused player is stuck
        if (IsPlayerStuck(moveDirection))
        {
            SnapPlayerToAxis(moveDirection * -1);
        }

        // check if the player has reached the waypoint
        if (focusedPlayer.transform.position == targetPosition)
        {
            // go to the next waypoint
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

            Cell currentCell = GetCurrentCellOfPlayer(player);

            List<Cell> path = pathFinder.FindPath(currentCell, GetGoalCell());
            player.SetPathToGoal(path);

            focusedPlayer = player;
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
     * snaps player back to the middle of the cell if he is somehow stuck
     */
    private void SnapPlayerToAxis(Vector3 direction)
    {
        Vector3 currentPosition = focusedPlayer.transform.position;
        Vector3 targetPosition = currentPosition + direction * 0.3f;
        focusedPlayer.MoveToTarget(targetPosition);
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
