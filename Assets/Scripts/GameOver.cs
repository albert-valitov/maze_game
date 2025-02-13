using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class GameOver : MonoBehaviour
{
    public GameObject gameOverPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

        if (GameManager.instance.gameOver)
        {
            gameOverPanel.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Activate the panel.
            gameOverPanel.SetActive(!gameOverPanel.activeSelf);
        }
    }

    public void RestartGame()
    {
        gameOverPanel.SetActive(false);
        GameManager.instance.RestartGame();
    }

    public void QuitToMenu()
    {
        gameOverPanel.SetActive(false);
        GameManager.instance.QuitToMenu();
    }

    public void OnQuit()
    {
        gameOverPanel.SetActive(false);
        Application.Quit();
    }
}
