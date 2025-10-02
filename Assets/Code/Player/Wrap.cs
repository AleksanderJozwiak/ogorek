using UnityEngine;

[ExecuteAlways]
public class Wrap : MonoBehaviour
{
    [Header("World bounds")]
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(200f, 120f);

    [Header("Collision safety")]
    public bool useRadius = true;
    public float radiusOverride = -1f;

    Rigidbody2D rb;

    float MinX => center.x - size.x * 0.5f;
    float MaxX => center.x + size.x * 0.5f;
    float MinY => center.y - size.y * 0.5f;
    float MaxY => center.y + size.y * 0.5f;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        var pos = rb ? rb.position : (Vector2)transform.position;
        float r = GetRadius();

        bool wrapped = false;

        // X
        if (pos.x < MinX - r) { pos.x += size.x; wrapped = true; }
        else if (pos.x > MaxX + r) { pos.x -= size.x; wrapped = true; }

        // Y
        if (pos.y < MinY - r) { pos.y += size.y; wrapped = true; }
        else if (pos.y > MaxY + r) { pos.y -= size.y; wrapped = true; }

        if (wrapped)
        {
            if (rb)
                rb.position = pos;   
            else
                transform.position = pos;
        }
    }

    float GetRadius()
    {
        if (!useRadius) return 0f;
        if (radiusOverride > 0f) return radiusOverride;

        float r = 0f;
        var col = GetComponent<Collider2D>();
        if (col) r = Mathf.Max(r, col.bounds.extents.magnitude * 0.7f);

        var sr = GetComponent<SpriteRenderer>();
        if (sr) r = Mathf.Max(r, sr.bounds.extents.magnitude * 0.7f);

        return r;
    }

}

