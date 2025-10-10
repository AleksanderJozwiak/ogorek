using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10f);
    public float smoothTime = 0.15f;

    Vector3 vel;

    void FixedUpdate()
    {
        if (!target) return;
        var desired = target.position + offset;
        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
    }
}