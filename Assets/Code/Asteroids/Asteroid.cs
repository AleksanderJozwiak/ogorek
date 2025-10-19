using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [SerializeField] private float spawnDistanceFromCenter = 170f;
    [SerializeField] private float damage = 1f;


    public float speed = 5f;
    private Vector3 direction;

    private void OnEnable()
    {
        transform.position = GetRandomSpawnPosition();
        direction = (Vector3.zero - transform.position).normalized;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
            StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.Asteroids, gameObject));
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float x = Mathf.Cos(angle) * spawnDistanceFromCenter;
        float y = Mathf.Sin(angle) * spawnDistanceFromCenter;
        return new Vector3(x, y, 0f);
    }
}