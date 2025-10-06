using UnityEngine;

public class RandomShipManager : MonoBehaviour
{
    [SerializeField] Sprite[] ships;
    [SerializeField] Color[] colors;
    [SerializeField] GameObject[] trails;

    void Start()
    {
        if(ships.Length == colors.Length)
        {
            int result = Random.Range(0, ships.Length);
            
            SpriteRenderer? spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = ships[result];

            foreach(GameObject trail in trails)
            {
                TrailRenderer? renderer = trail.GetComponent<TrailRenderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TeamColor", colors[result]);
                renderer.SetPropertyBlock(block);
            }
        }
        else
        {
            Debug.LogError("The numbers of ships' sprites and colors are not equal!");
        }
    }
}
