using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10f);
    public float smoothTime = 0.15f;

    public Vector2 mapCenter = Vector2.zero; // center of the map
    public Vector2 mapSize = new Vector2(200f, 120f); // size of the map

    private Vector3 vel;

    void FixedUpdate()
    {
        if (!target) return;

        Vector3 desired = target.position + offset;
        desired.z = offset.z;

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);

        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        float minX = mapCenter.x - mapSize.x / 2f + camWidth;
        float maxX = mapCenter.x + mapSize.x / 2f - camWidth;
        float minY = mapCenter.y - mapSize.y / 2f + camHeight;
        float maxY = mapCenter.y + mapSize.y / 2f - camHeight;

        smoothed.x = Mathf.Clamp(smoothed.x, minX, maxX);
        smoothed.y = Mathf.Clamp(smoothed.y, minY, maxY);

        transform.position = smoothed;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw map bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapCenter, mapSize);

        if (Camera.main)
        {
            // Draw camera bounds within map
            float camHeight = Camera.main.orthographicSize * 2f;
            float camWidth = camHeight * Camera.main.aspect;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mapCenter, new Vector3(
                Mathf.Min(camWidth, mapSize.x),
                Mathf.Min(camHeight, mapSize.y),
                0));
        }
    }
#endif
}
