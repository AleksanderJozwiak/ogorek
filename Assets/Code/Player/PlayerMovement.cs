using Steamworks;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ship parameters")]
    [SerializeField] private float shipAcceleration = 2f;
    [SerializeField] private float shipMaxVelocity = 3f;
    [SerializeField] private float shipRotationSpeed = 100f;
    [SerializeField] private float bulletSpeed = 8f;

    [Header("Object references")]
    [SerializeField] private Transform bulletSpawn;
    [SerializeField] private TrailRenderer[] trailRenderers;

    [Header("Trail parameters")]
    [SerializeField] private float trailLifetime = 0.5f;
    [SerializeField] private float trailFadeOutTime = 2f;

    private Rigidbody2D shipRigidbody;
    private PlayerHealth playerHealth;

    float thrustInput;
    float turnInput;

    private bool isAlive = true;

    private MenuManager menuManager;

    public float GetTrialLifetime() => trailLifetime;
    public float GetTrailFadeOutTime() => trailFadeOutTime;

    private void Start()
    {
        shipRigidbody = GetComponent<Rigidbody2D>();
        menuManager = FindAnyObjectByType<MenuManager>();
        playerHealth = GetComponent<PlayerHealth>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        isAlive = playerHealth.IsAlive();
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
            velY = shipRigidbody.linearVelocity.y,
            emmitingTrail = Input.GetKey(KeyCode.W),
            isAlive = isAlive,
        };

        SteamNetworkManager.SendPlayerState(msg);
    }


    private void HandleInputs()
    {
        thrustInput = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            thrustInput += 1f;
            
            foreach(TrailRenderer trail in trailRenderers)
            {
                trail.emitting = true;
                trail.time = trailLifetime;
            }
        }
        else
        {
            foreach(TrailRenderer trail in trailRenderers)
            {
                trail.time = Mathf.Max(0f, trail.time - trailFadeOutTime * Time.deltaTime);
                trail.emitting = trail.time > 0f;
            }
        }

        if (Input.GetKey(KeyCode.S)) thrustInput -= 1f;

        turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput += 1f;
        if (Input.GetKey(KeyCode.D)) turnInput -= 1f;

        if (Input.GetKeyDown(KeyCode.Escape)) menuManager.TogglePauseMenu();
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

        SoundFXManager.Instance.PlaySound2D(SoundType.Blaster, transform, 1f);
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
