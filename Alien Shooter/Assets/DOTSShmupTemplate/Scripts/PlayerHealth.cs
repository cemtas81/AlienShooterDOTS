using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int health = 10;
    [SerializeField] private Animator ani;
    private static readonly int DeathTrigger = Animator.StringToHash("Death");
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
          
         Death();
        }
    }
    public void SetHealth(int health)
    {
        if (isDead) return;
        this.health = health;
        if (health <= 0)
        {
          
            Death();
        }
    }
    private void Death()
    {
        playerController.enabled = false;
        isDead = true;
        ani.SetTrigger(DeathTrigger);
    }   
}