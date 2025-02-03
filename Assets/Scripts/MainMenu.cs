using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private Difficulty selectedDifficulty;
    public Button mediumDifficulty;

    void Start()
    {
        // preselect medium difficulty as default
        mediumDifficulty.Select();
    }
    public void OnPlay()
    {
        PlayerPrefs.SetInt("Difficulty", (int) selectedDifficulty);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }

    public void OnQuit()
    {
        Application.Quit();
    }

    public void SetDifficultyEasy()
    {
        selectedDifficulty = Difficulty.Easy;
    }

    public void SetDifficultyMedium()
    {
        selectedDifficulty = Difficulty.Medium;
    }

    public void SetDifficultyHard()
    {
        selectedDifficulty = Difficulty.Hard;
    }
}
