using UnityEngine;

/// <summary>
/// Mengambil dan mengembalikan data player (health, stamina, mana, posisi) dari PlayerStatus.
/// Pasang di GameObject yang sama dengan PlayerStatus, atau di GameObject yang persist.
/// </summary>
public class PlayerSaveProvider : MonoBehaviour, ISaveDataProvider
{
    public void PopulateSaveData(SaveData data)
    {
        if (PlayerStatus.Instance == null)
        {
            Debug.LogWarning("[PlayerSaveProvider] PlayerStatus.Instance null, skip populate.");
            return;
        }

        PlayerStatus ps = PlayerStatus.Instance;

        data.playerHealth = ps.health;
        data.playerMaxHealth = ps.maxHealth;
        data.playerStamina = ps.stamina;
        data.playerMaxStamina = ps.maxStamina;
        data.playerMana = ps.mana;
        data.playerMaxMana = ps.maxMana;

        data.playerPosition = ps.transform.position;
        data.playerRotation = ps.transform.rotation;
    }

    public void RestoreFromSaveData(SaveData data)
    {
        if (PlayerStatus.Instance == null)
        {
            Debug.LogWarning("[PlayerSaveProvider] PlayerStatus.Instance null, skip restore.");
            return;
        }

        PlayerStatus ps = PlayerStatus.Instance;

        // Restore stats menggunakan setter methods
        ps.SetHealth(data.playerHealth);
        ps.SetStamina(data.playerStamina);
        ps.SetMana(data.playerMana);

        // Restore posisi — perlu disable CharacterController dulu
        CharacterController cc = ps.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        ps.transform.position = data.playerPosition;
        ps.transform.rotation = data.playerRotation;

        if (cc != null) cc.enabled = true;

        Debug.Log($"[PlayerSaveProvider] Restored — HP:{data.playerHealth} Pos:{data.playerPosition.ToVector3()}");
    }
}
