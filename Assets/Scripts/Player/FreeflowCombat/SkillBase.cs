using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    [Header("Skill Settings")]
    public string skillName;
    public KeyCode keyBind;
    public float manaCost;

    /// <summary>
    /// Dipanggil oleh SkillManager saat tombol ditekan
    /// </summary>
    public abstract void ActivateSkill();
}
