using UnityEngine;

public class RandomShipManager : MonoBehaviour
{
    [SerializeField] Sprite[] ships;
    [SerializeField] GameObject[] trails;
    [SerializeField] ColorPalette colorPalette;

    void Start()
    {
        if(ships.Length == colorPalette.Colors.Length)
        {
            int result = Random.Range(0, ships.Length);
            
            SpriteRenderer? spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = ships[result];

            foreach(GameObject trail in trails)
            {
                TrailRenderer? renderer = trail.GetComponent<TrailRenderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TeamColor", colorPalette.Colors[result]);
                renderer.SetPropertyBlock(block);
            }
        }
        else
        {
            Debug.LogError("The numbers of ships' sprites and colors are not equal!");
        }
    }
}
