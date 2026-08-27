using UnityEngine;
using StarterAssets;

/* =============================================================================
 * WeightAnimationScript.cs
 * =============================================================================
 * Script tambahan (Add-on) untuk menjalankan sistem animasi baru.
 * Script ini dirancang agar BEKERJA BARENG dengan ThirdPersonController asli,
 * sehingga TIDAK PERLU menghapus atau mengganti komponen asli.
 * 
 * Menggunakan CrossFade agar transisi langsung dari state APAPUN.
 * Menggunakan DisableMovement dari ThirdPersonController untuk mencegah slide.
 * Menggunakan OnAnimatorMove untuk menerapkan root motion rotation saat turn.
 * ========================================================================== */

namespace StarterAssets.Prototype
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    [RequireComponent(typeof(CharacterController))]
    public class WeightAnimationScript : MonoBehaviour
    {
        [Header("V2 — Better Animation Settings")]
        [Tooltip("Sudut minimum (derajat) untuk memicu animasi idle turn")]
        public float IdleTurnThreshold = 60f;
        
        [Tooltip("Sudut minimum (derajat) untuk memicu animasi 180 turn saat berlari")]
        public float Turn180Threshold = 140f;
        
        [Tooltip("Kecepatan minimum untuk dianggap 'sedang berlari'")]
        public float RunSpeedThreshold = 3.0f;
        
        [Tooltip("Berapa lama (detik) setelah trigger sebelum bisa trigger lagi")]
        public float StopCooldown = 0.5f;

        [Tooltip("Berapa lama (detik) 'ingatan' bahwa karakter baru saja berlari")]
        public float WasRunningMemory = 0.4f;

        [Header("Animation Durations")]
        [Tooltip("Durasi animasi Left Turn / Right Turn dalam detik (sebelum rotasi dipaksa snap)")]
        public float Turn90Duration = 0.85f; // Diperpanjang agar tidak buru-buru
        
        [Tooltip("Durasi animasi Running Turn 180 dalam detik")]
        public float Turn180Duration = 1.2f; // Diperpanjang agar tidak buru-buru

        [Header("Sub-State Machine Name")]
        [Tooltip("Nama sub-state machine V2 di Animator (harus cocok persis)")]
        public string SubStateMachineName = "V2 Locomotion";

        [Header("Animation State Names")]
        [Tooltip("Nama state locomotion utama di Base Layer")]
        public string StateIdleWalkRun = "Idle Walk Run Blend";
        
        [Tooltip("Nama state animasi berhenti dari lari")]
        public string StateRunToStop = "Run To Stop";
        
        [Tooltip("Nama state animasi putar kiri di tempat")]
        public string StateLeftTurn = "Left Turn";
        
        [Tooltip("Nama state animasi putar kanan di tempat")]
        public string StateRightTurn = "Right Turn";
        
        [Tooltip("Nama state animasi putar balik saat lari")]
        public string StateRunningTurn180 = "Running Turn 180";

        private Animator _animator;
        private StarterAssetsInputs _input;
        private CharacterController _controller;
        private ThirdPersonController _tpc; // Referensi ke script asli
        private PlayerControl _playerControl;
        private Camera _mainCamera;        // V2: ANIMATION IDs
        private int _animIDIsSprinting;
        private int _animIDIsArmedRunning;
        private int _animIDSpeed;

        // State hashes
        private int _hashRunToStop;
        private int _hashLeftTurn;
        private int _hashRightTurn;
        private int _hashRunTurn180;

        // STATE TRACKING
        private float _previousYaw;
        private float _stopCooldownTimer;
        private float _currentTurnAngle;
        private bool _isTurnPlaying;
        private float _turnTimer;
        private float _turnTargetYaw; // Arah tujuan saat turn
        private float _turnStartYaw;  // Arah awal saat turn dimulai
        private float _turnDuration;  // Total durasi turn (untuk lerp)


        // "Ingatan" berlari
        private bool _wasRunningRecently;
        private float _wasRunningTimer;

        // Track animasi yang perlu DisableMovement
        private bool _isInRunToStop;
        private bool _isInV2Animation; // True saat animasi V2 yang perlu disable movement

        // Post-turn blend: putar kapsul bertahap setelah Turn 180 selesai
        private bool _isPostTurnBlend;
        private float _postTurnBlendTimer;
        private float _postTurnBlendDuration = 0.15f;
        private float _postTurnBlendStartYaw;
        private float _postTurnBlendTargetYaw;
        private float _postTurnClockwiseDelta; // Selalu positif = selalu clockwise

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _input = GetComponent<StarterAssetsInputs>();
            _controller = GetComponent<CharacterController>();
            _tpc = GetComponent<ThirdPersonController>(); // Referensi ke script asli
            _playerControl = GetComponent<PlayerControl>();
            
            if (Camera.main != null) 
                _mainCamera = Camera.main;
            
            AssignAnimationIDs();
            _previousYaw = transform.eulerAngles.y;
        }

        private void AssignAnimationIDs()
        {
            _animIDIsSprinting = Animator.StringToHash("IsSprinting");
            _animIDIsArmedRunning = Animator.StringToHash("IsArmedRunning");
            _animIDSpeed = Animator.StringToHash("Speed");

            _hashRunToStop = Animator.StringToHash(StateRunToStop);
            _hashLeftTurn = Animator.StringToHash(StateLeftTurn);
            _hashRightTurn = Animator.StringToHash(StateRightTurn);
            _hashRunTurn180 = Animator.StringToHash(StateRunningTurn180);
        }

        private void CrossFadeV2(string stateName, float duration)
        {
            _animator.CrossFade(SubStateMachineName + "." + stateName, duration);
        }

        /// <summary>
        /// Aktifkan DisableMovement pada ThirdPersonController asli
        /// agar karakter tidak bergerak/slide saat animasi V2 jalan
        /// </summary>
        private void SetMovementLock(bool locked)
        {
            if (_tpc != null)
            {
                _tpc.DisableMovement = locked;
            }
            _isInV2Animation = locked;
        }



        private void Update()
        {
            if (_animator == null) return;

            // ================================================================
            // POST-TURN BLEND: Putar kapsul bertahap setelah Turn 180 selesai.
            // Saat CrossFade nge-blend bone rotation dari 180° ke 0°,
            // kita putar kapsul dari 0° ke 180° → total visual tetap 180° → 0 rollback!
            // ================================================================
            if (_isPostTurnBlend)
            {
                _postTurnBlendTimer -= Time.deltaTime;
                // Pakai LINEAR agar sinkron dengan CrossFade yang juga linear
                float progress = 1f - Mathf.Clamp01(_postTurnBlendTimer / _postTurnBlendDuration);
                
                // PAKSA rotasi SELALU KE KANAN (clockwise / positive yaw)
                // karena animasi Turn 180 selalu muter ke kanan.
                // Mathf.LerpAngle kadang ambil jalur kiri (rollback!) — jadi tidak bisa dipakai.
                float newYaw = _postTurnBlendStartYaw + _postTurnClockwiseDelta * progress;
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
                
                if (_postTurnBlendTimer <= 0f)
                {
                    transform.rotation = Quaternion.Euler(0f, _postTurnBlendTargetYaw, 0f);
                    _isPostTurnBlend = false;
                }
            }

            // === Info terkini ===
            float currentYaw = transform.eulerAngles.y;
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            
            bool isMoving = _input.move != Vector2.zero;
            bool isHoldingSword = _playerControl != null && _playerControl.hasSwordEquipped && !_playerControl.isSwordSheathed;
            bool isSprinting = _input.sprint && currentSpeed > 3.0f;

            // Cek state animator saat ini dan state tujuan (jika sedang transisi)
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextStateInfo = _animator.GetNextAnimatorStateInfo(0);

            bool currentlyInRunToStop = stateInfo.shortNameHash == _hashRunToStop || nextStateInfo.shortNameHash == _hashRunToStop;
            bool currentlyInTurn = stateInfo.shortNameHash == _hashLeftTurn 
                                || stateInfo.shortNameHash == _hashRightTurn
                                || stateInfo.shortNameHash == _hashRunTurn180
                                || nextStateInfo.shortNameHash == _hashLeftTurn
                                || nextStateInfo.shortNameHash == _hashRightTurn
                                || nextStateInfo.shortNameHash == _hashRunTurn180;

            // ================================================================
            // EARLY EXIT: RunToStop → Locomotion
            // Jika player mulai gerak lagi, langsung keluar agar tidak slide
            // ================================================================
            if (_isInRunToStop)
            {
                if (isMoving)
                {
                    _animator.CrossFade(StateIdleWalkRun, 0.15f);
                    SetMovementLock(false); // Unlock movement
                    _isInRunToStop = false;
                    Debug.Log("[AnimV2] RunToStop interrupted — player started moving");
                }
                else if (!currentlyInRunToStop)
                {
                    // Animasi selesai secara natural
                    SetMovementLock(false);
                    _isInRunToStop = false;
                }
            }

            // ================================================================
            // EARLY EXIT: Turn → selesai
            // Setelah turn selesai, unlock movement & set rotasi ke arah tujuan
            // ================================================================
            if (_isTurnPlaying)
            {
                // UPDATE TARGET YAW SECARA DINAMIS (Hanya untuk Turn 180)
                // Untuk Turn 90, kita KUNCI arahnya supaya tidak jerk/patah kalau user pencet tombol lain di tengah animasi.
                if (isMoving && _mainCamera != null && _turnDuration == Turn180Duration)
                {
                    Vector3 inputDir = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
                    _turnTargetYaw = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                }

                if (_turnDuration != Turn180Duration)
                {
                    // ============================================
                    // TURN BIASA (Idle Turn Kiri/Kanan ~90°)
                    // Timer-based, kapsul ikut muter mulus via SmoothStep.
                    // ============================================
                    _turnTimer -= Time.deltaTime;
                    
                    float progress = 1f - Mathf.Clamp01(_turnTimer / _turnDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, progress);
                    float newYaw = Mathf.LerpAngle(_turnStartYaw, _turnTargetYaw, smoothT);
                    transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

                    if (_turnTimer <= 0f)
                    {
                        transform.rotation = Quaternion.Euler(0f, _turnTargetYaw, 0f);
                        if (_tpc != null) _tpc.ResetRotationVelocity();
                        SetMovementLock(false);
                        _isTurnPlaying = false;
                    }
                    else if (!currentlyInTurn && _turnTimer < 0.4f)
                    {
                        // Failsafe
                        transform.rotation = Quaternion.Euler(0f, _turnTargetYaw, 0f);
                        if (_tpc != null) _tpc.ResetRotationVelocity();
                        SetMovementLock(false);
                        _isTurnPlaying = false;
                    }
                }
                else
                {
                    // ============================================
                    // TURN 180 (Lari Putar Balik)
                    // Kapsul DIAM selama animasi (Bake Into Pose memutar mesh visual).
                    // Saat selesai, kapsul diputar BERTAHAP via post-turn blend
                    // agar sinkron dengan CrossFade bone de-rotation → 0 rollback.
                    // ============================================
                    
                    bool isInTurn180State = stateInfo.shortNameHash == _hashRunTurn180;
                    float animProgress = isInTurn180State ? stateInfo.normalizedTime : 0f;
                    bool animDone = isInTurn180State && animProgress >= 0.9f;
                    
                    _turnTimer -= Time.deltaTime;
                    bool timerFallback = _turnTimer <= 0f;
                    
                    if (animDone || timerFallback)
                    {
                        // JANGAN snap kapsul sekaligus! Mulai post-turn blend.
                        _isPostTurnBlend = true;
                        _postTurnBlendTimer = _postTurnBlendDuration;
                        _postTurnBlendStartYaw = transform.eulerAngles.y;
                        _postTurnBlendTargetYaw = _turnTargetYaw;
                        
                        // Hitung delta SELALU CLOCKWISE (positif)
                        // Animasi Turn 180 selalu muter ke kanan, jadi kapsul juga harus ke kanan.
                        float rawDelta = _turnTargetYaw - transform.eulerAngles.y;
                        // Normalize ke 0..360 (selalu positif = clockwise)
                        _postTurnClockwiseDelta = ((rawDelta % 360f) + 360f) % 360f;
                        // Kalau deltanya 0 (sudah di posisi target), paksa 360 (full turn)
                        if (_postTurnClockwiseDelta < 1f) _postTurnClockwiseDelta = 360f;
                        
                        // CrossFade ke Locomotion (durasi DETIK ASLI = sinkron dengan post-turn blend)
                        // HARUS pakai CrossFadeInFixedTime, bukan CrossFade!
                        // CrossFade(0.15) = 15% durasi animasi (bisa 0.3 detik kalau animasi 2 detik)
                        // CrossFadeInFixedTime(0.15) = PASTI 0.15 detik = sinkron dengan blend kapsul
                        _animator.CrossFadeInFixedTime(StateIdleWalkRun, _postTurnBlendDuration, 0);
                        
                        if (_tpc != null) _tpc.ResetRotationVelocity();
                        SetMovementLock(false);
                        _isTurnPlaying = false;
                        
                        Debug.Log($"[AnimV2] Turn 180 done — starting post-turn blend to {_turnTargetYaw:F1}°");
                    }
                }
            }

            // === Tracking "baru saja berlari" ===
            if (currentSpeed > RunSpeedThreshold && isMoving)
            {
                _wasRunningRecently = true;
                _wasRunningTimer = WasRunningMemory;
            }
            if (_wasRunningTimer > 0f)
            {
                _wasRunningTimer -= Time.deltaTime;
                if (_wasRunningTimer <= 0f)
                    _wasRunningRecently = false;
            }

            // --- 1. SPRINT DETECTION ---
            // Lari biasa (hanya jika TIDAK memegang pedang)
            bool isSprintingNormal = isSprinting && isMoving && !isHoldingSword;
            _animator.SetBool(_animIDIsSprinting, isSprintingNormal);

            // --- 2. ARMED RUNNING DETECTION ---
            // Lari bawa pedang (berbeda animasinya dari lari biasa)
            // currentSpeed > 3.0f memastikan kalau JALAN (speed ~2.0), dia TIDAK trigger state ini.
            bool isArmedRunning = isHoldingSword && isMoving && currentSpeed > 3.0f;
            _animator.SetBool(_animIDIsArmedRunning, isArmedRunning);

            // --- 3. ALL TURNS (IDLE TURN & 180 TURN) ---
            // Sistem turn terpusat. Berlaku saat dari diam maupun lari.
            if (!_isTurnPlaying && !_isInRunToStop && !_isInV2Animation && _mainCamera != null && _stopCooldownTimer <= 0f)
            {
                if (isMoving)
                {
                    Vector3 inputDir = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
                    float targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float angleDiff = Mathf.DeltaAngle(currentYaw, targetRotation);
                    float absAngleDiff = Mathf.Abs(angleDiff);

                    float animSpeed = _animator.GetFloat(_animIDSpeed);

                    // Prioritas 1: Turn 180 (Hanya dipicu saat BERLARI dan pencet tombol arah belakang)
                    if (absAngleDiff > Turn180Threshold && animSpeed >= RunSpeedThreshold)
                    {
                        CrossFadeV2(StateRunningTurn180, 0.1f);
                        _isTurnPlaying = true;
                        _turnDuration = Turn180Duration;
                        _turnTimer = _turnDuration; 
                        _turnStartYaw = currentYaw;
                        _turnTargetYaw = targetRotation;
                        SetMovementLock(true); // Lock pergerakan selama muter
                        _stopCooldownTimer = StopCooldown;
                        Debug.Log($"[AnimV2] Running Turn 180! AnimSpeed={animSpeed:F1}");
                    }
                    // Prioritas 2: Idle Turn (Hanya dipicu saat karakter lambat/diam)
                    else if (animSpeed < 1.5f) 
                    {
                        if (angleDiff < -IdleTurnThreshold)
                        {
                            CrossFadeV2(StateLeftTurn, 0.1f);
                            _isTurnPlaying = true;
                            _turnDuration = Turn90Duration;
                            _turnTimer = _turnDuration;
                            
                            // FIX STUTTER: Gunakan _previousYaw dan reset rotasi yang bocor dari controller
                            _turnStartYaw = _previousYaw;
                            transform.rotation = Quaternion.Euler(0f, _previousYaw, 0f);
                            
                            _turnTargetYaw = targetRotation;
                            SetMovementLock(true);
                            Debug.Log($"[AnimV2] Left Turn! Angle={angleDiff:F1}");
                        }
                        else if (angleDiff > IdleTurnThreshold)
                        {
                            CrossFadeV2(StateRightTurn, 0.1f);
                            _isTurnPlaying = true;
                            _turnDuration = Turn90Duration;
                            _turnTimer = _turnDuration;
                            
                            // FIX STUTTER: Gunakan _previousYaw dan reset rotasi yang bocor dari controller
                            _turnStartYaw = _previousYaw;
                            transform.rotation = Quaternion.Euler(0f, _previousYaw, 0f);
                            
                            _turnTargetYaw = targetRotation;
                            SetMovementLock(true);
                            Debug.Log($"[AnimV2] Right Turn! Angle={angleDiff:F1}");
                        }
                    }
                }
            }

            // --- 5. RUN TO STOP ---
            if (_stopCooldownTimer <= 0f && !_isInRunToStop && !_isInV2Animation)
            {
                bool isNowStopping = !isMoving && currentSpeed < 1.0f;
                
                if (_wasRunningRecently && isNowStopping)
                {
                    CrossFadeV2(StateRunToStop, 0.15f);
                    Debug.Log($"[AnimV2] RunToStop triggered! Speed={currentSpeed:F2}");
                    _isInRunToStop = true;
                    SetMovementLock(true); // Lock movement agar tidak slide
                    _wasRunningRecently = false;
                    _wasRunningTimer = 0f;
                    _stopCooldownTimer = StopCooldown;
                }
            }
            else
            {
                _stopCooldownTimer -= Time.deltaTime;
            }

            // Simpan data untuk frame berikutnya
            _previousYaw = currentYaw;
        }
    }
}
