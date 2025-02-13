using UnityEngine;
using UnityEngine.UI;

public class Multiplier : MonoBehaviour
{
    public Text multiplierText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        multiplierText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        multiplierText.text = "Multiplier: " + GameManager.instance.multiplier;
    }
}
