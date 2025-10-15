using System.Collections;
using UnityEngine;
using Steamworks;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float respawnTime = 5f;
    private GameSpawnManager gameSpawnManager;
    private bool isLocalPlayer;
    public bool IsAlive() => currentHealth > 0;

    private void Start()
    {
        currentHealth = maxHealth;
        gameSpawnManager = FindAnyObjectByType<GameSpawnManager>();

        // Sprawdü czy to lokalny gracz
        var playerId = GetComponent<PlayerIdentity>();
        if (playerId != null)
            isLocalPlayer = playerId.SteamId == SteamUser.GetSteamID();
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive()) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        currentHealth = 0;

        if (isLocalPlayer)
        {
            gameSpawnManager.ShowRespawnUI(true);
            PoolManager.Instance.StartCoroutine(RespawnCooldown());
        }

        gameObject.SetActive(false);
        SendDeathState(false);
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

        gameSpawnManager.RespawnPlayer(gameObject);
        gameObject.SetActive(true);
        SendDeathState(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // layer bullet
        if (collision.gameObject.layer == 7)
        {
            TakeDamage(1);
            collision.gameObject.SetActive(false);
        }
    }

    private void SendDeathState(bool alive)
    {
        // Wysy≥amy pakiet do innych graczy z aktualnym stanem
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
