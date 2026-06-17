using System.Collections;
using UnityEngine;
using StarterAssets;
using Cinemachine;

public class PlayerControl : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator anim;
    [SerializeField] private ThirdPersonController thirdPersonController;

    [Header("Combat")]
    public Transform target;
    [SerializeField] private Transform attackPos;

    [SerializeField] private float quickAttackDeltaDistance;
    [SerializeField] private float heavyAttackDeltaDistance;

    [SerializeField] private float quickAttackStaminaCost = 10f;
    [SerializeField] private float heavyAttackStaminaCost = 25f;

    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float airknockbackForce = 10f;

    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float reachTime = 0.3f;

    [SerializeField] private LayerMask enemyLayer;

    [Header("Hitstop & Camera Shake Settings")]
    [SerializeField] private float quickAttackHitstopDuration = 0.07f;
    [SerializeField] private float heavyAttackHitstopDuration = 0.15f;

    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float quickShakeForce = 0.5f;
    [SerializeField] private float heavyShakeForce = 1.5f;

    [Header("Sword Mechanics")]
    public bool hasSwordEquipped = true;
    public bool isSwordSheathed = true;
    private bool isSheathingAnim = false;

    [SerializeField] private GameObject swordInHand;
    [SerializeField] private GameObject swordInSheath;

    private bool isHitstopping = false;
    private bool isAttacking = false;

    [Header("Debug")]
    [SerializeField] private bool debug;

    private EnemyAI oldTarget;
    private EnemyAI currentTarget;

    private Coroutine moveRoutine;
    private Coroutine rotateRoutine;
    private Coroutine hitstopRoutine;
    private Coroutine sheathFailsafeRoutine; // Coroutine baru untuk pengaman

    private int currentAttackState = 0;

    void Start()
    {
        // Setup visual awal saat game dimulai
        if (hasSwordEquipped)
        {
            if (isSwordSheathed) SheathSwordVisual();
            else EquipSwordVisual();
        }
    }

    void Update()
    {
        if (isHitstopping) return;

        HandleInput();
    }

    private void FixedUpdate()
    {
        if (target == null || isHitstopping) return;

        if (Vector3.Distance(transform.position, target.position) >= TargetDetectionControl.instance.detectionRange)
        {
            NoTarget();
        }
    }

    void HandleInput()
    {
        if (isSheathingAnim) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) Attack(0);
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.K)) Attack(1);

        if (hasSwordEquipped && Input.GetKeyDown(KeyCode.R))
        {
            ToggleSwordSheath();
        }
    }

    public void ToggleSwordSheath()
    {
        if (isAttacking || isSheathingAnim) return;

        isSheathingAnim = true;
        TargetDetectionControl.instance.canChangeTarget = false;

        // --------------------

        if (isSwordSheathed)
        {
            anim.SetTrigger("DrawSword");
            anim.SetBool("isArmed", true);
        }
        else
        {
            anim.SetTrigger("SheathSword");
            anim.SetBool("isArmed", false); // Dari diskusi kita sebelumnya
        }

        if (sheathFailsafeRoutine != null) StopCoroutine(sheathFailsafeRoutine);
        sheathFailsafeRoutine = StartCoroutine(SheathFailsafe());
    }

    // Failsafe: Kalau Animation Event gagal/lupa dipasang, jalankan ini otomatis setelah 2 detik
    private IEnumerator SheathFailsafe()
    {
        yield return new WaitForSeconds(2f); // Sesuaikan durasi ini dengan panjang animasimu

        if (isSheathingAnim)
        {
            Debug.LogWarning("Failsafe Aktif! Kamu sepertinya belum menaruh Animation Event FinishSheathAnimation() di klip animasimu.");
            FinishSheathAnimation();
        }
    }

    public void FinishSheathAnimation()
    {
        isSheathingAnim = false;
        isSwordSheathed = !isSwordSheathed;

        TargetDetectionControl.instance.canChangeTarget = true;

        if (sheathFailsafeRoutine != null) StopCoroutine(sheathFailsafeRoutine);
    }

    public void EquipSwordVisual()
    {
        if (swordInHand != null) swordInHand.SetActive(true);
        if (swordInSheath != null) swordInSheath.SetActive(false);
    }

    public void SheathSwordVisual()
    {
        if (swordInHand != null) swordInHand.SetActive(false);
        if (swordInSheath != null) swordInSheath.SetActive(true);
    }

    public void Attack(int attackState)
    {
        if (isAttacking) return;
        if (target == null) return;

        if (hasSwordEquipped && isSwordSheathed)
        {
            return;
        }

        // --- CEK DAN KURANGI STAMINA ---
        float staminaCost = (attackState == 0) ? quickAttackStaminaCost : heavyAttackStaminaCost;
        if (PlayerStatus.Instance.stamina < staminaCost)
        {
            Debug.Log("Stamina tidak cukup untuk menyerang!");
            return; // Batal menyerang jika stamina kurang
        }

        PlayerStatus.Instance.UseStamina(staminaCost);
        // -------------------------------

        isAttacking = true;
        currentAttackState = attackState;
        thirdPersonController.DisableMovement = true;
        TargetDetectionControl.instance.canChangeTarget = false;

        if (attackState == 0) QuickAttack();
        else HeavyAttack();
    }

    void QuickAttack()
    {
        int attackIndex = Random.Range(1, 4);

        switch (attackIndex)
        {
            case 1: MoveTowardsTarget(target.position, quickAttackDeltaDistance, "punch"); break;
            case 2: MoveTowardsTarget(target.position, quickAttackDeltaDistance, "kick"); break;
            case 3: MoveTowardsTarget(target.position, quickAttackDeltaDistance, "mmakick"); break;
        }
    }

    void HeavyAttack()
    {
        int attackIndex = Random.Range(1, 3);

        if (attackIndex == 1) anim.SetBool("heavyAttack1", true);
        else anim.SetBool("heavyAttack2", true);

        FaceThis(target.position);
    }

    public void ResetAttack()
    {
        anim.SetBool("punch", false);
        anim.SetBool("kick", false);
        anim.SetBool("mmakick", false);
        anim.SetBool("heavyAttack1", false);
        anim.SetBool("heavyAttack2", false);

        thirdPersonController.DisableMovement = false;
        TargetDetectionControl.instance.canChangeTarget = true;

        isAttacking = false;
    }

    public void PerformAttack()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPos.position, attackRange, enemyLayer);
        bool hasHitTarget = false;

        foreach (Collider enemy in hitEnemies)
        {
            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            EnemyAI enemyBase = enemy.GetComponent<EnemyAI>();

            if (enemyRb != null)
            {
                hasHitTarget = true;

                Vector3 knockDir = enemy.transform.position - transform.position;
                knockDir.y = airknockbackForce;

                enemyRb.AddForce(knockDir.normalized * knockbackForce, ForceMode.Impulse);

                if (enemyBase != null) 
                {
                    enemyBase.SpawnHitVfx(enemyBase.transform.position);
                    
                    // Hitung damage
                    float damageAmount = (currentAttackState == 0) ? PlayerStatus.Instance.damage1 : PlayerStatus.Instance.damage2;
                    enemyBase.TakeDamage(damageAmount);
                }
            }
        }

        if (hasHitTarget)
        {
            float duration = (currentAttackState == 0) ? quickAttackHitstopDuration : heavyAttackHitstopDuration;
            TriggerHitstop(duration);

            if (impulseSource != null)
            {
                float force = (currentAttackState == 0) ? quickShakeForce : heavyShakeForce;
                impulseSource.GenerateImpulseWithForce(force);
            }
        }
    }

    public void TriggerHitstop(float duration)
    {
        if (hitstopRoutine != null) StopCoroutine(hitstopRoutine);
        hitstopRoutine = StartCoroutine(HitstopCoroutine(duration));
    }

    private IEnumerator HitstopCoroutine(float duration)
    {
        isHitstopping = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isHitstopping = false;
    }

    public void ChangeTarget(Transform target_)
    {
        if (target == target_ || isHitstopping) return;

        if (oldTarget != null) oldTarget.ActiveTarget(false);

        target = target_;
        currentTarget = target_.GetComponent<EnemyAI>();
        oldTarget = currentTarget;

        if (currentTarget != null) currentTarget.ActiveTarget(true);
    }

    public void NoTarget()
    {
        if (isHitstopping) return;

        if (currentTarget != null) currentTarget.ActiveTarget(false);

        currentTarget = null;
        oldTarget = null;
        target = null;
    }

    public void MoveTowardsTarget(Vector3 targetPos, float deltaDistance, string animName)
    {
        PerformAttackAnimation(animName);
        FaceThis(targetPos);

        Vector3 finalPos = TargetOffset(targetPos, deltaDistance);
        finalPos.y = transform.position.y;

        if (moveRoutine != null) StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveTo(finalPos, reachTime));
    }

    public void GetClose()
    {
        if (target == null && oldTarget == null) return;

        Vector3 t = target != null ? target.position : oldTarget.transform.position;
        FaceThis(t);

        Vector3 finalPos = TargetOffset(t, 1.4f);
        finalPos.y = transform.position.y;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(finalPos, 0.2f));
    }

    IEnumerator MoveTo(Vector3 targetPos, float duration)
    {
        CharacterController cc = GetComponent<CharacterController>();
        Vector3 startPos = transform.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            
            // Hitung posisi selanjutnya berdasarkan interpolasi waktu (Lerp)
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, time / duration);
            
            // Hitung jarak yang harus ditempuh frame ini
            Vector3 moveDelta = nextPos - transform.position;
            
            if (cc != null && cc.enabled)
            {
                // Berikan gravitasi secukupnya agar menempel di tanah (jika isGrounded)
                // Jika sedang melayang, beri gravitasi penuh.
                // Ini mencegah Player menginjak musuh dan membenamkannya ke tanah!
                if (!cc.isGrounded) moveDelta.y -= 9.81f * Time.deltaTime;
                else moveDelta.y -= 1f * Time.deltaTime;

                // Gunakan fungsi Move bawaan Unity agar menabrak collider dengan benar
                cc.Move(moveDelta);
            }
            else
            {
                transform.position = nextPos;
            }

            yield return null;
        }
    }

    void PerformAttackAnimation(string animName)
    {
        anim.SetBool(animName, true);
    }

    public Vector3 TargetOffset(Vector3 targetPos, float delta)
    {
        return Vector3.MoveTowards(targetPos, transform.position, delta);
    }

    public void FaceThis(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;

        if (dir == Vector3.zero) return;

        Quaternion rot = Quaternion.LookRotation(dir);

        if (rotateRoutine != null) StopCoroutine(rotateRoutine);
        rotateRoutine = StartCoroutine(RotateTo(rot, 0.2f));
    }

    IEnumerator RotateTo(Quaternion targetRot, float duration)
    {
        Quaternion startRot = transform.rotation;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, time / duration);
            yield return null;
        }

        transform.rotation = targetRot;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }
}