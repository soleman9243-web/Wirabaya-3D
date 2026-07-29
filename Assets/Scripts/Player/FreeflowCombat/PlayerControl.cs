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

    [Header("Attack Input Settings")]
    [Tooltip("Durasi menahan tombol (dalam detik) untuk memicu Heavy Attack")]
    [SerializeField] private float heavyAttackHoldDuration = 0.4f;
    private float attackHoldTime = 0f;
    private bool isHoldingAttack = false;

    [Header("Hitstop & Camera Shake Settings")]
    [SerializeField] private float quickAttackHitstopDuration = 0.07f;
    [SerializeField] private float heavyAttackHitstopDuration = 0.15f;

    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float quickShakeForce = 0.5f;
    [SerializeField] private float heavyShakeForce = 1.5f;

    [Header("Sword Mechanics")]
    public bool hasSwordEquipped = false;
    public bool isSwordSheathed = true;
    private bool isSheathingAnim = false;

    [SerializeField] private GameObject swordInHand;
    [SerializeField] private GameObject swordInSheath;

    [Header("Attack Trail")]
    [Tooltip("TrailRenderer yang ada di sword/weapon GameObject")]
    [SerializeField] private TrailRenderer attackTrail;
    [Tooltip("Berapa detik trail tetap muncul setelah serangan selesai sebelum hilang")]
    [SerializeField] private float trailHideDelay = 0.3f;

    [Header("Ghost Trail Effect")]
    [Tooltip("Script GhostTrail untuk efek bayangan karakter")]
    [SerializeField] private GhostTrail ghostTrail;

    [Header("Anime Awakening (Skill)")]
    public bool canUseSkills = true;
    public bool isAwakened = false;
    public float awakeningDuration = 15f;
    public float awakeningManaCost = 100f; // Max mana required
    public Material normalTrailMat;
    public Material animeTrailMat;
    private Coroutine awakeningRoutine;
    private float originalTrailTime;

    [Header("Item System")]
    public bool isHoldingItem = false;

    private bool isHitstopping = false;
    private bool isAttacking = false;

    [Header("Debug")]
    [SerializeField] private bool debug;

    private EnemyAI oldTarget;
    private EnemyAI currentTarget;

    private BossAI oldBossTarget;
    private BossAI currentBossTarget;

    private Coroutine moveRoutine;
    private Coroutine rotateRoutine;
    private Coroutine hitstopRoutine;
    private Coroutine sheathFailsafeRoutine; // Coroutine baru untuk pengaman
    private Coroutine trailHideRoutine;      // Coroutine untuk hide trail setelah delay

    private int currentAttackState = 0;

    void Start()
    {
        // Setup visual awal saat game dimulai
        if (hasSwordEquipped)
        {
            if (isSwordSheathed) 
            {
                SheathSwordVisual();
            }
            else 
            {
                EquipSwordVisual();
                if (anim != null) anim.SetBool("isArmed", true);
            }
        }
        else
        {
            if (swordInHand != null) swordInHand.SetActive(false);
            if (swordInSheath != null) swordInSheath.SetActive(false);
        }

        // Pastikan trail mati di awal
        if (attackTrail != null)
        {
            attackTrail.emitting = false;

            // --- Peningkatan Visibilitas Trail Senjata (Blur) untuk Grafik Rendah ---
            // Memaksimalkan alpha pada gradient trail agar solid dan terlihat tebal tanpa efek Bloom
            Color startCol = attackTrail.startColor;
            startCol.a = 1f; 
            attackTrail.startColor = startCol;

            Color endCol = attackTrail.endColor;
            endCol.a = Mathf.Max(endCol.a, 0.5f); 
            attackTrail.endColor = endCol;

            // Memperpanjang durasi ekor trail sedikit agar tidak putus-putus atau terlalu pendek saat FPS rendah
            attackTrail.time = Mathf.Max(attackTrail.time, 0.35f);

            // Opsional: Memperlebar trail sedikit agar lebih tegas
            attackTrail.widthMultiplier = attackTrail.widthMultiplier * 1.3f;

            if (attackTrail.material != null && attackTrail.material.HasProperty("_BaseColor"))
            {
                Color matColor = attackTrail.material.GetColor("_BaseColor");
                matColor.a = 1f;
                attackTrail.material.SetColor("_BaseColor", matColor);
            }
        }
        if (ghostTrail != null)
        {
            ghostTrail.StopTrail();
        }
    }

    public void UnlockWeapon()
    {
        hasSwordEquipped = true;
        isSwordSheathed = true;
        SheathSwordVisual();
    }

    void Update()
    {
        if (isHitstopping) return;

        HandleInput();

        // Failsafe: Pastikan bayangan blur mati kalau lagi nggak nyerang
        if (!isAttacking && ghostTrail != null)
        {
            ghostTrail.StopTrail();
        }
    }

    private void FixedUpdate()
    {
        if (target == null || isHitstopping) return;

        if (Vector3.Distance(transform.position, target.position) >= TargetDetectionControl.instance.detectionRange)
        {
            NoTarget();
        }
    }

    private Transform lockedTargetOnPress;

    void HandleInput()
    {
        if (isSheathingAnim) return;

        // Mulai hold input serangan (Klik Kiri / J)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            isHoldingAttack = true;
            attackHoldTime = 0f;
            
            // Simpan target awal saat pencetan pertama (hanya akan digunakan jika jadi Heavy Attack)
            lockedTargetOnPress = target;
        }

        // Selama tombol ditahan
        if (isHoldingAttack && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.J)))
        {
            attackHoldTime += Time.deltaTime;
            
            // Jika ditahan lebih lama dari batas waktu, otomatis memicu Heavy Attack
            if (attackHoldTime >= heavyAttackHoldDuration)
            {
                isHoldingAttack = false;
                
                // Panggil kembali target yang pertama kali di-lock sebelum nge-hold
                if (lockedTargetOnPress != null)
                {
                    ChangeTarget(lockedTargetOnPress);
                }

                Attack(1); // 1 = Heavy Attack
            }
        }

        // Jika dilepas sebelum batas waktu tercapai, picu Quick Attack
        if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.J))
        {
            if (isHoldingAttack)
            {
                isHoldingAttack = false;
                if (attackHoldTime < heavyAttackHoldDuration)
                {
                    // Quick Attack menggunakan target saat ini (bebas pindah dengan cepat)
                    Attack(0); // 0 = Quick Attack
                }
            }
        }

        if (hasSwordEquipped && Input.GetKeyDown(KeyCode.R))
        {
            ToggleSwordSheath();
        }
        
        HandleAwakeningInput();
    }

    private void HandleAwakeningInput()
    {
        if (Input.GetKeyDown(KeyCode.V) && !isAwakened)
        {
            if (canUseSkills)
            {
                if (currentTarget != null || currentBossTarget != null) 
                {
                    if (PlayerStatus.Instance != null && PlayerStatus.Instance.mana >= awakeningManaCost)
                    {
                        PlayerStatus.Instance.UseMana(awakeningManaCost);
                        ActivateAwakening();
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
            else
            {
                Debug.Log("Skill dinonaktifkan pada scene ini.");
            }
        }
    }

    public void ActivateAwakening()
    {
        isAwakened = true;
        
        // Ganti material pedang ke Anime Shader
        if (attackTrail != null && animeTrailMat != null)
        {
            normalTrailMat = attackTrail.material; // Simpan material asli
            originalTrailTime = attackTrail.time;  // Simpan durasi asli

            attackTrail.material = animeTrailMat;
            
            // Lebarkan trail secukupnya (2.5x). Jangan terlalu besar agar tidak overlap & muter-muter.
            attackTrail.widthMultiplier *= 2.5f; 
            
            // Perlama durasi hilangnya trail agar efek ombaknya terlihat mengular
            attackTrail.time = originalTrailTime * 3f; 
        }
        
        Debug.Log("AWAKENING MODE AKTIF!");
        
        if (awakeningRoutine != null) StopCoroutine(awakeningRoutine);
        awakeningRoutine = StartCoroutine(AwakeningTimerRoutine());
    }

    private IEnumerator AwakeningTimerRoutine()
    {
        yield return new WaitForSeconds(awakeningDuration);
        
        isAwakened = false;
        
        if (attackTrail != null && normalTrailMat != null)
        {
            attackTrail.material = normalTrailMat;
            attackTrail.widthMultiplier /= 2.5f;
            attackTrail.time = originalTrailTime; // Kembalikan ke durasi awal
        }
        
        Debug.Log("Awakening Mode Selesai.");
    }

    public void ToggleSwordSheath()
    {
        if (isAttacking || isSheathingAnim || isHoldingItem) return;

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

        if (isHoldingItem)
        {
            Debug.Log("Tidak bisa menyerang karena sedang memegang item!");
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

        if (attackIndex == 1) MoveTowardsTarget(target.position, heavyAttackDeltaDistance, "heavyAttack1");
        else MoveTowardsTarget(target.position, heavyAttackDeltaDistance, "heavyAttack2");
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

        // Sembunyikan trail setelah delay yang bisa diatur
        HideTrailWithDelay();

        isAttacking = false;
    }

    // ─── Trail Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil lewat Animation Event saat frame serangan dimulai.
    /// Nyalakan trail di sini supaya muncul hanya saat animasi menyerang.
    /// </summary>
    public void ShowAttackTrail()
    {
        if (attackTrail != null)
        {
            // Batalkan coroutine hide yang mungkin sedang berjalan
            if (trailHideRoutine != null) StopCoroutine(trailHideRoutine);

            attackTrail.Clear();      // Bersihkan sisa trail sebelumnya
            attackTrail.emitting = true;
        }

        if (ghostTrail != null)
        {
            ghostTrail.StartTrail();
        }
    }

    /// <summary>
    /// Mulai coroutine untuk menyembunyikan trail setelah trailHideDelay detik.
    /// Dipanggil otomatis dari ResetAttack().
    /// </summary>
    private void HideTrailWithDelay()
    {
        if (attackTrail != null)
        {
            if (trailHideRoutine != null) StopCoroutine(trailHideRoutine);
            trailHideRoutine = StartCoroutine(HideTrailRoutine());
        }

        if (ghostTrail != null)
        {
            ghostTrail.StopTrail();
        }
    }

    private IEnumerator HideTrailRoutine()
    {
        yield return new WaitForSeconds(trailHideDelay);
        if (attackTrail != null) attackTrail.emitting = false;
        trailHideRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────

    public void PerformAttack()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPos.position, attackRange, enemyLayer);
        bool hasHitTarget = false;

        foreach (Collider enemy in hitEnemies)
        {
            Rigidbody enemyRb = enemy.GetComponentInParent<Rigidbody>();
            EnemyAI enemyBase = enemy.GetComponentInParent<EnemyAI>();
            BossAI bossBase = enemy.GetComponentInParent<BossAI>();

            if (enemyRb != null || bossBase != null) // boss mungkin ditaruh collider di parent
            {
                hasHitTarget = true;

                if (enemyRb != null)
                {
                    Vector3 knockDir = enemy.transform.position - transform.position;
                    knockDir.y = airknockbackForce;

                    // Bos biasanya tidak kena knockback kecil
                    if (bossBase == null)
                    {
                        enemyRb.AddForce(knockDir.normalized * knockbackForce, ForceMode.Impulse);
                    }
                }

                if (enemyBase != null) 
                {
                    enemyBase.SpawnHitVfx(enemyBase.transform.position);
                    
                    // Hitung damage
                    float damageAmount = (currentAttackState == 0) ? PlayerStatus.Instance.damage1 : PlayerStatus.Instance.damage2;
                    if (isAwakened) damageAmount *= 2f;
                    enemyBase.TakeDamage(damageAmount);
                }
                else if (bossBase != null)
                {
                    bossBase.SpawnHitVfx(bossBase.transform.position);

                    // Hitung damage bos
                    float damageAmount = (currentAttackState == 0) ? PlayerStatus.Instance.damage1 : PlayerStatus.Instance.damage2;
                    if (isAwakened) damageAmount *= 2f;
                    bossBase.TakeDamage(damageAmount);
                }
            }
        }

        if (hasHitTarget)
        {
            float duration = (currentAttackState == 0) ? quickAttackHitstopDuration : heavyAttackHitstopDuration;
            if (isAwakened) duration *= 1.5f; // Hitstop lebih lama saat awakening
            TriggerHitstop(duration);

            if (impulseSource != null)
            {
                float force = (currentAttackState == 0) ? quickShakeForce : heavyShakeForce;
                if (isAwakened) force *= 2f; // Camera shake lebih heboh
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
        if (oldBossTarget != null) oldBossTarget.ActiveTarget(false);

        target = target_;
        currentTarget = target_.GetComponent<EnemyAI>();
        currentBossTarget = target_.GetComponent<BossAI>();
        
        oldTarget = currentTarget;
        oldBossTarget = currentBossTarget;

        if (currentTarget != null) currentTarget.ActiveTarget(true);
        if (currentBossTarget != null) currentBossTarget.ActiveTarget(true);
    }

    public void NoTarget()
    {
        if (isHitstopping) return;

        if (currentTarget != null) currentTarget.ActiveTarget(false);
        if (currentBossTarget != null) currentBossTarget.ActiveTarget(false);

        currentTarget = null;
        oldTarget = null;

        currentBossTarget = null;
        oldBossTarget = null;
        
        target = null;
    }

    public void MoveTowardsTarget(Vector3 targetPos, float deltaDistance, string animName)
    {
        PerformAttackAnimation(animName);
        FaceThis(targetPos);

        Vector3 finalPos = TargetOffset(targetPos, deltaDistance);
        // Membiarkan finalPos menggunakan Y dari target agar bisa menyerang miring (ke atas/bawah)

        if (moveRoutine != null) StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveTo(finalPos, reachTime));
    }

    public void GetClose()
    {
        if (target == null && oldTarget == null) return;

        Vector3 t = target != null ? target.position : oldTarget.transform.position;
        FaceThis(t);

        Vector3 finalPos = TargetOffset(t, 1.4f);
        // Membiarkan Y

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
            
            // Lerp 3D penuh (X, Y, Z)
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, time / duration);
            
            Vector3 moveDelta = nextPos - transform.position;
            
            if (cc != null && cc.enabled)
            {
                // JANGAN kurangi moveDelta.y dengan gravitasi selama dash berlangsung!
                // Supaya player bisa benar-benar melesat lurus secara diagonal ke arah target (atas maupun bawah)
                // tanpa ditarik turun oleh gravitasi palsu yang membuatnya stuck/ngambang salah tempat.
                
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
        // Hitung arah mundur di bidang XZ saja agar saat menyerang dari atas, 
        // jarak mundurnya murni secara horizontal (tidak landing di atas kepala musuh)
        Vector3 playerPosXZ = new Vector3(transform.position.x, targetPos.y, transform.position.z);
        return Vector3.MoveTowards(targetPos, playerPosXZ, delta);
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