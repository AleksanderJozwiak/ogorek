using Steamworks;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    public static RemotePlayerManager Instance;

    private Dictionary<ulong, GameObject> remotePlayers = new();

    void Awake() => Instance = this;

    public void UpdateRemotePlayer(PlayerStateMessage msg)
    {
        if (!remotePlayers.TryGetValue(msg.steamId, out GameObject player))
        {
            if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

            string slotMeta = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, new CSteamID(msg.steamId), "slot");

            if (string.IsNullOrEmpty(slotMeta)) return;
            string[] split = slotMeta.Split('_');
            int teamNum = int.Parse(split[0]);
            GameObject playerPrefab = Resources.Load<GameObject>($"PlayerShip_{teamNum}");
            player = Instantiate(playerPrefab, new Vector2(msg.posX, msg.posY), Quaternion.Euler(0, 0, msg.rot));
            remotePlayers[msg.steamId] = player;
            player.GetComponent<PlayerMovement>().enabled = false; // disable input on remotes
        }

        // Smooth interpolation instead of snapping:
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.position = Vector2.Lerp(rb.position, new Vector2(msg.posX, msg.posY), 0.2f);
        rb.rotation = msg.rot;
        rb.linearVelocity = new Vector2(msg.velX, msg.velY);
    }

    public void SpawnRemoteBullet(ShootMessage msg)
    {
        if (!remotePlayers.TryGetValue(msg.steamId, out GameObject player)) return;

        GameObject go = PoolManager.Instance.PoolMap[PoolCategory.Bullets].Get();
        go.transform.SetPositionAndRotation(new Vector2(msg.posX, msg.posY), Quaternion.identity);

        var rb = go.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 dir = new Vector2(msg.dirX, msg.dirY).normalized;
        rb.AddForce(dir * 8f, ForceMode2D.Impulse); // use same bullet speed as local
    }

    public void RemoveRemotePlayer(ulong steamId)
    {
        if (remotePlayers.TryGetValue(steamId, out GameObject player))
        {
            Destroy(player);
            remotePlayers.Remove(steamId);
        }
    }

}
