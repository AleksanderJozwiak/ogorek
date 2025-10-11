using UnityEngine;

public class PlanetHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;        
    }

    private void Update()
    {
        
    }

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0) DestroyTeam();
    }

    private void DestroyTeam()
    {
        // temp
        Destroy(gameObject);
        // wys³aæ do steam coœ
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
