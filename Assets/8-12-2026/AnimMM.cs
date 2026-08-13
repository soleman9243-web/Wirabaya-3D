using UnityEngine;

public class IdleAnimationController : MonoBehaviour
{
    private Animator animator;

    void Update()
    {
        animator = GetComponent<Animator>();
        
        // Mainkan animasi Idle saat game dimulai
        animator.Play("Idle");
    }
}