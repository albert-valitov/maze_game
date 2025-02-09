using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 1.9f;          // Speed of movement
    public float cellSize = 1f;          // Size of one cell in the maze (default 1 unit)
    public int stepsToMove = 3;        // Number of cells to move before returning

    public Vector3[] wayPoints;
    public int targetPoint;
    
    private Vector3 spawnLocation;       // Spawn position of the enemy
    private Vector3 currentDirection;    // Current movement direction
    private bool movingForward = true;  // True if moving away from spawn, false if returning

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // visiting the first waypoint (waypoint[0]) makes no sense, since the first waypoint is always the spawn location
        targetPoint = 1;

        spawnLocation = SnapToGrid(transform.position);
        transform.position = spawnLocation;

        // Set initial direction
        currentDirection = GetRandomValidDirection(Vector3.zero, transform.position);
               
        FindPath();
    }

    private void Patrol()
    {
        if (movingForward)
        {
            MoveForward();
        }
        else
        {
            MoveBackward();
        }
    }

    public List<Vector3> GetPatrollingWayPoints()
    {
        return wayPoints.ToList();
    }

    void Update()
    {
        Patrol();
    }

    public Vector3 getCurrentDirection()
    {
        return currentDirection;
    }

    private void FixedUpdate()
    {
        //Patrol();
    }

    private void MoveBackward()
    {

        if(transform.position == wayPoints[targetPoint])
        {
            DecreaseTargetPointCounter();
        }

        transform.position = Vector3.MoveTowards(transform.position, wayPoints[targetPoint], Time.deltaTime / moveSpeed);
        
        if (targetPoint == 0)
        {
            movingForward = true;
        }
    }

    private void MoveForward()
    {
        if (transform.position == wayPoints[targetPoint])
        {
            IncreaseTargetPointCounter();
        }

        transform.position = Vector3.MoveTowards(transform.position, wayPoints[targetPoint], Time.deltaTime / moveSpeed);

        if (targetPoint == stepsToMove - 1)
        {
            movingForward = false;
        }
    }

    private void IncreaseTargetPointCounter()
    {
        targetPoint++;
    }

    private void DecreaseTargetPointCounter()
    {
        targetPoint--;
    }

    private void FindPath()
    {
        wayPoints = new Vector3[stepsToMove];

        Vector3 targetLocation = spawnLocation;

        for (int i = 0; i < stepsToMove; i++)
        {
            if (i == 0)
            {
                // first waypoint is the spawn location
                wayPoints[i] = targetLocation;
                
            } else
            {
                if (IsObjectInDirection(currentDirection, targetLocation))
                {
                    currentDirection = GetRandomValidDirection(currentDirection, targetLocation);
                }
                targetLocation = targetLocation + currentDirection;

                wayPoints[i] = targetLocation;
            }
            //Debug.Log("Waypoint " + i + ": " +  wayPoints[i]);
        }
    }
    private Vector3 SnapToGrid(Vector3 position)
    {
        // snap position to the nearest cell center
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;

        return new Vector3(x, position.y, z);
    }

    private void OnTriggerEnter(Collider other)
    {
        

        //if (other.tag == "Player")
        //{
        //    foreach (Player player in GameManager.instance.players)
        //    {
        //        if (player == other.gameObject)
        //        {
        //            if (!player.IsInvulnerable())
        //            {
        //                Destroy(other.gameObject);
        //            }
        //        }
        //    }
        //}
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


    private Vector3 GetRandomValidDirection(Vector3 currentDirection, Vector3 position)
    {
        // Possible movement directions
        Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.left, Vector3.back};

        foreach (Vector3 direction in directions)
        {
            // look for next posible direction (only one possible valid direction exists at any time)
            if (!IsObjectInDirection(direction, position) && direction != - currentDirection)
            {
                //Debug.Log("Valid direction found: " + direction.ToString());

                return direction;
            }
        }

        // no valid direction found - move backwards (only happens when reaching a dead end)

        //Debug.Log("NO VALID DIRECTION FOUND!");

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
            bool wallAhead = hit.collider.CompareTag("Wall");
            bool upgradeAhead = hit.collider.CompareTag("Upgrade");
            bool goalAhead = hit.collider.CompareTag("Goal");

            //Debug.Log("Wall ahead: " + wallAhead + " || Position: " + position + " || Direction: " + direction);
            //Debug.Log("Upgrade ahead: " + upgradeAhead + " || Position: " + position + " || Direction: " + direction);
            //Debug.Log("Goal ahead: " + goalAhead + " || Position: " + position + " || Direction: " + direction);

            //Debug.Log("Object in the way: " + hit.collider.tag);

            // players are no obstacles therefore the enemy does not need to evade them
            return  wallAhead || upgradeAhead || goalAhead;
        }
        return false;
    }
}
