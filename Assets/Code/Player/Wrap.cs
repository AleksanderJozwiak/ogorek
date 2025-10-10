using UnityEngine;

[ExecuteAlways]
public class Wrap : MonoBehaviour
{
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(200f, 120f);

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

        Vector2 pos = rb ? rb.position : (Vector2)transform.position;
        float r = GetRadius();

        // Predictive wrap offset
        Vector2 wrapOffset = Vector2.zero;

        if (pos.x < MinX - r) wrapOffset.x = size.x;
        else if (pos.x > MaxX + r) wrapOffset.x = -size.x;

        if (pos.y < MinY - r) wrapOffset.y = size.y;
        else if (pos.y > MaxY + r) wrapOffset.y = -size.y;

        // Apply the wrap offset smoothly to the camera
        if (wrapOffset != Vector2.zero && Camera.main)
        {
            Camera.main.transform.position += new Vector3(wrapOffset.x, wrapOffset.y, 0f);
        }

        // Apply the actual wrap to the player
        if (wrapOffset != Vector2.zero)
        {
            pos += wrapOffset;
            if (rb) rb.position = pos;
            else transform.position = pos;
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
