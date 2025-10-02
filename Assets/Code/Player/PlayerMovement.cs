using Steamworks;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ship parameters")]
    [SerializeField] private float shipAcceleration = 10f;
    [SerializeField] private float shipMaxVelocity = 10f;
    [SerializeField] private float shipRotationSpeed = 100f;
    [SerializeField] private float bulletSpeed = 8f;

    [Header("Object references")]
    [SerializeField] private Transform bulletSpawn;

    private Rigidbody2D shipRigidbody;

    float thrustInput;
    float turnInput;

    private bool isAlive = true;

    private void Start()
    {
        shipRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!isAlive) return;

        HandleInputs();
        HandleShooting();
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        float rotationDelta = turnInput * shipRotationSpeed * Time.fixedDeltaTime;
        shipRigidbody.MoveRotation(shipRigidbody.rotation + rotationDelta);

        if (Mathf.Abs(thrustInput) > 0f)
        {
            shipRigidbody.AddForce(shipAcceleration * thrustInput * (Vector2)transform.up, ForceMode2D.Force);
        }

        shipRigidbody.linearVelocity = Vector2.ClampMagnitude(shipRigidbody.linearVelocity, shipMaxVelocity);

        PlayerStateMessage msg = new()
        {
            steamId = SteamUser.GetSteamID().m_SteamID,
            posX = transform.position.x,
            posY = transform.position.y,
            rot = transform.eulerAngles.z,
            velX = shipRigidbody.linearVelocity.x,
            velY = shipRigidbody.linearVelocity.y
        };

        SteamNetworkManager.SendPlayerState(msg);
    }


    private void HandleInputs()
    {
        thrustInput = 0f;
        if (Input.GetKey(KeyCode.W)) thrustInput += 1f;
        if (Input.GetKey(KeyCode.S)) thrustInput -= 1f;

        turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput += 1f;
        if (Input.GetKey(KeyCode.D)) turnInput -= 1f;
    }

    private void FireBullet()
    {
        GameObject go = PoolManager.Instance.PoolMap[PoolCategory.Bullets].Get();

        go.transform.SetPositionAndRotation(bulletSpawn.position, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 shipVelocity = shipRigidbody.linearVelocity;
        Vector2 shipDirection = transform.up;
        float shipForwardSpeed = Mathf.Max(0f, Vector2.Dot(shipVelocity, shipDirection));

        rb.linearVelocity = shipDirection * shipForwardSpeed;
        rb.AddForce(bulletSpeed * (Vector2)transform.up, ForceMode2D.Impulse);
    }

    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireBullet();

            ShootMessage msg = new()
            {
                steamId = SteamUser.GetSteamID().m_SteamID,
                posX = bulletSpawn.position.x,
                posY = bulletSpawn.position.y,
                dirX = transform.up.x,
                dirY = transform.up.y
            };

            SteamNetworkManager.SendShoot(msg);
        }
    }
}
