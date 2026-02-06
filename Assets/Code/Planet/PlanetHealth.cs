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
    private CSteamID lastAttacker;

    private void Start()
    {
        currentHealth = maxHealth;
        if (TryGetComponent(out spriteRenderer))
        {
            hittableMaterial = Instantiate(spriteRenderer.material);
            spriteRenderer.material = hittableMaterial;
        }

        // Informujemy Managera, ¿e baza stoi
        StatsManager.Instance?.SetPlanetState(teamNumber, true);

        // Rejestracja w SpawnManagerze (tylko do celów respawnu, nie logiki win condition)
        if (GameSpawnManager.Instance != null)
        {
            GameSpawnManager.Instance.SetTeamBaseState(teamNumber, true);
            GameSpawnManager.Instance.RegisterTeamBase(teamNumber, this);
        }
    }

    public void TakeDamage(float damage, CSteamID attackerId)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        transform.localScale -= new Vector3(damage * 0.01f, damage * 0.01f, 0);

        if (attackerId.IsValid())
        {
            lastAttacker = attackerId;
            StatsManager.Instance?.RecordPlanetDamage(attackerId, damage);
        }

        StartCoroutine(HitFlashEffect());

        if (currentHealth <= 0)
            DestroyPlanet();
    }

    private IEnumerator HitFlashEffect()
    {
        // ... (Twój efekt wizualny bez zmian) ...
        if (hittableMaterial != null)
        {
            hittableMaterial.SetFloat("_HitColorAmount", 1f);
            yield return new WaitForSeconds(0.1f);
            hittableMaterial.SetFloat("_HitColorAmount", 0f);
        }
    }

    private void DestroyPlanet()
    {
        // 1. Zapisz statystyki (bed destroyed)
        if (lastAttacker.IsValid())
        {
            StatsManager.Instance?.RecordPlanetKill(lastAttacker);
        }

        // 2. Powiadom StatsManager -> To NIE koñczy gry, tylko blokuje respawn i sprawdza warunki
        StatsManager.Instance?.SetPlanetState(teamNumber, false);

        // 3. Powiadom GameSpawnManager (tylko ¿eby wiedzia³, ¿e nie mo¿na tu respiæ)
        GameSpawnManager.Instance?.SetTeamBaseState(teamNumber, false);

        // UWAGA: Jeœli UnregisterTeamBase w Twoim kodzie automatycznie sprawdza "Czy liczba baz == 1" i koñczy grê,
        // to zakomentuj poni¿sz¹ liniê! Jeœli s³u¿y tylko do cleanupu, zostaw.
        // Bezpieczniej jest polegaæ na logice w StatsManagerze.
        GameSpawnManager.Instance?.UnregisterTeamBase(teamNumber);

        // 4. Sieæ i cleanup
        SendTeamBaseState(teamNumber, false);
        Destroy(gameObject);
    }

    // ... (SendTeamBaseState bez zmian) ...
    private void SendTeamBaseState(int teamNum, bool alive)
    {
        // (Twój kod sieciowy)
        TeamBaseMessage msg = new() { teamNumber = teamNum, baseAlive = alive };
        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.TeamBaseDestroyed;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);
        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
            SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
    }
}