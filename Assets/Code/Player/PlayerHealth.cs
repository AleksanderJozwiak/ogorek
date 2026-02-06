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
    private bool isLocalPlayer;

    private int teamNumber = -1;
    private bool _isAlive = true;

    private CSteamID lastAttacker;
    private PlayerIdentity identity;

    public bool IsAlive() => _isAlive;

    private void Start()
    {
        currentHealth = maxHealth;
        _isAlive = true;

        if (identity != null)
        {
            StatsManager.Instance?.RecordRespawn(identity.SteamId);
        }

        if (TryGetComponent(out identity))
            isLocalPlayer = identity.SteamId == SteamUser.GetSteamID();

        if (TryGetComponent(out spriteRenderer))
        {
            hittableMaterial = Instantiate(spriteRenderer.material);
            spriteRenderer.material = hittableMaterial;
        }
    }

    public void SetTeam(int team)
    {
        teamNumber = team;
    }

    public void TakeDamage(float damage, CSteamID attackerId)
    {
        if (!_isAlive) return;

        currentHealth -= damage;

        if (attackerId.IsValid() && identity != null)
        {
            lastAttacker = attackerId;

            if (attackerId != identity.SteamId)
            {
                StatsManager.Instance?.RecordPlayerDamage(attackerId, identity.SteamId, damage, maxHealth);
            }
        }

        SendPlayerHit(damage);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (spriteRenderer != null && hittableMaterial != null)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (currentHealth <= 0 && _isAlive)
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
        if (!_isAlive) return;

        _isAlive = false;
        currentHealth = 0;

        if (identity != null)
        {
            // Zg³oœ w³asn¹ œmieræ
            StatsManager.Instance?.RecordDeath(identity.SteamId);

            // Zg³oœ, ¿e `lastAttacker` zdoby³ zabójstwo
            // (Jeœli lastAttacker jest nieprawid³owy, np. samobójstwo, nic siê nie stanie)
            if (lastAttacker.IsValid())
            {
                StatsManager.Instance?.RecordKill(lastAttacker, identity.SteamId, maxHealth);
            }
        }

        lastAttacker = CSteamID.Nil;

        hittableMaterial.SetFloat("_HitColorAmount", 0);

        if (isLocalPlayer)
        {
            if (teamNumber == -1)
            {
                Debug.LogError("PlayerHealth: teamNumber nie zosta³ ustawiony! Gracz nie mo¿e siê odrodziæ.");
                GameSpawnManager.Instance?.StartSpectateMode();
                SendPlayerState(false);
                gameObject.SetActive(false);
                return;
            }

            if (GameSpawnManager.Instance != null && GameSpawnManager.Instance.IsTeamBaseAlive(teamNumber))
            {
                GameSpawnManager.Instance.ShowRespawnUI(true);
                SendPlayerState(false);
                gameObject.SetActive(false);
                GameSpawnManager.Instance.StartCoroutine(RespawnCooldown());
            }
            else
            {
                GameSpawnManager.Instance?.StartSpectateMode();
                SendPlayerState(false);
                gameObject.SetActive(false);
            }
        }
    }

    IEnumerator RespawnCooldown()
    {
        float remainingTime = respawnTime;
        while (remainingTime > 0)
        {
            GameSpawnManager.Instance?.UpdateRespawnCounter(Mathf.CeilToInt(remainingTime));
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        if (teamNumber == -1)
        {
            Debug.LogError("PlayerHealth: teamNumber nie ustawiony podczas próby odrodzenia!");
            GameSpawnManager.Instance?.StartSpectateMode();
            yield break;
        }

        if (GameSpawnManager.Instance != null && GameSpawnManager.Instance.IsTeamBaseAlive(teamNumber))
        {
            // Baza wci¹¿ ¿yje: odrodzenie
            currentHealth = maxHealth;
            _isAlive = true;
            GameSpawnManager.Instance.ShowRespawnUI(false);
            gameObject.SetActive(true);
            GameSpawnManager.Instance.RespawnPlayer(gameObject);
            SendPlayerState(true);
        }
        else
        {
            GameSpawnManager.Instance?.StartSpectateMode();
        }
    }

    private void SendPlayerState(bool alive)
    {
        if (!isLocalPlayer) return;

        float velX = 0f;
        float velY = 0f;

        if (alive && TryGetComponent<Rigidbody2D>(out var rb))
        {
            velX = rb.linearVelocity.x;
            velY = rb.linearVelocity.y;
        }

        PlayerStateMessage msg = new()
        {
            steamId = SteamUser.GetSteamID().m_SteamID,
            posX = transform.position.x,
            posY = transform.position.y,
            rot = transform.rotation.eulerAngles.z,
            velX = velX,
            velY = velY,
            emmitingTrail = false,
            isAlive = alive
        };

        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.PlayerState;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);

        if (SteamNetworkingManager.Instance != null)
        {
            SteamNetworkingManager.Instance.BroadcastPacket(packet, EP2PSend.k_EP2PSendReliable);
        }
    }
}