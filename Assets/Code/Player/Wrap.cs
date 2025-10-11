using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
public class Wrap : MonoBehaviour
{
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(200f, 120f);

    Rigidbody2D rb;

    float HalfWidth => size.x * 0.5f;
    float HalfHeight => size.y * 0.5f;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        Vector2 pos = rb.position;

        pos.x = WrapCoordinate(pos.x, center.x - HalfWidth, center.x + HalfWidth);
        pos.y = WrapCoordinate(pos.y, center.y - HalfHeight, center.y + HalfHeight);

        rb.position = pos;
        
    }

    float WrapCoordinate(float value, float min, float max)
    {
        float range = max - min;
        if (range <= 0f) return value;
        value = (value - min) % range;
        if (value < 0) value += range;
        return min + value;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
