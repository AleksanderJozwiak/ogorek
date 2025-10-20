using UnityEngine;
using System.Collections;
using Steamworks;

public class PlanetHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int teamNumber = 1;
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;

    private Material hittableMaterial;
    private SpriteRenderer spriteRenderer;
    private Coroutine hitEffectCoroutine;

    private void Start()
    {
        currentHealth = maxHealth;

        if (TryGetComponent(out spriteRenderer))
        {
            hittableMaterial = Instantiate(spriteRenderer.material);
            spriteRenderer.material = hittableMaterial;
        }

        SendTeamBaseState(teamNumber, true);
        // Immediately sync with GameSpawnManager
        GameSpawnManager.Instance?.SetTeamBaseState(teamNumber, true);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        transform.localScale -= new Vector3(damage * 0.01f, damage * 0.01f, 0);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (spriteRenderer != null && hittableMaterial != null)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (currentHealth <= 0)
            DestroyTeam();
    }

    private IEnumerator HitFlashEffect()
    {
        float flashTime = 0.75f;

        while (flashTime > 0)
        {
            hittableMaterial.SetFloat("_HitColorAmount", flashTime);
            yield return new WaitForSeconds(0.01f);
            flashTime -= 0.02f;
        }
    }

    private void DestroyTeam()
    {
        hittableMaterial.SetFloat("_HitColorAmount", 0);

        GameSpawnManager.Instance?.SetTeamBaseState(teamNumber, false);

        SendTeamBaseState(teamNumber, false);

        Destroy(gameObject);
    }

    private void SendTeamBaseState(int teamNum, bool alive)
    {
        TeamBaseMessage msg = new()
        {
            teamNumber = teamNum,
            baseAlive = alive
        };

        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.TeamBaseDestroyed;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);

        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
        {
            SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
        }
    }
}