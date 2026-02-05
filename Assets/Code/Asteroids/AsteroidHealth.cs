using Steamworks;
using System.Collections;
using UnityEngine;

public class AsteroidHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 3f;
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

    public void TakeDamage(float damage, CSteamID attackerId)
    {
        currentHealth -= damage;
        transform.localScale -= new Vector3(damage * 0.01f, damage * 0.01f, 0);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (attackerId.IsValid())
        {
            StatsManager.Instance?.RecordPlanetDamage(attackerId, damage);
        }

        if (spriteRenderer != null && hittableMaterial != null)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (currentHealth <= 0)
            StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.Asteroids, gameObject));
    }
}
