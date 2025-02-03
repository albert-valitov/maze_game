using UnityEngine;

public class MazePopulator
{
    private Difficulty difficulty;
    public MazePopulator(Difficulty difficulty)
    {
        this.difficulty = difficulty;
    }

    public void Populate(GameObject maze, GameObject enemyPrefab, GameObject upgradePrefab, GameObject goalPrefab)
    {
        GameObject.Instantiate(enemyPrefab);
    }
}
