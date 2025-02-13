using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    private float startTime;
    public float score;
    public Text scoreText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreText = GetComponent<Text>();
        startTime = Time.time;
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        score = GameManager.instance.score * GameManager.instance.multiplier;
        scoreText.text = "Multiplied Score: " + score;
    }
}
