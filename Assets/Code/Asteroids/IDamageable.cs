using Steamworks;

public interface IDamageable
{
    void TakeDamage(float damage, CSteamID attackerId);
}
