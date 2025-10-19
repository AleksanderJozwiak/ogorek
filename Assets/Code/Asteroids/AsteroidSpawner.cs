using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate = 2f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            GameObject asteroid = PoolManager.Instance.PoolMap[PoolCategory.Asteroids].Get();
            asteroid.SetActive(true);
        }
    }
}
