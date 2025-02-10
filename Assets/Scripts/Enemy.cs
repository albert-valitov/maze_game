using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static Cell;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.Image;

public class Enemy : MonoBehaviour
{
    public Rigidbody rb;
    public float moveSpeed = 1.9f;
    public float cellSize = 1f;
    private Cell[,] mazeGrid;
    public Vector3 targetPosition;
    
    private Vector3 spawnLocation;
    private Vector3 currentDirection = Vector3.zero;
    private List<Vector3> movementHistory = new List<Vector3>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private int positionLimit = 6;
    private int historyLimit = 6;


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
    }

    private void Move(Vector3 target)
    {
        Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        rb.Move(newPosition, Quaternion.identity);
    }

    void Update()
    {
        Move(targetPosition);

        if (rb.position == targetPosition)
        {
            UpdatePostionHistory(targetPosition);

            targetPosition = GetNextWayPoint(transform.position);
        }        
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

    private Vector3 GetRandomValidDirection(Vector3 currentDirection, Vector3 position)
    {
        // Possible movement directions
        Vector3Int[] directions = { Vector3Int.forward, Vector3Int.right, Vector3Int.left, Vector3Int.back};

        foreach (Vector3Int direction in directions)
        {
            Vector3 targetPosition = position + direction;

            Cell currentCell = mazeGrid[((int)position.x), ((int)position.z)];
            if (!currentCell.CanWalk(direction))
            {
                // wall blocks the way
                continue;
            }

            Cell targetCell = mazeGrid[((int)targetPosition.x), ((int)targetPosition.z)];

           

            // look for next posible direction
            if (!IsObjectInDirection(direction, position) && direction != - currentDirection)
            {
                
                if (GameManager.instance.IsPlayerSafeSpace(targetCell))
                {
                    // do not walk into player safe space
                    continue;
                }
                
                // no object in the way and no player safe space
                return direction;
            }
        }

        return - currentDirection;
    }

    private bool IsObjectInDirection(Vector3 direction, Vector3 position)
    {
        // Check for a wall in the given direction using a raycast
        RaycastHit hit;
        Vector3 rayOrigin = position + Vector3.up * 0.5f;

        //Debug.DrawRay(rayOrigin, direction, UnityEngine.Color.red, 5.0f); 

        // check at the center of the cell if wall/upgrade/goal is ahead - otherwise movement could be locked due to running into the small walls on the edges of a cell
        if (Physics.Raycast(position, direction, out hit, cellSize))
        {
            //bool wallAhead = hit.collider.CompareTag("Wall");
            bool upgradeAhead = hit.collider.CompareTag("Upgrade");
            bool goalAhead = hit.collider.CompareTag("Goal");

            //Debug.Log("Wall ahead: " + wallAhead + " || Position: " + position + " || Direction: " + direction);
            //Debug.Log("Upgrade ahead: " + upgradeAhead + " || Position: " + position + " || Direction: " + direction);
            //Debug.Log("Goal ahead: " + goalAhead + " || Position: " + position + " || Direction: " + direction);

            //Debug.Log("Object in the way: " + hit.collider.tag);

            // players are no obstacles therefore the enemy does not need to evade them
            return  upgradeAhead || goalAhead;
        }
        return false;
    }
}
