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

        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;



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



        private const float _threshold = 0.01f;



        private bool _hasAnimator;

        // ===== NO JUMP ZONE =====
        private bool _isOnNoJumpZone = false;
        private int _noJumpLayerIndex;

        // ===== VARIABEL UNTUK PARKOUR SYSTEM =====

        private bool _hasControl = true;
        [Tooltip("Jumlah stamina yang berkurang per detik saat berlari")]
        public float SprintStaminaCost = 15f;


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

        }



        private void LateUpdate()

        {

            if (IsInFinisher)

            {

                return;

            }



            CameraRotation();

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

                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
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
                // Saat combat: tetap terapkan gravitasi vertikal agar player tidak melayang
                // setelah traversal selesai. Hanya gerakan horizontal yang dimatikan.
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                return;
            }

            // --- LOGIKA PENGURANGAN STAMINA LARI ---
            bool isMoving = _input.move != Vector2.zero;
            bool isHoldingSword = _playerControl != null && _playerControl.hasSwordEquipped && !_playerControl.isSwordSheathed;
            bool canSprint = !isHoldingSword || PlayerStatus.Instance.stamina > 0;

            if (_input.sprint && canSprint && isMoving)
            {
                if (isHoldingSword)
                {
                    // Kurangi stamina secara konstan per detik
                    PlayerStatus.Instance.UseStamina(SprintStaminaCost * Time.deltaTime);
                }
            }
            else if (isHoldingSword && PlayerStatus.Instance.stamina <= 0)
            {
                _input.sprint = false; // Paksa berhenti lari jika stamina menyentuh 0
            }

            // Set target speed berdasarkan ketersediaan stamina
            float targetSpeed = (_input.sprint && canSprint) ? SprintSpeed : MoveSpeed;
            // ---------------------------------------

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // ... (Sisanya biarkan sama persis seperti aslinya mulai dari baris di bawah ini)
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;




            // a reference to the players current horizontal velocity

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;



            float speedOffset = 0.1f;

            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;



            // accelerate or decelerate to target speed

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||

                currentHorizontalSpeed > targetSpeed + speedOffset)

            {

                // creates curved result rather than a linear one giving a more organic speed change

                // note T in Lerp is clamped, so we don't need to clamp our speed

                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,

                    Time.deltaTime * SpeedChangeRate);



                // round speed to 3 decimal places

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



            // move the player

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +

                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);



            // update animator if using character

            if (_hasAnimator)

            {

                _animator.SetFloat(_animIDSpeed, _animationBlend);

                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                // Perbaikan Bug: Animasi Turn (Left/Right) HANYA aktif saat berdiri diam (_input.move == Vector2.zero & _speed < 0.1f).
                // Saat berjalan/lari dengan WASD, Turn dipaksa ke 0 agar animasi Walk_N / Run_N berjalan mulus tanpa looping turn!
                float targetTurn = (_input.move == Vector2.zero && _speed < 0.1f) ? Mathf.Clamp(_rotationVelocity / 180f, -1f, 1f) : 0f;
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



                // stop our velocity dropping infinitely when grounded

                if (_verticalVelocity < 0.0f)

                {

                    _verticalVelocity = -2f;

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

            if (animationEvent.animatorClipInfo.weight > 0.5f)

            {

                if (FootstepAudioClips.Length > 0)

                {

                    var index = Random.Range(0, FootstepAudioClips.Length);

                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);

                }

            }

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
            _isOnNoJumpZone = (hit.gameObject.layer == _noJumpLayerIndex);

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



            if (TryGetComponent<CharacterController>(out var characterController))

            {

                characterController.enabled = hasControl;

            }



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
            if (_hasAnimator && _input != null && _input.move == Vector2.zero && _speed < 0.1f)
            {
                // Terapkan rotasi murni dari animasi (Root Motion) saat diam/Idle Turn
                // Ini menjamin rotasi karakter 100% bergerak pas & bersamaan dengan langkah kaki animasi Mixamo!
                transform.rotation *= _animator.deltaRotation;
            }
        }

    }

}