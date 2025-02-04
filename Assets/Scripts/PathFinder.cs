using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{
    public Cell[,] mazeGrid;
    public List<Enemy> enemies;

    public PathFinder(Cell[,] mazeGrid, List<Enemy> enemies)
    {
        this.mazeGrid = mazeGrid;
        this.enemies = enemies;
    }

    public float GetTileWeight(Cell nextCell, Cell currentCell)
    {
        if (nextCell.isEnemyPlaced())
        {
            // high cost if enemy is present
            return 50f;
        } 
            
        if (nextCell.isUpgradePlaced() && !currentCell.isUpgradePlaced())
        {
            // low cost if upgrade present and current cell has no upgrade placed
            return -2f;
        }
        

        return 1f; // Default cost for normal cells
    }
    public List<Cell> FindShortestPath(Cell start, Cell goal)
    {
        Dictionary<Cell, float> distances = new Dictionary<Cell, float>();
        Dictionary<Cell, Cell> cameFrom = new Dictionary<Cell, Cell>();
        SortedDictionary<float, Queue<Cell>> priorityQueue = new SortedDictionary<float, Queue<Cell>>();
        HashSet<Cell> visited = new HashSet<Cell>();
        distances[start] = 0;
        Enqueue(priorityQueue, start, 0);

        while (priorityQueue.Count > 0)
        {
            Cell current = Dequeue(priorityQueue);

            if (visited.Contains(current))
            {
                // skip already visited cells
                continue; 
            }
               

            visited.Add(current);

            if (current == goal)
            {
                break;
            }
            foreach (Cell neighbor in GetWalkableNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    float newDist = distances[current] + GetTileWeight(neighbor, current);

                    if (!distances.ContainsKey(neighbor) || newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        cameFrom[neighbor] = current;
                        Enqueue(priorityQueue, neighbor, newDist);
                    }
                }
                 
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private void Enqueue(SortedDictionary<float, Queue<Cell>> queue, Cell cell, float priority)
    {
        if (!queue.ContainsKey(priority))
        {
            queue[priority] = new Queue<Cell>();
        }

        queue[priority].Enqueue(cell);
    }

    private Cell Dequeue(SortedDictionary<float, Queue<Cell>> queue)
    {
        var firstPair = queue.First();
        Cell cell = firstPair.Value.Dequeue();

        if (firstPair.Value.Count == 0) 
        {
            queue.Remove(firstPair.Key);
        }
            
        return cell;
    }


private List<Cell> GetWalkableNeighbors(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();
        int x = ((int)cell.transform.position.x);
        int z = ((int)cell.transform.position.z);

        if (cell.CanWalkInFrontDirection()) neighbors.Add(mazeGrid[x, z + 1]);
        if (cell.CanWalkInBackDirection()) neighbors.Add(mazeGrid[x, z - 1]);
        if (cell.CanWalkInLeftDirection()) neighbors.Add(mazeGrid[x - 1, z]);
        if (cell.CanWalkInRightDirection()) neighbors.Add(mazeGrid[x + 1, z]);

        return neighbors;
    }

    private List<Cell> ReconstructPath(Dictionary<Cell, Cell> cameFrom, Cell start, Cell goal)
    {
        List<Cell> path = new List<Cell>();
        Cell current = goal;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom.ContainsKey(current) ? cameFrom[current] : null;
        }

        path.Reverse(); // Start to goal order
        return path;
    }

}
