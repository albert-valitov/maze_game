using Unity.VisualScripting;
using UnityEngine;

public class Upgrade : AbstractUpgrade
{
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetComponent<Player>() != null)
            {
                GameManager.instance.score++;
                // apply upgrade effect to player object
                GameManager.instance.ApplyUpgradeEffect(other.GetComponent<Player>());
                GameManager.instance.ChangeFocusedPlayer(other.GetComponent<Player>());
                Debug.Log("PLAYER (" + other.GetComponent<Player>().startPosition.x + ", " + other.GetComponent<Player>().startPosition.z + ") ACTIVATED UPGRADE");
                // TODO: play sound

                // remove upgrade object from game after it was activated
                Destroy(gameObject);                
            }
        }        
    }

    public override void ApplyEffect(GameObject player)
    {
        //TODO: apply 
    }


}
