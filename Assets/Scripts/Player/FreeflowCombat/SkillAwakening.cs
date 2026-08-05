using UnityEngine;

public class SkillAwakening : SkillBase
{
    [Header("Dependencies")]
    public PlayerControl playerControl;

    private void Reset()
    {
        skillName = "Awakening";
        keyBind = KeyCode.V;
        manaCost = 100f;
    }

    void Start()
    {
        if (playerControl == null)
        {
            playerControl = GetComponent<PlayerControl>();
        }
    }

    public override void ActivateSkill()
    {
        if (playerControl == null)
        {
            Debug.LogError("PlayerControl belum di-assign di SkillAwakening!");
            return;
        }

        if (playerControl.isAwakened) 
        {
            Debug.Log("Awakening sudah aktif!");
            return; // Cegah aktivasi ulang jika sudah awakening
        }

        if (playerControl.target != null || (PlayerStatus.Instance != null && PlayerStatus.Instance.mana >= manaCost))
        {
            // Untuk memastikan kita memiliki target seperti di script lama, 
            // Kita harus mengecek apakah target sudah di-lock atau ada boss target.
            // Namun, untuk lebih aman, kita serahkan pengecekan mana ke Skill,
            // atau langsung panggil fungsi awakening dari playerControl.
            
            bool hasTarget = playerControl.target != null; // Cek logic target yang lebih detail jika perlu

            if (hasTarget)
            {
                if (PlayerStatus.Instance != null && PlayerStatus.Instance.mana >= manaCost)
                {
                    PlayerStatus.Instance.UseMana(manaCost);
                    playerControl.ActivateAwakening();
                }
                else
                {
                    Debug.Log("Mana tidak cukup untuk Awakening!");
                }
            }
            else
            {
                Debug.Log("Awakening harus memiliki target!");
            }
        }
    }
}
