using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int health = 10;
    private PlayerHybridController playerController;
    private bool isDead = false;    
    private void Start()
    {
        playerController = GetComponent<PlayerHybridController>();
    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;
    

        if (health <= 0)
        {
            // Player öldü, script disable edilir
            playerController.enabled = false;
            isDead = true;

        }
    }
}