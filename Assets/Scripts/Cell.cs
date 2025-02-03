using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public GameObject frontWall;

    public GameObject backWall;

    public GameObject leftWall;

    public GameObject rightWall;

    public AbstractUpgrade upgrade;

    public Enemy enemy;

    public Goal goal;

    private bool visited = false;

    public Vector3 position;

    public enum WallType
    {
        FrontWall,
        BackWall,
        LeftWall,
        RightWall
    }
    private void Start()
    {
       
    }
    public Dictionary<WallType, bool> walkableDirections = new Dictionary<WallType, bool>
    {
        { WallType.FrontWall, false },
        { WallType.BackWall, false },
        { WallType.LeftWall, false },
        { WallType.RightWall, false }
    };

    public bool CanWalk(WallType wallType)
    {
        return walkableDirections.ContainsKey(wallType) && walkableDirections[wallType];
    }

    public void SetVisited()
    {
        visited = true;
    }

    public bool isVisited()
    {
        return visited;
    }

    public void SetPosition(Vector3 position)
    {
        // the position within the maze grid
        this.position = position;
    }
    public void BreakWall(WallType wallType)
    {
        GameObject wall = null;

        switch (wallType)
        {
            case WallType.FrontWall:
                wall = frontWall;
                break;
            case WallType.BackWall:
                wall = backWall;
                break;
            case WallType.LeftWall:
                wall = leftWall;
                break;
            case WallType.RightWall:
                wall = rightWall;
                break;
            default:
                Debug.LogError("Can not break wall. Unkown wall type");
                break;
        }

        // set value of the walltype to true -> a possible walkable direction
        walkableDirections[wallType] = true;

        BreakWallInternal(wall);
    }

    public void setUpgrade(AbstractUpgrade upgrade)
    {
        this.upgrade = upgrade;
    }

    public bool isUpgradePlaced()
    {
        return upgrade != null;
    }

    public void setEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public bool isEnemyPlaced()
    {
        return enemy != null;
    }

    public bool isGoalPlaced()
    {
        return goal != null;
    }
   
    private void BreakWallInternal(GameObject wall)
    {
        wall.SetActive(false);
    }

    public bool Equals(Cell cell)
    {
        return transform.position.x == cell.transform.position.x && transform.position.z == cell.transform.position.z;
    }

    public bool CanWalkInRightDirection()
    {
        return rightWall.activeSelf;
    }

    public bool CanWalkInLeftDirection()
    {
        return leftWall.activeSelf;
    }

    public bool CanWalkInFrontDirection() 
    { 
        return frontWall.activeSelf; 
    }

    public bool CanWalkInBackDirection() 
    { 
        return backWall.activeSelf; 
    }

    public List<WallType> GetIntactWalls()
    {
        List<WallType> intactWalls = new List<WallType>();

        foreach (var item in walkableDirections)
        {
            if (!item.Value)
            {
                // can not walk in that direction - wall exists
                intactWalls.Add(item.Key);
            }
        }
        
        return intactWalls;

    }


}
