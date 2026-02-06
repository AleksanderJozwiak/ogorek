using Steamworks;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [SerializeField] private float spawnDistanceFromCenter = 170f;
    [SerializeField] private float damage = 1f;


    public float maxSpeed = 10f;
    private float speed = 3f;
    private Vector3 direction;
    public Vector3 Direction => direction;
    public void SetDirection(Vector3 dir) => direction = dir.normalized;

    private void OnEnable()
    {
        transform.position = GetRandomSpawnPosition();
        Vector3 randomTarget = new Vector3(
            Random.Range(-75f, 75f),
            Random.Range(-75f, 75f),
            0f
        );
        speed = Random.Range(3f, maxSpeed);
        direction = (randomTarget - transform.position).normalized;
    }

    void FixedUpdate()
    {
        transform.position += direction * speed * Time.deltaTime;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable target) && !collision.TryGetComponent(out AsteroidHealth health))
        {
            target.TakeDamage(damage, CSteamID.Nil);
            StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.Asteroids, gameObject));
        }
        //if (collision.TryGetComponent(out Bullet bullet))
        //{
        //    target.TakeDamage(damage, CSteamID.Nil);
        //    StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.Asteroids, gameObject));
        //}
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float x = Mathf.Cos(angle) * spawnDistanceFromCenter;
        float y = Mathf.Sin(angle) * spawnDistanceFromCenter;
        return new Vector3(x, y, 0f);
    }
}