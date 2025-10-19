using System.Collections;
using UnityEngine;
using Steamworks;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float respawnTime = 5f;

    private Material hittableMaterial;
    private SpriteRenderer spriteRenderer;
    private Coroutine hitEffectCoroutine;

    private GameSpawnManager gameSpawnManager;
    private bool isLocalPlayer;
    public bool IsAlive() => currentHealth > 0;

    private void Start()
    {
        currentHealth = maxHealth;
        gameSpawnManager = FindAnyObjectByType<GameSpawnManager>();

        if (TryGetComponent<PlayerIdentity>(out var playerId))
            isLocalPlayer = playerId.SteamId == SteamUser.GetSteamID();

        if (TryGetComponent(out spriteRenderer))
        {
            hittableMaterial = Instantiate(spriteRenderer.material);
            spriteRenderer.material = hittableMaterial;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive()) return;

        currentHealth -= damage;
        SendPlayerHit(damage);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (spriteRenderer != null && hittableMaterial != null)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (currentHealth <= 0)
            Die();
    }

    private void SendPlayerHit(float damage)
    {
        PlayerIdentity identity = GetComponent<PlayerIdentity>();
        if (identity == null) return;

        PlayerHitMessage msg = new PlayerHitMessage
        {
            steamId = identity.SteamId.m_SteamID,
            damage = damage
        };

        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.PlayerHit;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);

        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
        {
            if (member != SteamUser.GetSteamID())
                SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
        }
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

    private void Die()
    {
        currentHealth = 0;
        hittableMaterial.SetFloat("_HitColorAmount", 0);

        if (isLocalPlayer)
        {
            string slotMeta = SteamMatchmaking.GetLobbyMemberData(
                LobbyManager.Instance.currentLobby,
                SteamUser.GetSteamID(),
                "slot"
            );

            if (!string.IsNullOrEmpty(slotMeta))
            {
                string[] split = slotMeta.Split('_');
                int teamNum = int.Parse(split[0]);

                if (gameSpawnManager.IsTeamBaseAlive(teamNum))
                {
                    gameSpawnManager.ShowRespawnUI(true);
                    gameObject.SetActive(false);
                    SendDeathState(false);
                    GameSpawnManager.Instance.StartCoroutine(RespawnCooldown());
                }
                else
                {
                    gameSpawnManager.StartSpectateMode();
                    gameObject.SetActive(false);
                    SendDeathState(false);
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
            SendDeathState(false);
        }
    }



    IEnumerator RespawnCooldown()
    {
        float remainingTime = respawnTime;
        while (remainingTime > 0)
        {
            gameSpawnManager.UpdateRespawnCounter(Mathf.CeilToInt(remainingTime));
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        currentHealth = maxHealth;
        gameSpawnManager.ShowRespawnUI(false);

        gameObject.SetActive(true);
        gameSpawnManager.RespawnPlayer(gameObject);
        SendDeathState(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // layer bullet, team tag?
        if (collision.gameObject.layer == 7)
        {
            TakeDamage(1);
            collision.gameObject.SetActive(false);
        }
    }

    private void SendDeathState(bool alive)
    {
        PlayerStateMessage msg = new PlayerStateMessage
        {
            steamId = SteamUser.GetSteamID().m_SteamID,
            posX = transform.position.x,
            posY = transform.position.y,
            rot = transform.rotation.eulerAngles.z,
            velX = 0,
            velY = 0,
            emmitingTrail = false,
            isAlive = alive
        };

        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.PlayerState;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);

        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
        {
            if (member != SteamUser.GetSteamID())
                SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
        }
    }
}
