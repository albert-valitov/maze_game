using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Cell;


/*
 * Pathfinding class for the AI Controller. Looks for a path from start to goal using Bellman-Ford algorithm. 
 * Cells have different weights which may change depending on the current situation of the player (fast movement or invulnerable)
 */
public class PathFinder
{
    public class Node
    {
        public Cell cell;
        public Vector3Int position;
        public float cost;
        public Node parent;
        public bool inNegativeCycle;
        public int stepsSinceLastUpgrade;

        public Node(Cell cell, float cost, Node parent = null, int stepsSinceUpgrade = 0)
        {
            this.cell = cell;
            this.cost = cost;
            this.parent = parent;
            inNegativeCycle = false;
            stepsSinceLastUpgrade = stepsSinceUpgrade;
            position = Vector3Int.FloorToInt(cell.transform.position);
        }
    }

    private int width, height;
    private Cell[,] mazeGrid;
    private List<Enemy> enemies;

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

    public PathFinder(Cell[,] mazeGrid, List<Enemy> enemies)
    {
        this.mazeGrid = mazeGrid;
        this.enemies = enemies;
        width = mazeGrid.GetLength(0);
        height = mazeGrid.GetLength(1);
    }

    public List<Cell> FindPath(Cell start, Cell goal)
    {
        Dictionary<Cell, Node> nodes = new Dictionary<Cell, Node>();
        nodes[start] = new Node(start, 0, null, 3);

        // Bellman-Ford: relax edges
        for (int i = 0; i < width * height - 1; i++)
        {
            bool updated = false;
            foreach (var kvp in new Dictionary<Cell, Node>(nodes))
            {
                Node current = kvp.Value;

                foreach (Vector3Int dir in possibleDirections.Values)
                {
                    Vector3Int neighborPos = current.position + dir;

                    if (IsOutOfBounds(neighborPos))
                    {
                        continue;
                    }

                    Cell neighborCell = GetCell(neighborPos);

                    if (!current.cell.CanWalk(directionToWallType[dir]))
                    {
                        // wall blocks movement in that direction
                        continue;
                    }

                    float baseCost = GetWeight(neighborCell);

                    int newStepsSinceUpgrade = current.stepsSinceLastUpgrade + 1;

                    // decide if we should pick up the upgrade if one exists
                    if (neighborCell.isUpgradePlaced())
                    {
                        if (newStepsSinceUpgrade <= 3)
                        {
                            // upgrade was picked up recently - treat it like a regular/empty cell 
                            baseCost = 1;                        
                        } else
                        {
                            // upgrade is picked up - reset the counter
                            newStepsSinceUpgrade = 0;
                        }
                    }

                    float newCost = current.cost + baseCost;

                    if (!nodes.ContainsKey(neighborCell) || newCost < nodes[neighborCell].cost)
                    {
                        nodes[neighborCell] = new Node(neighborCell, newCost, current, newStepsSinceUpgrade);
                        updated = true;
                    }
                }
            }
            if (!updated)
            {
                break;
            }

        }

        return ReconstructPath(nodes, goal);
    }

    private Cell GetCell(Vector3Int position)
    {
        return mazeGrid[position.x, position.z];
    }

    private bool IsOutOfBounds(Vector3Int position)
    {
        int x = position.x;
        int z = position.z;

        return x < 0 || x > width - 1 || z < 0 || z > height - 1;
    }

    private List<Cell> ReconstructPath(Dictionary<Cell, Node> nodes, Cell goal)
    {
        List<Cell> path = new List<Cell>();

        if (!nodes.ContainsKey(goal)) 
        {
            // no path found
            return path;
        }
       

        Node current = nodes[goal];
        float cost = 0f;

        while (current != null)
        {
            cost += current.cost;
            path.Add(current.cell);
            current = current.parent;
        }

        if (cost == float.PositiveInfinity)
        {
            // no viable path exists - player would have to move through enemy
            return new List<Cell>();
        }

        path.Reverse();

        return path;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        // snap position to the nearest cell center
        float x = Mathf.Round(position.x / 1f) * 1f;
        float z = Mathf.Round(position.z / 1f) * 1f;

        return new Vector3(x, 0, z);
    }
    
    private float GetWeight(Cell cell)
    {
        foreach (Enemy enemy in enemies)
        {
            
            Vector3Int position = Vector3Int.FloorToInt(SnapToGrid(enemy.transform.position));

            if (IsOutOfBounds(position))
            {
                continue;
            }            

            Cell enemyCell = GetCell(position);

            if (cell == enemyCell)
            {
                return float.PositiveInfinity;
            }
        }

        if (cell.isUpgradePlaced())
        {
            // motivate to walk a detour to pick up an upgrade
            return -3f;
        }

        // regular/empty cell
        return 1f;
    }
}
