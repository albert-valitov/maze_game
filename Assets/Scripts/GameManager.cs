using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public AIController aIController;

    private List<Player> players;

    private List<Enemy> enemies;

    private List<Upgrade> upgrades;

    private Goal goal;

    private Cell[,] mazeGrid;

    public bool aiControlled;

    public int multiplier = 0;

    public float upgradeTime = 3f;
    
    void Awake()
    {
        instance = this;

        //if (instance == null)
        //{
        //    instance = this;
        //}
        //else
        //{
        //    Destroy(gameObject); // Prevent duplicate GameManagers
        //    return;
        //}

        //DontDestroyOnLoad(gameObject); // Optional: Persist across scenes

        aiControlled = true;

        
    }

    public void InitAiController()
    {
        if (aiControlled)
        {
            aIController = Instantiate(aIController);
            aIController.Init(players, enemies, upgrades, goal, mazeGrid);
        }
    }

    public bool isAiControlled()
    {
        return aiControlled;
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

    public void ApplyUpgradeEffect(Player affectedPlayer)
    {
        foreach (Player player in players)
        {
            if (player == affectedPlayer)
            {
                player.SetIsUpgraded(true);
                player.SetMovementPaused(false);
            }
            else
            {
                // pause movement of all unaffected players
                player.SetMovementPaused(true);
            }
        }
    }

    public void RemovePlayer(Player playerToRemove)
    {
        if (playerToRemove != null)
        {
            players.Remove(playerToRemove);

            if (playerToRemove.IsUpgraded())
            {
                foreach (Player player in players)
                {
                    // disable upgrade effect when upgraded player dies or reaches goal
                    player.SetMovementPaused(false);
                }
            }


            if (players.Count == 0)
            {
                // TODO: game is over -> calculate score with multiplier. If multiplier 0 -> no player made it to the goal -> 0 points
            }
        }
    }

    public void IncreaseMultiplier()
    {
        multiplier++;
    }

    public void ChangeFocusedPlayer(Player player)
    {
        if (aiControlled)
        {
            aIController.ChangeFocusedPlayer(player);
        }
    }

}
