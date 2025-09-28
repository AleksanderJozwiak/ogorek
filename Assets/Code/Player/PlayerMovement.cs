using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ship parameters")]
    [SerializeField] private float shipAcceleration = 10f;
    [SerializeField] private float shipMaxVelocity = 10f;
    [SerializeField] private float shipRotationSpeed = 100f;
    [SerializeField] private float bulletSpeed = 8f;

    [Header("Object refereneces")]
    [SerializeField] private Transform bulletSpawn;

    private Rigidbody2D shipRigidbody;
    private bool isAlive = true;
    private bool isAccelerating = false;


    private void Start()
    {
        shipRigidbody = GetComponent<Rigidbody2D>();
    }


    private void Update()
    {
        if (isAlive)
        {
            HandleShipAcceleration();
            HandleShipRotation();
            HandleShooting();
        }
    }

    private void FixedUpdate()
    {
        if (isAlive && isAccelerating)
        {
            shipRigidbody.AddForce(shipAcceleration * transform.up);
            shipRigidbody.linearVelocity = Vector2.ClampMagnitude(shipRigidbody.linearVelocity, shipMaxVelocity);

        }
    }

    private void HandleShipAcceleration()
    {
        isAccelerating = Input.GetKey(KeyCode.UpArrow);
    }

    private void HandleShipRotation()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(shipRotationSpeed * Time.deltaTime * transform.forward);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(-shipRotationSpeed * Time.deltaTime * transform.forward);
        }
    }

    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject go = PoolManager.Instance.PoolMap[PoolCategory.Bullets].Get();

            go.transform.SetPositionAndRotation(bulletSpawn.position, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Vector2 shipVelocity = shipRigidbody.linearVelocity;
            Vector2 shipDirection = transform.up;
            float shipForwardSpeed = Vector2.Dot(shipVelocity, shipDirection);

            if (shipForwardSpeed < 0)
            {
                shipForwardSpeed = 0;
            }

            rb.linearVelocity = shipDirection * shipForwardSpeed;
            rb.AddForce(bulletSpeed * (Vector2)transform.up, ForceMode2D.Impulse);

        }
    }
}
