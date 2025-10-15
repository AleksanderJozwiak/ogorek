using Steamworks;
using System.Collections;
using UnityEngine;

public class PlanetHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;

    private Material hittableMaterial;
    private SpriteRenderer spriteRenderer;
    private Coroutine hitEffectCoroutine;

    private void Start()
    {
        currentHealth = maxHealth;

        if (TryGetComponent(out spriteRenderer))
        {
            hittableMaterial = Instantiate(spriteRenderer.material);
            spriteRenderer.material = hittableMaterial;
        }
    }

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        gameObject.transform.localScale -= new Vector3(damage * 0.01f, damage * 0.01f, damage * 0.01f);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (spriteRenderer != null && hittableMaterial != null)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (currentHealth <= 0) DestroyTeam();
    }

    private IEnumerator HitFlashEffect()
    {
        float flashTime = 0.75f;

        while (flashTime > 0)
        {
            hittableMaterial.SetFloat("_HitColorAmount", flashTime);
            yield return new WaitForSeconds(0.01f);
            flashTime -= 0.02f;
        }
    }

    private void DestroyTeam()
    {
        hittableMaterial.SetFloat("_HitColorAmount", 0);
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
