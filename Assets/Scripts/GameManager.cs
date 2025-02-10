using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public AIController aIController;

    public List<Player> players;

    public List<Enemy> enemies;

    public List<Upgrade> upgrades;

    public Goal goal;

    public Cell[,] mazeGrid;

    public bool aiControlled;

    public int multiplier = 0;

    public float upgradeTime = 3.0f;

    public List<Cell> playerSafeSpace = new List<Cell>();

    public bool gameOver;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
        
    }

    private void Start()
    {
        
    }
   

    private void Update()
    {
       
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
                player.SetInvulnerable(true);
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
                    player.SetInvulnerable(false);
                }
            }


            if (players.Count == 0)
            {
                gameOver = true;
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

    public bool IsPlayerSafeSpace(Cell cell)
    {
        return playerSafeSpace.Contains(cell);
    }

    public void RestartGame()
    {
        gameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        gameOver = false;
        SceneManager.LoadScene("MainMenu");
    }

}
