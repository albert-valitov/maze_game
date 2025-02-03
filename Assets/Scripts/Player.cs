using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviour
{
    private bool isMovementPaused = false;
    private float moveSpeed = 1.5f;
    bool isUpgraded;
    private Rigidbody rb;
    public Vector3 startPosition;
    private List<List<Cell>> upgradeCells = new List<List<Cell>>();
    private List<Cell> pathToGoal = new List<Cell>();


    
    void Start()
    {
        startPosition = transform.position; 
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
       
    }
    private void FixedUpdate()
    {
        if (GameManager.instance.isAiControlled())
        {
            // do not accept inputs when the ai is playing the game
            return;
        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude > 0.1f)
        {
            // Move the character
            Move(direction);
        }

    }

    public void Move(Vector3 direction)
    {
        if (IsMovementPaused())
        {
            return;
        }
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    public void MoveToTarget(Vector3 target)
    {
        if (IsMovementPaused())
        {
            return;
        }

        Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        rb.MovePosition(newPosition);
    }

    private void OnDestroy()
    {
        
        GameManager.instance.RemovePlayer(this);
    }

    public void SetPathToGoal(List<Cell> path)
    {
        pathToGoal = path;
    }

    public List<Cell> GetPathToGoal()
    {
        return pathToGoal;
    }

    public bool IsUpgraded()
    {
        return isUpgraded;
    }

    public void SetUpgradeCells(List<List<Cell>> cells)
    {
        upgradeCells = cells;
    }

    public List<List<Cell>> GetUpgradeCells()
    {
        return upgradeCells;
    }

    public void AddUpgradeCell(List<Cell> cell)
    {
        upgradeCells.Add(cell);
    }

    private bool IsMovementPaused()
    {
        return isMovementPaused;
    }
    public void SetIsUpgraded(bool isUpgraded)
    {
        this.isUpgraded = isUpgraded;

        if (isUpgraded)
        {
            moveSpeed = 2f;
            StopCoroutine(ResumeMovementAfterDelay());
            StartCoroutine(ResumeMovementAfterDelay());
        }
    }

    private IEnumerator ResetMovementSpeed()
    {
        yield return new WaitForSeconds(GameManager.instance.upgradeTime);
        moveSpeed = 1.5f;
    }


    public void SetMovementPaused(bool value)
    {
        isMovementPaused = value;

        if (value)
        {
            // if movement was paused -> restart it after a short delay
            StartCoroutine(ResumeMovementAfterDelay());
        }
    }
    
    private IEnumerator ResumeMovementAfterDelay()
    {
        yield return new WaitForSeconds(GameManager.instance.upgradeTime);

        SetMovementPaused(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
       
    }
}
