using UnityEngine;

#if ENABLE_INPUT_SYSTEM 

using UnityEngine.InputSystem;

#endif



/* Note: animations are called via the controller for both the character and capsule using animator null checks

 */



namespace StarterAssets

{

    [RequireComponent(typeof(CharacterController))]

#if ENABLE_INPUT_SYSTEM 

    [RequireComponent(typeof(PlayerInput))]

#endif

    public class ThirdPersonController : MonoBehaviour

    {

        [Header("Player")]

        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;
        public bool HasControl => _hasControl;


        [Tooltip("Sprint speed of the character in m/s")]

        public float SprintSpeed = 5.335f;



        [Tooltip("How fast the character turns to face movement direction")]

        [Range(0.0f, 0.3f)]

        public float RotationSmoothTime = 0.12f;



        [Tooltip("Acceleration and deceleration")]

        public float SpeedChangeRate = 10.0f;



        public AudioClip LandingAudioClip;

        public AudioClip JumpAudioClip;

        public AudioClip[] FootstepAudioClips;

        [Tooltip("Suara langkah kaki khusus saat menginjak rumput (Unity Terrain Paint Details atau objek ber-Tag 'Grass')")]
        public AudioClip[] GrassFootstepAudioClips;

        [Tooltip("Opsional: Suara khusus saat lari/sprint di rumput. Jika kosong, otomatis memakai Grass Footstep Audio Clips.")]
        public AudioClip[] GrassRunAudioClips;

        [Tooltip("Opsional: Suara langkah khusus saat berada di area Hutan (buat BoxCollider IsTrigger bernama 'Hutan' atau 'Forest').")]
        public AudioClip[] ForestFootstepAudioClips;

        [Tooltip("Opsional: Suara langkah di lantai rumah/kayu (objek bernama mengandung 'Wood' / 'Rumah' / 'Lantai').")]
        public AudioClip[] HouseFootstepAudioClips;

        [Tooltip("Opsional: Suara langkah di air dangkal (objek di Layer 'Water' atau bernama mengandung 'Water' / 'Air').")]
        public AudioClip[] WaterFootstepAudioClips;

        [Header("Footstep Trim & Feel")]
        [Tooltip("Detik mulai pemutaran audio (untuk melewati bagian hening di awal file audio panjang).")]
        public float FootstepStartTime = 0f;

        [Tooltip("Durasi maksimal bunyi per 1 langkah kaki dalam detik untuk file audio panjang (misal 0.35). Isi 0 untuk putar penuh.")]
        public float FootstepMaxDuration = 0.35f;

        [Tooltip("Untuk file audio panjang (> 1.2 detik), acak titik potong mulainya agar setiap langkah bervariasi.")]
        public bool RandomizeStartForLongClips = true;

        [Tooltip("Sedikit mengacak pitch tiap langkah agar suara langkah terdengar alami.")]
        public bool RandomizeFootstepPitch = true;

        [Range(0f, 1f)]
        [Tooltip("Volume suara semak/rumput yang ditumpuk di atas suara langkah biasa saat menginjak rumput.")]
        public float GrassOverlayVolume = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Pengali volume footstep biasa saat berada di atas rumput (0.45 = 45% agar tidak bertabrakan dengan suara rumput).")]
        public float FootstepVolumeOnGrassMultiplier = 0.45f;

        [Range(0.1f, 4f)]
        [Tooltip("Pengali volume langkah kaki saat di dalam rumah (1.0 = normal, 2.5 = lebih kencang).")]
        public float HouseFootstepVolumeMultiplier = 2.5f;

        [Tooltip("Jeda langkah otomatis saat jalan (detik) jika animasi tidak punya Animation Event OnFootstep.")]
        public float WalkStepInterval = 0.48f;

        [Tooltip("Jeda langkah otomatis saat lari (detik) jika animasi lari tidak punya Animation Event OnFootstep.")]
        public float RunStepInterval = 0.24f;

        [Range(1f, 3f)]
        [Tooltip("Kecepatan/pitch audio rumput saat berlari penuh (1.0 = normal, 1.95 = hampir 2x lebih cepat).")]
        public float GrassRunPitchMultiplier = 1.95f;

        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Bush Rustle Audio (Terrain Tree)")]
        [Tooltip("Slot AudioClip suara gemerisik saat melewati semak/bush (Tree Terrain). Tinggal drag-and-drop file SFX bush di sini.")]
        public AudioClip[] BushRustleAudioClips;

        [Range(0f, 1f)]
        [Tooltip("Volume suara gemerisik saat bergerak melewati semak/bush.")]
        public float BushAudioVolume = 0.6f;

        [Range(0.5f, 4f)]
        [Tooltip("Radius deteksi semak/bush di sekitar karakter (meter).")]
        public float BushDetectionRadius = 1.4f;

        [Tooltip("Kata kunci nama prefab Tree Terrain yang dianggap sebagai Bush (pisahkan dengan koma).")]
        public string BushTreeKeywords = "bush,semak,shrub";



        [Space(10)]

        [Tooltip("The height the player can jump")]

        public float JumpHeight = 1.2f;



        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]

        public float Gravity = -15.0f;



        [Space(10)]

        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]

        public float JumpTimeout = 0.50f;



        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]

        public float FallTimeout = 0.15f;



        [Header("Player Grounded")]

        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]

        public bool Grounded = true;



        [Tooltip("Useful for rough ground")]

        public float GroundedOffset = -0.14f;



        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]

        public float GroundedRadius = 0.28f;



        [Tooltip("What layers the character uses as ground")]

        public LayerMask GroundLayers;



        [Header("Cinemachine")]

        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]

        public GameObject CinemachineCameraTarget;



        [Tooltip("How far in degrees can you move the camera up")]

        public float TopClamp = 70.0f;



        [Tooltip("How far in degrees can you move the camera down")]

        public float BottomClamp = -30.0f;



        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]

        public float CameraAngleOverride = 0.0f;



        [Tooltip("For locking the camera position on all axis")]

        public bool LockCameraPosition = false;



        // cinemachine

        private float _cinemachineTargetYaw;

        private float _cinemachineTargetPitch;



        // player

        private float _speed;

        private float _animationBlend;

        private float _targetRotation = 0.0f;

        private float _rotationVelocity;

        public void ResetRotationVelocity()
        {
            _rotationVelocity = 0f;
        }

        private float _currentTurnValue;

        private float _verticalVelocity;

        private float _terminalVelocity = 53.0f;



        // timeout deltatime

        private float _jumpTimeoutDelta;

        private float _fallTimeoutDelta;



        // animation IDs

        private int _animIDSpeed;

        private int _animIDGrounded;

        private int _animIDJump;

        private int _animIDFreeFall;

        private int _animIDMotionSpeed;

        private int _animIDTurn;



        public bool DisableMovement;

        public bool IsInFinisher;



#if ENABLE_INPUT_SYSTEM 

        private PlayerInput _playerInput;

#endif

        private Animator _animator;

        private CharacterController _controller;

        private StarterAssetsInputs _input;

        private GameObject _mainCamera;
        private PlayerControl _playerControl;
        private AudioSource _footstepAudioSource;
        private AudioSource _grassLoopSource;
        private AudioSource _bushLoopSource;
        private float _lastGrassCheckTime = -1f;
        private float _lastBushCheckTime = -1f;
        private bool _isCurrentlyOnGrass = false;
        private bool _isCurrentlyInBush = false;
        private Terrain _cachedBushTerrain;
        private readonly System.Collections.Generic.List<Vector3> _cachedBushPositions =
            new System.Collections.Generic.List<Vector3>();
        private readonly AudioSource[] _footstepVoices = new AudioSource[2];
        private readonly Coroutine[] _footstepVoiceRoutines = new Coroutine[2];
        private int _nextFootstepVoice = 0;
        private float _lastFootstepTime = -10f;
        private float _lastAnimEventFootstepTime = -10f;
        private bool _isSprintExhausted = false;
        private Transform _leftFootBone;
        private Transform _rightFootBone;
        private bool _footBonesInitialized;
        private float _prevLeftFootH;
        private float _prevRightFootH;
        private bool _leftFootMovingDown;
        private bool _rightFootMovingDown;
        private readonly System.Collections.Generic.Dictionary<AudioClip, System.Collections.Generic.List<float>> _cachedClipPeaks =
            new System.Collections.Generic.Dictionary<AudioClip, System.Collections.Generic.List<float>>();



        private const float _threshold = 0.01f;



        private bool _hasAnimator;

        // ===== NO JUMP ZONE =====
        private bool _isOnNoJumpZone = false;
        private bool _wasInHouseZone = false;
        private int _noJumpLayerIndex;

        public bool IsInHouse => _isOnNoJumpZone || HouseAudioZone.IsPlayerInside;

        // ===== VARIABEL UNTUK PARKOUR SYSTEM =====

        private bool _hasControl = true;
        [Tooltip("Jumlah stamina yang berkurang per detik saat berlari")]
        public float SprintStaminaCost = 15f;

        [Tooltip("Stamina minimal agar bisa lari kembali setelah stamina habis (mencegah lari putus-putus saat stamina 0).")]
        public float MinStaminaToResumeSprint = 15f;

        // ===== SISTEM ADVANCED LOCOMOTION & TERRAIN SLOPES =====
        private bool _wasGroundedLastFrame = true;
        private Vector3 _currentSlopeNormal = Vector3.up;

        public float GetSpeed() => _speed;
        public Vector3 GetSlopeNormal() => _currentSlopeNormal;


        private bool IsCurrentDeviceMouse

        {

            get

            {

#if ENABLE_INPUT_SYSTEM

                return _playerInput.currentControlScheme == "KeyboardMouse";

#else

                return false;

#endif

            }

        }





        private void Awake()

        {

            // get a reference to our main camera

            if (_mainCamera == null)

            {

                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            }

        }



        private void Start()

        {

            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;



            _hasAnimator = TryGetComponent(out _animator);

            _controller = GetComponent<CharacterController>();

            _input = GetComponent<StarterAssetsInputs>();
            
            _playerControl = GetComponent<PlayerControl>();

#if ENABLE_INPUT_SYSTEM 

            _playerInput = GetComponent<PlayerInput>();

#else

            Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");

#endif



            AssignAnimationIDs();

            _noJumpLayerIndex = LayerMask.NameToLayer("NoJump");

            // reset our timeouts on start

            _jumpTimeoutDelta = JumpTimeout;

            _fallTimeoutDelta = FallTimeout;

        }



        private void Update()

        {
            if (Time.timeScale == 0f || (Wirabaya.UI.SettingsMenuController.Instance != null && Wirabaya.UI.SettingsMenuController.Instance.IsMenuOpen))
            {
                if (_grassLoopSource != null && _grassLoopSource.isPlaying) _grassLoopSource.Pause();
                return;
            }

            _hasAnimator = TryGetComponent(out _animator);



            // ===== PENGECEKAN PARKOUR CONTROL =====

            // Jika tidak ada kontrol (sedang animasi parkour), hentikan fungsi gerak bawaan

            if (!_hasControl) return;



            if (IsInFinisher)

            {

                return;

            }



            JumpAndGravity();

            GroundedCheck();

            Move();

            HandleFootstepFallback();

            UpdateContinuousGrassAudio();

            UpdateContinuousBushAudio();

            bool isInHouseNow = IsInHouse;
            if (isInHouseNow != _wasInHouseZone)
            {
                _wasInHouseZone = isInHouseNow;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetIndoorState(isInHouseNow);
                }
            }

        }



        private void LateUpdate()

        {
            if (Time.timeScale == 0f || (Wirabaya.UI.SettingsMenuController.Instance != null && Wirabaya.UI.SettingsMenuController.Instance.IsMenuOpen)) return;

            if (IsInFinisher)

            {

                return;

            }



            CameraRotation();

            HandleFootBoneTouchdown();

        }



        private void AssignAnimationIDs()

        {

            _animIDSpeed = Animator.StringToHash("Speed");

            _animIDGrounded = Animator.StringToHash("Grounded");

            _animIDJump = Animator.StringToHash("Jump");

            _animIDFreeFall = Animator.StringToHash("FreeFall");

            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            _animIDTurn = Animator.StringToHash("Turn");

        }



        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // --- DOWNHILL STICKING (Anti-Bouncing saat berlari menuruni lereng bukit) ---
            // Jika CheckSphere false tetapi karakter baru saja berada di tanah, tidak sedang melompat, dan ada lereng tepat di bawah kaki:
            if (!Grounded && _wasGroundedLastFrame && _verticalVelocity <= 0.0f && _jumpTimeoutDelta <= 0.0f)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
                int layerMask = GroundLayers.value != 0 ? (int)GroundLayers : ~LayerMask.GetMask("Player", "Ignore Raycast", "TransparentFX");

                float checkDist = _controller.stepOffset + 0.2f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit downHit, checkDist + 0.2f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    float slopeAngle = Vector3.Angle(Vector3.up, downHit.normal);
                    if (slopeAngle <= _controller.slopeLimit)
                    {
                        // Karakter tetap dipertahankan grounded
                        Grounded = true;
                        _currentSlopeNormal = downHit.normal;
                    }
                }
            }

            _wasGroundedLastFrame = Grounded;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }



        private void CameraRotation()
        {
            // --- Cek apakah sedang Hard-Locked ke musuh ---
            bool isLocked = TargetDetectionControl.instance != null && TargetDetectionControl.instance.isHardLocked && _playerControl != null && _playerControl.target != null;

            if (isLocked)
            {
                // Hitung arah dari kamera ke target
                // Offset agar menatap dada/kepala musuh
                Vector3 targetPos = _playerControl.target.position;
                targetPos.y += 1.2f; 
                
                // Gunakan posisi target kamera di leher player sebagai acuan, bukan kaki (transform.position)
                // dan bukan kamera (_mainCamera) agar tidak feedback loop.
                Vector3 dirToTarget = targetPos - CinemachineCameraTarget.transform.position;
                
                if (dirToTarget != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                    
                    // Lerp Yaw dan Pitch agar pergerakan kamera mulus
                    _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, lookRot.eulerAngles.y, Time.deltaTime * 10f);
                    
                    // Normalisasi sudut pitch agar tidak terbalik saat di atas 180 derajat
                    float rawPitch = lookRot.eulerAngles.x;
                    if (rawPitch > 180f) rawPitch -= 360f;
                    
                    // Batasi Pitch agar tidak terlalu nunduk/ndangak ekstrim
                    float targetPitch = ClampAngle(rawPitch, BottomClamp, TopClamp);
                    _cinemachineTargetPitch = Mathf.LerpAngle(_cinemachineTargetPitch, targetPitch, Time.deltaTime * 10f);
                }
            }
            else
            {
                // if there is an input and camera position is not fixed
                if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    //Don't multiply mouse input by Time.deltaTime;
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                    float sensitivity = Wirabaya.UI.SettingsMenuController.MouseSensitivity;
                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * sensitivity;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * sensitivity;
                }
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }



        private void Move()
        {
            if (DisableMovement)
            {
                // Reset internal speed agar tidak drift/slide sesudah animasi serang selesai
                _speed = 0f;
                _animationBlend = 0f;
                _currentTurnValue = 0f;

                // Saat combat: tetap terapkan gravitasi vertikal agar player tidak melayang
                // setelah traversal selesai. Hanya gerakan horizontal yang dimatikan.
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                return;
            }

            // --- LOGIKA PENGURANGAN STAMINA LARI ---
            bool isMoving = _input.move != Vector2.zero;
            bool isHoldingSword = _playerControl != null && _playerControl.hasSwordEquipped && !_playerControl.isSwordSheathed;
            bool hasPlayerStatus = PlayerStatus.Instance != null;

            if (hasPlayerStatus)
            {
                if (PlayerStatus.Instance.stamina <= 0f)
                {
                    _isSprintExhausted = true;
                }
                else if (_isSprintExhausted && PlayerStatus.Instance.stamina >= MinStaminaToResumeSprint)
                {
                    _isSprintExhausted = false;
                }
            }
            else
            {
                _isSprintExhausted = false;
            }

            bool isInHouse = IsInHouse;
            bool canSprint = !isInHouse && (!hasPlayerStatus || (!_isSprintExhausted && PlayerStatus.Instance.stamina > 0f));

            if (!canSprint)
            {
                _input.sprint = false; // Kunci agar benar-benar tidak bisa lari saat di dalam rumah atau stamina habis
            }

            if (_input.sprint && canSprint && isMoving)
            {
                if (hasPlayerStatus)
                {
                    // Kurangi stamina secara konstan per detik baik saat memegang pedang maupun tidak
                    PlayerStatus.Instance.UseStamina(SprintStaminaCost * Time.deltaTime);
                    if (PlayerStatus.Instance.stamina <= 0f)
                    {
                        _isSprintExhausted = true;
                        _input.sprint = false;
                    }
                }
            }

            // Set target speed berdasarkan ketersediaan stamina
            float targetSpeed = (_input.sprint && canSprint) ? SprintSpeed : MoveSpeed;
            // ---------------------------------------

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // ... (Sisanya biarkan sama persis seperti aslinya mulai dari baris di bawah ini)
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;




            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // Percepatan dan perlambatan langsung ke targetSpeed secara mandiri (tidak mengambil dari _controller.velocity)
            // Solusi tuntas masalah "nggeret" / lari di tempat saat menyenggol dinding, sudut lantai kayu, atau rintangan.
            if (_speed < targetSpeed - speedOffset || _speed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }



            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            if (_animationBlend < 0.01f) _animationBlend = 0f;



            // normalise input direction

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;



            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude

            // if there is a move input rotate player when the player is moving

            if (_input.move != Vector2.zero)

            {

                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +

                                  _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,

                    RotationSmoothTime);



                // rotate to face input direction relative to camera position

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            }



            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            Vector3 moveDirection = targetDirection.normalized;

            // --- SISTEM ADAPTASI LERENG GUNUNG / TERRAIN (Slope Projection & Anti-Stuck) ---
            if (Grounded && _speed > 0.01f && _verticalVelocity <= 0.0f)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;
                int layerMask = GroundLayers.value != 0 ? (int)GroundLayers : ~LayerMask.GetMask("Player", "Ignore Raycast", "TransparentFX");

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit slopeHit, 0.9f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    _currentSlopeNormal = slopeHit.normal;
                    float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                    // Hanya proyeksikan jika berada di lereng bukit curam nyata (>= 8 derajat) dan sedang mendaki ke atas (projected.y > 0)
                    // Mencegah vektor gerak terdorong menancap ke lantai kayu, undakan datar, atau dinding
                    if (slopeAngle >= 8.0f && slopeAngle <= _controller.slopeLimit)
                    {
                        Vector3 projected = Vector3.ProjectOnPlane(targetDirection, slopeHit.normal);
                        if (projected.y > 0.0f)
                        {
                            moveDirection = projected.normalized;
                        }
                    }
                }
                else
                {
                    _currentSlopeNormal = Vector3.up;
                }
            }
            else if (Grounded)
            {
                // Tetap simpan normal tanah saat berdiri diam
                Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;
                int layerMask = GroundLayers.value != 0 ? (int)GroundLayers : ~LayerMask.GetMask("Player", "Ignore Raycast", "TransparentFX");
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit idleHit, 0.9f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    _currentSlopeNormal = idleHit.normal;
                }
                else
                {
                    _currentSlopeNormal = Vector3.up;
                }
            }
            else
            {
                _currentSlopeNormal = Vector3.up;
            }

            // move the player
            _controller.Move(moveDirection * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);



            // update animator if using character

            if (_hasAnimator)

            {

                _animator.SetFloat(_animIDSpeed, _animationBlend);

                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                // Jika sedang pegang senjata, matikan semua nilai Turn ke Animator agar tidak memicu animasi belok.
                float targetTurn = 0f;
                if (!isHoldingSword)
                {
                    // Perbaikan Bug: Animasi Turn (Left/Right) HANYA aktif saat berdiri diam (_input.move == Vector2.zero & _speed < 0.1f).
                    // Saat berjalan/lari dengan WASD, Turn dipaksa ke 0 agar animasi Walk_N / Run_N berjalan mulus tanpa looping turn!
                    targetTurn = (_input.move == Vector2.zero && _speed < 0.1f) ? Mathf.Clamp(_rotationVelocity / 180f, -1f, 1f) : 0f;
                }
                
                _currentTurnValue = Mathf.Lerp(_currentTurnValue, targetTurn, Time.deltaTime * 10f);

                _animator.SetFloat(_animIDTurn, _currentTurnValue);

            }

        }



        private void JumpAndGravity()

        {

            if (Grounded)

            {

                // reset the fall timeout timer

                _fallTimeoutDelta = FallTimeout;



                // update animator if using character

                if (_hasAnimator)

                {

                    _animator.SetBool(_animIDJump, false);

                    _animator.SetBool(_animIDFreeFall, false);

                }



                // Menekan karakter dengan lembut (-5f) agar tetap menempel alami saat menuruni lereng tanpa terpental
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -5.0f;
                }



                // Jump (diblokir jika sedang di zona NoJump)

                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isOnNoJumpZone)

                {

                    // the square root of H * -2 * G = how much velocity needed to reach desired height

                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);



                    // update animator if using character

                    if (_hasAnimator)

                    {

                        _animator.SetBool(_animIDJump, true);

                    }

                    if (JumpAudioClip != null)

                    {

                        AudioSource.PlayClipAtPoint(JumpAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);

                    }

                }



                // jump timeout

                if (_jumpTimeoutDelta >= 0.0f)

                {

                    _jumpTimeoutDelta -= Time.deltaTime;

                }

            }

            else

            {

                // reset the jump timeout timer

                _jumpTimeoutDelta = JumpTimeout;



                // fall timeout

                if (_fallTimeoutDelta >= 0.0f)

                {

                    _fallTimeoutDelta -= Time.deltaTime;

                }

                else

                {

                    // update animator if using character

                    if (_hasAnimator)

                    {

                        _animator.SetBool(_animIDFreeFall, true);

                    }

                }



                // if we are not grounded, do not jump

                _input.jump = false;

            }



            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)

            if (_verticalVelocity < _terminalVelocity)

            {

                _verticalVelocity += Gravity * Time.deltaTime;

            }

        }





        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)

        {

            if (lfAngle < -360f) lfAngle += 360f;

            if (lfAngle > 360f) lfAngle -= 360f;

            return Mathf.Clamp(lfAngle, lfMin, lfMax);

        }



        private void OnDrawGizmosSelected()

        {

            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);

            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);



            if (Grounded) Gizmos.color = transparentGreen;

            else Gizmos.color = transparentRed;



            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider

            Gizmos.DrawSphere(

                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),

                GroundedRadius);

        }



        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.25f)
            {
                bool isSprinting = _input != null && _input.sprint;
                float minCooldown = isSprinting ? 0.20f : 0.44f;
                if (Time.time - _lastFootstepTime < minCooldown) return;

                _lastAnimEventFootstepTime = Time.time;
                TriggerFootstep();
            }
        }

        private void HandleFootBoneTouchdown()
        {
            if (!Grounded || DisableMovement || _input == null || _input.move == Vector2.zero || _speed <= 0.4f)
            {
                _leftFootMovingDown = false;
                _rightFootMovingDown = false;
                return;
            }

            if (!_footBonesInitialized && _hasAnimator && _animator != null && _animator.isHuman)
            {
                _leftFootBone = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                _rightFootBone = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
                _footBonesInitialized = true;
                if (_leftFootBone != null) _prevLeftFootH = _leftFootBone.position.y - transform.position.y;
                if (_rightFootBone != null) _prevRightFootH = _rightFootBone.position.y - transform.position.y;
            }

            if (_leftFootBone == null || _rightFootBone == null) return;

            bool isSprinting = _input.sprint;
            // Jika Animation Event OnFootstep aktif dipakai saat jalan, biarkan animasi yang mengatur tempo jalan agar tidak ganda
            if (!isSprinting && Time.time - _lastAnimEventFootstepTime < 0.7f)
            {
                _prevLeftFootH = _leftFootBone.position.y - transform.position.y;
                _prevRightFootH = _rightFootBone.position.y - transform.position.y;
                return;
            }

            float minCooldown = isSprinting ? 0.21f : 0.46f;

            float leftH = _leftFootBone.position.y - transform.position.y;
            float leftDelta = leftH - _prevLeftFootH;
            _prevLeftFootH = leftH;

            if (leftDelta < -0.005f)
            {
                _leftFootMovingDown = true;
            }
            else if (_leftFootMovingDown && leftDelta >= 0f && leftH < 0.32f)
            {
                _leftFootMovingDown = false;
                if (Time.time - _lastFootstepTime >= minCooldown)
                {
                    TriggerFootstep();
                }
            }

            float rightH = _rightFootBone.position.y - transform.position.y;
            float rightDelta = rightH - _prevRightFootH;
            _prevRightFootH = rightH;

            if (rightDelta < -0.005f)
            {
                _rightFootMovingDown = true;
            }
            else if (_rightFootMovingDown && rightDelta >= 0f && rightH < 0.32f)
            {
                _rightFootMovingDown = false;
                if (Time.time - _lastFootstepTime >= minCooldown)
                {
                    TriggerFootstep();
                }
            }
        }

        private void HandleFootstepFallback()
        {
            if (!Grounded || DisableMovement || _input == null || _input.move == Vector2.zero || _speed <= 0.4f)
            {
                return;
            }

            bool hasFootBones = _leftFootBone != null && _rightFootBone != null;
            bool isSprinting = _input.sprint;
            float interval = isSprinting ? RunStepInterval : WalkStepInterval;
            float tolerance = hasFootBones ? 0.12f : 0f;

            if (Time.time - _lastFootstepTime >= interval + tolerance)
            {
                TriggerFootstep();
            }
        }

        private void TriggerFootstep()
        {
            _lastFootstepTime = Time.time;
            AudioClip[] baseClips = SelectBaseFootstepClips(out bool isOnGrass);

            // Jika di atas rumput dan audio rumput tersedia, biarkan suara rumput berjalan sendiri tanpa ditimpa footstep biasa
            if (isOnGrass)
            {
                AudioClip[] grassClips = GetGrassClips();
                if (grassClips != null && grassClips.Length > 0)
                {
                    return;
                }
            }

            if (baseClips != null && baseClips.Length > 0)
            {
                var index = Random.Range(0, baseClips.Length);
                if (baseClips[index] != null)
                {
                    float stepVolume = FootstepAudioVolume;
                    if (baseClips == HouseFootstepAudioClips)
                    {
                        stepVolume *= HouseFootstepVolumeMultiplier;
                    }
                    PlayFootstepClip(baseClips[index], stepVolume);
                }
            }
        }

        private void UpdateContinuousGrassAudio()
        {
            bool isMovingOnGround = Grounded && !DisableMovement && _input != null && _input.move != Vector2.zero && _speed > 0.4f;

            if (isMovingOnGround)
            {
                if (Time.time - _lastGrassCheckTime >= 0.08f)
                {
                    _lastGrassCheckTime = Time.time;
                    SelectBaseFootstepClips(out _isCurrentlyOnGrass);
                }
            }
            else
            {
                _isCurrentlyOnGrass = false;
            }

            AudioClip[] grassClips = GetGrassClips();
            bool shouldPlay = isMovingOnGround && _isCurrentlyOnGrass && grassClips != null && grassClips.Length > 0;
            float targetVol = shouldPlay ? FootstepAudioVolume : 0f;

            if (shouldPlay)
            {
                AudioSource src = GetOrCreateGrassLoopSource();
                float speedRatio = Mathf.InverseLerp(MoveSpeed, Mathf.Max(MoveSpeed + 0.1f, SprintSpeed), _speed);
                float targetPitch = Mathf.Lerp(1.0f, GrassRunPitchMultiplier, speedRatio);

                AudioClip desiredClip = grassClips[0];
                if (desiredClip != null && src.clip != desiredClip)
                {
                    src.Stop();
                    src.clip = desiredClip;
                    src.pitch = targetPitch;
                    src.Play();
                }
                else if (!src.isPlaying && src.clip != null)
                {
                    src.pitch = targetPitch;
                    src.UnPause();
                    if (!src.isPlaying) src.Play();
                }

                src.pitch = Mathf.Lerp(src.pitch, targetPitch, Time.deltaTime * 15f);
                src.volume = Mathf.MoveTowards(src.volume, targetVol, Time.deltaTime * 6f);
            }
            else if (_grassLoopSource != null && _grassLoopSource.isPlaying)
            {
                _grassLoopSource.volume = Mathf.MoveTowards(_grassLoopSource.volume, 0f, Time.deltaTime * 6f);
                if (_grassLoopSource.volume <= 0.005f)
                {
                    _grassLoopSource.volume = 0f;
                    _grassLoopSource.Pause();
                }
            }
        }

        private AudioSource GetOrCreateGrassLoopSource()
        {
            if (_grassLoopSource != null) return _grassLoopSource;

            Transform child = transform.Find("GrassLoopAudio");
            if (child == null)
            {
                GameObject go = new GameObject("GrassLoopAudio");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            _grassLoopSource = child.GetComponent<AudioSource>();
            if (_grassLoopSource == null)
            {
                _grassLoopSource = child.gameObject.AddComponent<AudioSource>();
            }

            _grassLoopSource.playOnAwake = false;
            _grassLoopSource.loop = true;
            _grassLoopSource.volume = 0f;
            _grassLoopSource.spatialBlend = 0.5f;
            return _grassLoopSource;
        }

        private void UpdateContinuousBushAudio()
        {
            bool hasBushClips = BushRustleAudioClips != null && BushRustleAudioClips.Length > 0 && BushRustleAudioClips[0] != null;
            bool isMoving = Grounded && !DisableMovement && !IsInHouse && _input != null && _input.move != Vector2.zero && _speed > 0.4f;

            if (isMoving && hasBushClips)
            {
                if (Time.time - _lastBushCheckTime >= 0.08f)
                {
                    _lastBushCheckTime = Time.time;
                    _isCurrentlyInBush = IsMovingInsideTerrainBush(transform.position);
                }
            }
            else
            {
                _isCurrentlyInBush = false;
            }

            bool shouldPlay = isMoving && _isCurrentlyInBush && hasBushClips;
            float targetVol = shouldPlay ? BushAudioVolume : 0f;

            if (shouldPlay)
            {
                AudioSource src = GetOrCreateBushLoopSource();
                float speedRatio = Mathf.InverseLerp(MoveSpeed, Mathf.Max(MoveSpeed + 0.1f, SprintSpeed), _speed);
                float targetPitch = Mathf.Lerp(1.0f, 1.45f, speedRatio);

                AudioClip desiredClip = BushRustleAudioClips[0];
                if (src.clip != desiredClip)
                {
                    src.Stop();
                    src.clip = desiredClip;
                    src.pitch = targetPitch;
                    src.Play();
                }
                else if (!src.isPlaying && src.clip != null)
                {
                    src.pitch = targetPitch;
                    src.UnPause();
                    if (!src.isPlaying) src.Play();
                }

                src.pitch = Mathf.Lerp(src.pitch, targetPitch, Time.deltaTime * 12f);
                src.volume = Mathf.MoveTowards(src.volume, targetVol, Time.deltaTime * 6f);
            }
            else if (_bushLoopSource != null && _bushLoopSource.isPlaying)
            {
                _bushLoopSource.volume = Mathf.MoveTowards(_bushLoopSource.volume, 0f, Time.deltaTime * 6f);
                if (_bushLoopSource.volume <= 0.005f)
                {
                    _bushLoopSource.volume = 0f;
                    _bushLoopSource.Pause();
                }
            }
        }

        private AudioSource GetOrCreateBushLoopSource()
        {
            if (_bushLoopSource != null) return _bushLoopSource;

            Transform child = transform.Find("BushLoopAudio");
            if (child == null)
            {
                GameObject go = new GameObject("BushLoopAudio");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            _bushLoopSource = child.GetComponent<AudioSource>();
            if (_bushLoopSource == null)
            {
                _bushLoopSource = child.gameObject.AddComponent<AudioSource>();
            }

            _bushLoopSource.playOnAwake = false;
            _bushLoopSource.loop = true;
            _bushLoopSource.volume = 0f;
            _bushLoopSource.spatialBlend = 0.5f;
            return _bushLoopSource;
        }

        private bool IsMovingInsideTerrainBush(Vector3 worldPos)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null) return false;

            if (_cachedBushTerrain != terrain)
            {
                RebuildTerrainBushCache(terrain);
            }

            if (_cachedBushPositions.Count == 0) return false;

            float radiusSq = BushDetectionRadius * BushDetectionRadius;
            for (int i = 0; i < _cachedBushPositions.Count; i++)
            {
                Vector3 bp = _cachedBushPositions[i];
                float dx = worldPos.x - bp.x;
                float dz = worldPos.z - bp.z;
                if ((dx * dx + dz * dz) <= radiusSq && Mathf.Abs(worldPos.y - bp.y) <= 2.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildTerrainBushCache(Terrain terrain)
        {
            _cachedBushTerrain = terrain;
            _cachedBushPositions.Clear();

            TerrainData data = terrain.terrainData;
            if (data == null || data.treePrototypes == null || data.treeInstances == null) return;

            string[] keywords = string.IsNullOrEmpty(BushTreeKeywords)
                ? new[] { "bush", "semak", "shrub" }
                : BushTreeKeywords.Split(',');

            var bushProtoIndices = new System.Collections.Generic.HashSet<int>();
            TreePrototype[] prototypes = data.treePrototypes;
            for (int i = 0; i < prototypes.Length; i++)
            {
                GameObject prefab = prototypes[i].prefab;
                if (prefab == null) continue;

                for (int k = 0; k < keywords.Length; k++)
                {
                    string kw = keywords[k].Trim();
                    if (!string.IsNullOrEmpty(kw) && prefab.name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bushProtoIndices.Add(i);
                        break;
                    }
                }
            }

            if (bushProtoIndices.Count == 0) return;

            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = data.size;
            TreeInstance[] instances = data.treeInstances;
            for (int i = 0; i < instances.Length; i++)
            {
                if (!bushProtoIndices.Contains(instances[i].prototypeIndex)) continue;

                Vector3 np = instances[i].position;
                Vector3 wp = new Vector3(
                    terrainPos.x + np.x * terrainSize.x,
                    terrainPos.y + np.y * terrainSize.y,
                    terrainPos.z + np.z * terrainSize.z
                );
                _cachedBushPositions.Add(wp);
            }
        }

        private AudioClip[] SelectBaseFootstepClips(out bool isOnGrass)
        {
            isOnGrass = false;

            if ((_isOnNoJumpZone || HouseAudioZone.IsPlayerInside) && HouseFootstepAudioClips != null && HouseFootstepAudioClips.Length > 0)
            {
                return HouseFootstepAudioClips;
            }

            bool inForestZone = false;
            int waterLayer = LayerMask.NameToLayer("Water");

            // Cek apakah karakter sedang berada di dalam Trigger zona (misal Zona Rumah / Hutan / Air)
            Collider[] overlaps = Physics.OverlapSphere(transform.position + Vector3.up * 0.25f, 0.4f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider col = overlaps[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;

                string objName = col.gameObject.name;
                if ((waterLayer != -1 && col.gameObject.layer == waterLayer) || ContainsAny(objName, "water", "air"))
                {
                    if (WaterFootstepAudioClips != null && WaterFootstepAudioClips.Length > 0)
                        return WaterFootstepAudioClips;
                }

                if (col.GetComponent<HouseAudioZone>() != null || ContainsAny(objName, "rumah", "house", "indoor", "home"))
                {
                    if (HouseFootstepAudioClips != null && HouseFootstepAudioClips.Length > 0)
                        return HouseFootstepAudioClips;
                }

                if (ContainsAny(objName, "hutan", "forest"))
                {
                    inForestZone = true;
                }
            }

            Vector3 rayOrigin = transform.position + Vector3.up * 0.35f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1.5f, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                string hitName = hit.collider.gameObject.name;

                if ((waterLayer != -1 && hit.collider.gameObject.layer == waterLayer) || ContainsAny(hitName, "water", "air"))
                {
                    if (WaterFootstepAudioClips != null && WaterFootstepAudioClips.Length > 0)
                        return WaterFootstepAudioClips;
                }

                if ((_noJumpLayerIndex != -1 && hit.collider.gameObject.layer == _noJumpLayerIndex) || IsHouseTransform(hit.collider.transform))
                {
                    if (HouseFootstepAudioClips != null && HouseFootstepAudioClips.Length > 0)
                        return HouseFootstepAudioClips;
                }

                if (ContainsAny(hitName, "grass", "rumput"))
                {
                    isOnGrass = true;
                }

                Terrain terrain = hit.collider.GetComponent<Terrain>();
                if (terrain != null && IsStandingOnTerrainGrass(terrain, hit.point))
                {
                    isOnGrass = true;
                }
            }

            if (inForestZone && ForestFootstepAudioClips != null && ForestFootstepAudioClips.Length > 0)
            {
                return ForestFootstepAudioClips;
            }

            return FootstepAudioClips;
        }

        private static bool IsHouseTransform(Transform tr)
        {
            Transform current = tr;
            int depth = 0;
            while (current != null && depth < 6)
            {
                string tag = current.gameObject.tag;
                if (string.Equals(tag, "House", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "Wood", System.StringComparison.OrdinalIgnoreCase) ||
                    ContainsAny(current.name, "wood", "house", "rumah", "lantai", "papan", "floor", "home", "joglo", "pendopo", "pondok", "indoor", "kayu"))
                {
                    return true;
                }
                current = current.parent;
                depth++;
            }
            return false;
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (text.IndexOf(keywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private AudioClip[] GetGrassClips()
        {
            if (_input != null && _input.sprint && !_isSprintExhausted && GrassRunAudioClips != null && GrassRunAudioClips.Length > 0)
            {
                return GrassRunAudioClips;
            }
            return GrassFootstepAudioClips;
        }

        private bool IsStandingOnTerrainGrass(Terrain terrain, Vector3 worldPos)
        {
            TerrainData data = terrain.terrainData;
            if (data == null || data.detailPrototypes == null || data.detailPrototypes.Length == 0)
            {
                return false;
            }

            Vector3 terrainPos = terrain.transform.position;
            float normX = (worldPos.x - terrainPos.x) / data.size.x;
            float normZ = (worldPos.z - terrainPos.z) / data.size.z;

            if (normX < 0f || normX > 1f || normZ < 0f || normZ > 1f)
            {
                return false;
            }

            int detailX = Mathf.Clamp(Mathf.FloorToInt(normX * data.detailWidth), 0, data.detailWidth - 1);
            int detailZ = Mathf.Clamp(Mathf.FloorToInt(normZ * data.detailHeight), 0, data.detailHeight - 1);

            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                int[,] detail = data.GetDetailLayer(detailX, detailZ, 1, 1, layer);
                if (detail[0, 0] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayFootstepClip(AudioClip clip, float targetVolume)
        {
            if (targetVolume <= 0.001f) return;

            bool isLongClip = clip.length > 1.2f;
            if (isLongClip && (FootstepMaxDuration > 0f || FootstepStartTime > 0f))
            {
                int voiceIdx = _nextFootstepVoice;
                _nextFootstepVoice = (_nextFootstepVoice + 1) % 2;

                AudioSource voice = GetOrCreateFootstepVoice(voiceIdx);
                if (_footstepVoiceRoutines[voiceIdx] != null)
                {
                    StopCoroutine(_footstepVoiceRoutines[voiceIdx]);
                }

                float minStart = Mathf.Clamp(FootstepStartTime, 0f, Mathf.Max(0f, clip.length - 0.15f));
                float playDuration = FootstepMaxDuration > 0f ? Mathf.Max(0.22f, FootstepMaxDuration) : 0.4f;
                float maxStart = Mathf.Max(minStart, clip.length - playDuration - 0.05f);

                float chosenTime = minStart;
                if (RandomizeStartForLongClips)
                {
                    var peaks = GetOrScanClipPeaks(clip);
                    if (peaks != null && peaks.Count > 0)
                    {
                        chosenTime = peaks[Random.Range(0, peaks.Count)];
                    }
                    else if (maxStart > minStart)
                    {
                        chosenTime = Random.Range(minStart, maxStart);
                    }
                }

                float startTime = Mathf.Clamp(chosenTime, 0f, Mathf.Max(0f, clip.length - 0.1f));
                float pitch = RandomizeFootstepPitch ? Random.Range(0.94f, 1.06f) : 1f;
                _footstepVoiceRoutines[voiceIdx] = StartCoroutine(PlayFootstepSliceRoutine(voice, clip, startTime, playDuration, pitch, targetVolume));
            }
            else
            {
                AudioSource source = GetOrCreateFootstepSource();
                source.pitch = RandomizeFootstepPitch ? Random.Range(0.92f, 1.08f) : 1f;
                source.PlayOneShot(clip, targetVolume);
            }
        }

        private System.Collections.IEnumerator PlayFootstepSliceRoutine(AudioSource voice, AudioClip clip, float startTime, float duration, float pitch, float targetVolume)
        {
            voice.Stop();
            voice.clip = clip;
            voice.pitch = pitch;
            voice.time = startTime;
            voice.volume = 0f;
            voice.Play();

            float fadeIn = Mathf.Min(0.03f, duration * 0.15f);
            float fadeOut = Mathf.Min(0.18f, duration * 0.45f);
            float sustain = Mathf.Max(0f, duration - fadeIn - fadeOut);

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                voice.volume = Mathf.Lerp(0f, targetVolume, t / fadeIn);
                yield return null;
            }
            voice.volume = targetVolume;

            if (sustain > 0f)
            {
                yield return new WaitForSeconds(sustain);
            }

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                voice.volume = Mathf.Lerp(targetVolume, 0f, t / fadeOut);
                yield return null;
            }

            voice.volume = 0f;
            voice.Stop();
        }

        private AudioSource GetOrCreateFootstepVoice(int index)
        {
            if (_footstepVoices[index] != null) return _footstepVoices[index];

            string childName = $"FootstepAudio_Voice{index}";
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            AudioSource src = child.GetComponent<AudioSource>();
            if (src == null)
            {
                src = child.gameObject.AddComponent<AudioSource>();
            }

            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0.5f;
            _footstepVoices[index] = src;
            return src;
        }

        private System.Collections.Generic.List<float> GetOrScanClipPeaks(AudioClip clip)
        {
            if (clip == null) return null;
            if (_cachedClipPeaks.TryGetValue(clip, out var cached)) return cached;

            var peaks = new System.Collections.Generic.List<float>();
            try
            {
                if (clip.loadType == AudioClipLoadType.DecompressOnLoad || clip.loadState == AudioDataLoadState.Loaded)
                {
                    int channels = clip.channels;
                    int freq = clip.frequency;
                    int totalSamples = clip.samples;
                    float[] data = new float[totalSamples * channels];

                    if (clip.GetData(data, 0))
                    {
                        int stepSamples = Mathf.Max(1, freq / 25); // window 40ms
                        float maxAmp = 0f;
                        for (int i = 0; i < data.Length; i += channels * 4)
                        {
                            float a = Mathf.Abs(data[i]);
                            if (a > maxAmp) maxAmp = a;
                        }

                        float threshold = maxAmp * 0.28f;
                        float lastPeakTime = -1f;
                        float maxAllowedTime = FootstepMaxDuration > 0f
                            ? Mathf.Max(0f, clip.length - FootstepMaxDuration - 0.05f)
                            : clip.length - 0.1f;

                        for (int s = 0; s < totalSamples; s += stepSamples)
                        {
                            float t = (float)s / freq;
                            if (t < FootstepStartTime || t > maxAllowedTime) continue;

                            float windowMax = 0f;
                            int end = Mathf.Min(totalSamples, s + stepSamples);
                            for (int k = s; k < end; k++)
                            {
                                float v = Mathf.Abs(data[k * channels]);
                                if (v > windowMax) windowMax = v;
                            }

                            if (windowMax >= threshold && (t - lastPeakTime) >= 0.35f)
                            {
                                peaks.Add(Mathf.Max(0f, t - 0.02f));
                                lastPeakTime = t;
                            }
                        }
                    }
                }
            }
            catch { }

            _cachedClipPeaks[clip] = peaks;
            return peaks;
        }

        private AudioSource GetOrCreateFootstepSource()
        {
            if (_footstepAudioSource != null) return _footstepAudioSource;

            Transform child = transform.Find("FootstepAudio");
            if (child == null)
            {
                GameObject go = new GameObject("FootstepAudio");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            _footstepAudioSource = child.GetComponent<AudioSource>();
            if (_footstepAudioSource == null)
            {
                _footstepAudioSource = child.gameObject.AddComponent<AudioSource>();
            }

            _footstepAudioSource.playOnAwake = false;
            _footstepAudioSource.loop = false;
            _footstepAudioSource.spatialBlend = 0.5f;
            return _footstepAudioSource;
        }



        private void OnLand(AnimationEvent animationEvent)

        {

            if (animationEvent.animatorClipInfo.weight > 0.5f)

            {

                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);

            }

        }

        private void OnControllerColliderHit(ControllerColliderHit hit)

        {

            // ===== NO JUMP ZONE DETECTION =====
            if (_noJumpLayerIndex != -1 && hit.normal.y > 0.3f)
            {
                _isOnNoJumpZone = (hit.gameObject.layer == _noJumpLayerIndex);
            }

            Rigidbody rb = hit.collider.attachedRigidbody;



            if (rb == null)

            {

                return;

            }



            if (rb.isKinematic)

            {

                return;

            }



            Vector3 pushDir = hit.moveDirection;

            pushDir.y = 0f;



            rb.AddForce(pushDir * 0.5f, ForceMode.Impulse);

        }



        // ===== FUNGSI UNTUK RESET VERTICAL VELOCITY (COMBAT) =====
        /// <summary>
        /// Dipanggil oleh PlayerControl setelah traversal combat selesai.
        /// Mereset kecepatan vertikal agar gravitasi langsung aktif kembali
        /// dan player tidak melayang/ngambang setelah menyerang di udara.
        /// </summary>
        public void ResetVerticalVelocity()
        {
            _verticalVelocity = -2f;
        }

        // ===== FUNGSI UNTUK MENGATUR KONTROL SAAT PARKOUR =====

        public void SetControl(bool hasControl)

        {

            _hasControl = hasControl;

            // KITA JANGAN MEMATIKAN CharacterController DI SINI!
            // Kita ingin CharacterController tetap menyala saat parkour agar Fisika Unity
            // mencegah karakter berguling menembus benda padat (seperti pohon).
            // Ukuran kapsul sudah di-handle oleh Tiny Capsule di ParkourController.



            if (_animator != null)

            {

                _animator.applyRootMotion = !hasControl;

            }



            if (!hasControl)

            {

                _animator.SetFloat(_animIDSpeed, 0f);

                _animator.SetFloat(_animIDMotionSpeed, 0f);



                _verticalVelocity = -2f;

            }

        }

        private void OnAnimatorMove()
        {
            // PENTING: Saat Parkour (hasControl == false), kita harus menerapkan Root Motion secara manual.
            // Kita TIDAK BISA menggunakan ApplyBuiltinRootMotion() karena animasi Mixamo sering memiliki BUG 
            // di frame terakhirnya (akar/root kembali ke 0,0,0) yang membuat deltaPosition menjadi raksasa.
            // Hal inilah yang membuat karaktermu "kepental" dengan sangat kencang ke belakang!
            if (!_hasControl && _animator != null)
            {
                // ANTI-GLITCH: Jika perpindahan lebih dari 0.3 meter dalam 1 frame (tidak masuk akal secara fisik), abaikan!
                if (_animator.deltaPosition.magnitude < 0.3f)
                {
                    // Gunakan _controller.Move() alih-alih transform.position.
                    // Ini memungkinkan kapsul (yang sedang mode Tiny) untuk menabrak rintangan/pohon secara alami.
                    if (_controller != null && _controller.enabled)
                    {
                        _controller.Move(_animator.deltaPosition);
                    }
                    else
                    {
                        transform.position += _animator.deltaPosition;
                    }
                }
                transform.rotation *= _animator.deltaRotation;
                return;
            }

            if (_hasAnimator && _input != null && _input.move == Vector2.zero && _speed < 0.1f)
            {
                // Terapkan rotasi murni dari animasi (Root Motion) saat diam/Idle Turn
                // Ini menjamin rotasi karakter 100% bergerak pas & bersamaan dengan langkah kaki animasi Mixamo!
                transform.rotation *= _animator.deltaRotation;
            }
        }

    }

}
