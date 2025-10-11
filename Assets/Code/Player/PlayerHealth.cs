using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float respawnTime = 5f;
     private GameSpawnManager gameSpawnManager;

    private void Start()
    {
        currentHealth = maxHealth;
        gameSpawnManager = FindAnyObjectByType<GameSpawnManager>();
    }

    public bool IsAlive() => currentHealth > 0;

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        // temp
        // Destroy(gameObject);
        // przejœæ tryb spectate gdy nie ma ju¿ planety
        // gdy jest planeta to respawn obok nie niej po 5s 
        // wys³aæ do steam coœ

        gameObject.SetActive(false);
        gameSpawnManager.RespawnPlayer();

        PoolManager.Instance.StartCoroutine(RespawnCooldown());
    }

    IEnumerator RespawnCooldown()
    {
        yield return new WaitForSeconds(respawnTime);
        currentHealth = maxHealth;
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // layer bullet
        if (collision.gameObject.layer == 7)
        {
            TakeDamage(1);
            collision.gameObject.SetActive(false);
        }
    }
}
