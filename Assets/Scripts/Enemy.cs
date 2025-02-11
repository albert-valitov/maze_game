using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static Cell;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.Image;

public class Enemy : MonoBehaviour
{
    public Rigidbody rb;
    private float moveSpeed = 0.3f;
    public float cellSize = 1f;
    private Cell[,] mazeGrid;
    public Vector3 targetPosition;
    public bool chasing = false;
    public Player player;
    
    private Vector3 spawnLocation;
    private Vector3 currentDirection = Vector3.zero;
    private List<Vector3> movementHistory = new List<Vector3>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private int positionLimit = 6;
    private int historyLimit = 6;
    private Cell lastSeenCell;


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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mazeGrid = GameManager.instance.mazeGrid;

        spawnLocation = SnapToGrid(transform.position);
        transform.position = spawnLocation;
        positionHistory.Add(spawnLocation);

        targetPosition = GetNextWayPoint(transform.position);

        // acts like a noise detactor, if a player enters the sphere, the enemy will be alarmed and move towards the point where the collision happened
        SphereCollider playerDetector = GetComponent<SphereCollider>();
    }

    private void Move(Vector3 target)
    {
        if (chasing)
        {
            moveSpeed = 0.8f;
        } else
        {
            moveSpeed = 0.2f;
        }

        Vector3 newPosition = Vector3.MoveTowards(transform.position, target, Time.deltaTime * moveSpeed);

        transform.position = newPosition;

        //rb.MovePosition(newPosition);
    }

    void Update()
    {
        if (chasing) 
        {
            if (CanSeePlayer(player))
            {
                targetPosition = player.transform.position;
                lastSeenCell = GetCellFromPosition(player.transform.position);
            } 
            else
            {
                chasing = false;
                targetPosition = SnapToGrid(transform.position);
            }
        } 

        if (transform.position == targetPosition && !chasing)
        {
            UpdatePostionHistory(targetPosition);
            targetPosition = GetNextWayPoint(transform.position);
        }

        Move(targetPosition);       
    }

    private void UpdatePostionHistory(Vector3 position)
    {
        positionHistory.Add(position);

        if (positionHistory.Count() > positionLimit)
        {
            // FIFO
            positionHistory.RemoveAt(0);
        }
    }

    public Vector3 getCurrentDirection()
    {
        return currentDirection;
    }

    private void FixedUpdate()
    {
       
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        // snap position to the nearest cell center
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;

        return new Vector3(x, position.y, z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            if (!player.IsInvulnerable())
            {
                Destroy(player.gameObject);
            }

        }
    }

    /*
     * Check if enemy can see player. True if no obstacle between between enemy and player. False if wall is bet
     */
    bool CanSeePlayer(Player player)
    {
        if (player == null)
        {
            chasing = false;
            return false;
        }

        if (GameManager.instance.IsPlayerSafeSpace(GetCellFromPosition(player.transform.position)))
        {
            return false;
        }

        if (player.IsInvulnerable())
        {
            // do not chase invulnerable players
            return false;
        }

        Vector3 direction = (player.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        RaycastHit hit;
        ;

        if (Physics.Raycast(transform.position, direction, out hit, distance))
        {
            bool canSeePlayer = hit.collider.CompareTag("Player");
            string tag = hit.collider.tag;
            return canSeePlayer;
        }

        return false;
    }

    Cell GetCellFromPosition(Vector3 position)
    {
        
        return GameManager.instance.GetCell(position);
    }

    private void LookForPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance.IsPlayerSafeSpace(GetCellFromPosition(other.transform.position)))
            {
                // do not chase players when in safe space
                return;
            }

            if (chasing)
            {
                // already chasing someone
                return;
            }

            // start chasing player if can see him else go to cell that player was in when the player triggered
            if (CanSeePlayer(other.GetComponent<Player>()))
            {
                chasing = true;
                this.player = other.GetComponent<Player>();
                lastSeenCell = GetCellFromPosition(other.transform.position);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        LookForPlayer((Collider)other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!chasing)
        {
            LookForPlayer((Collider)other);
        }
    }

    private Vector3 GetNextWayPoint(Vector3 position)
    {
        List<Vector3> allowedDirections = new List<Vector3>();
        position = SnapToGrid(position);
        Cell currentCell = mazeGrid[((int)position.x), ((int)position.z)];

        foreach (Vector3Int direction in possibleDirections.Values)
        {
            if (!currentCell.CanWalk(direction))
            {
                continue;
            }
            
            Cell targetCell = mazeGrid[((int)position.x + direction.x), ((int)position.z + direction.z)];

            if (GameManager.instance.IsPlayerSafeSpace(targetCell))
            {
                continue;
            }

            if (direction == -currentDirection)
            {
                continue;
            }
            allowedDirections.Add(direction);
        }
        Vector3 nextBestDirection = GetNextBestDirection(allowedDirections);

        currentDirection = nextBestDirection == Vector3.zero ? -currentDirection : nextBestDirection;

        return currentCell.transform.position + currentDirection;
    }

    /*
     * Determines which direction should be taken based on the last visited cells and directions taken
     */
    private Vector3 GetNextBestDirection(List<Vector3> directions)
    {
        Dictionary<Vector3, float> directionWeights = new Dictionary<Vector3, float>();
        List<Vector3> shuffledDirections = directions.OrderBy(x => Random.value).ToList();

        foreach (Vector3 dir in possibleDirections.Values)
        {
            // init all directions with base weight
            directionWeights[dir] = 1f;
        }

        Vector3 currentPosition = SnapToGrid(transform.position);

        foreach (Vector3 dir in movementHistory)
        {
            if (dir == Vector3.zero)
            {
                continue;
            }

            if (directionWeights.ContainsKey(dir))
            {
                // reduce probability of overused directions
                directionWeights[dir] *= 0.5f; 
            }

            if (!positionHistory.Contains(currentPosition + dir))
            {
                // encourage to take a direction to a cell that has not been visited yet/in a while
                directionWeights[dir] += 1f;
            } else
            {
                directionWeights[dir] *= 0.2f;
            }
        }


        Vector3 bestDirection = Vector3.zero;
        float bestValue = 0f;

        foreach (Vector3 dir in shuffledDirections)
        {
            if (directionWeights[dir] > bestValue)
            {
                bestValue = directionWeights[dir];
                bestDirection = dir;
            }
        }

        movementHistory.Add(bestDirection);

        if (movementHistory.Count > historyLimit)
        {
            // FIFO
            movementHistory.RemoveAt(0);
        }
        
        return bestDirection;
    }

    
    //private bool IsPlayerInDirection(Vector3 direction, Vector3 position)
    //{
    //    // Check for a wall in the given direction using a raycast
    //    RaycastHit hit;
    //    Vector3 rayOrigin = position + Vector3.up * 0.5f;

    //    //Debug.DrawRay(rayOrigin, direction, UnityEngine.Color.red, 5.0f); 

        
    //    if (Physics.Raycast(position, direction, out hit, cellSize))
    //    {
    //        bool playerSeen = hit.collider.CompareTag("Player");

    //        //Debug.Log("Object in the way: " + hit.collider.tag);

    //        return playerSeen;
    //    }
    //    return false;
    //}
}
