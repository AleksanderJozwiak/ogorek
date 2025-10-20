using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float bulletLifetime = 1f;
    [SerializeField] private PoolCategory category = PoolCategory.Bullets;
    [SerializeField] private float damage = 1f;

    Rigidbody2D rb;
    Coroutine lifeRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        lifeRoutine = StartCoroutine(AutoRelease());
    }

    void OnDisable()
    {
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
    }

    IEnumerator AutoRelease()
    {
        yield return new WaitForSeconds(bulletLifetime);
        yield return PoolManager.Instance.ReleaseObject(category, gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var sameTeam = collision.CompareTag(tag);

        if (sameTeam) return;

        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
        }

        if (!sameTeam)
            gameObject.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        StartCoroutine(PoolManager.Instance.ReleaseObject(category, gameObject));
    }
}
