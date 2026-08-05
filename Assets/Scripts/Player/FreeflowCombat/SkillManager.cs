using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [Header("Manager Settings")]
    public bool canUseSkills = true;
    
    [Header("Assigned Skills (Auto-filled if empty)")]
    public List<SkillBase> skills = new List<SkillBase>();

    void Start()
    {
        // Auto-fetch all SkillBase components attached to this GameObject
        if (skills.Count == 0)
        {
            skills.AddRange(GetComponents<SkillBase>());
        }
    }

    void Update()
    {
        if (!canUseSkills) return;

        foreach (var skill in skills)
        {
            if (Input.GetKeyDown(skill.keyBind))
            {
                skill.ActivateSkill();
            }
        }
    }
}
