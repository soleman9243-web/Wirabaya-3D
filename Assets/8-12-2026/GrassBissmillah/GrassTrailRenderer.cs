using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// GrassTrailRenderer: Sistem interaksi rumput & jejak langkah (trail).
    /// - Mengirimkan posisi player realtime (_PlayerTramplePos) agar rumput langsung rebah di sekitar kaki.
    /// - Merekam jejak langkah ke RenderTexture (_GrassTrailRT) yang bertahan dan memudar perlahan (recovery time).
    /// - Otomatis mendeteksi player (Tag 'Player' / 'PlayerArmature' / CharacterController) tanpa perlu setup manual yang rumit.
    /// </summary>
    [ExecuteAlways]
    public class GrassTrailRenderer : MonoBehaviour
    {
        [Header("Target Player")]
        [Tooltip("Transform karakter player. Jika kosong, otomatis mencari objek ber-tag 'Player' atau 'PlayerArmature'.")]
        public Transform playerTransform;

        [Header("Real-Time Player Trample")]
        [Tooltip("Radius area rumput yang merunduk & membuka jalan di sekitar player (meter).")]
        [Range(0.3f, 2.0f)]
        public float interactionRadius = 0.65f;

        [Tooltip("Offset titik kaki player.")]
        public Vector3 footOffset = new Vector3(0f, 0.05f, 0f);

        [Header("Trail / Footprint Settings")]
        [Tooltip("Berapa detik sebelum jejak langkah menghilang sepenuhnya (recovery time).")]
        [Range(0.5f, 30f)]
        public float trailRecoveryTime = 5f;

        [Tooltip("Ukuran stempel tapak jejak (persentase dari capture area, ~0.025 = 1.0m).")]
        [Range(0.005f, 0.1f)]
        public float stampRadius = 0.025f;

        [Tooltip("Kekuatan kedalaman jejak (0 = tanpa bekas, 1 = rebah maksimal).")]
        [Range(0f, 1f)]
        public float stampStrength = 0.95f;

        [Header("Capture Area")]
        [Tooltip("Lebar area dunia yang direkam oleh trail RT (meter).")]
        public float captureSize = 40f;

        [Tooltip("Resolusi RenderTexture trail.")]
        public int textureResolution = 512;

        [Header("Debug")]
        public bool showGizmos = true;

        // Internal
        private RenderTexture trailRT_A;
        private RenderTexture trailRT_B;
        private Material scrollFadeMat;
        private Material stampMat;
        private Vector3 lastPlayerPos;
        private Vector2 rtCenter;
        private bool initialized;

        // Shader property IDs
        private static readonly int PlayerTramplePos_ID = Shader.PropertyToID("_PlayerTramplePos");
        private static readonly int PlayerPosition_ID = Shader.PropertyToID("_PlayerPosition");
        private static readonly int PlayerForwardDir_ID = Shader.PropertyToID("_PlayerForwardDir");
        private static readonly int GrassTrailRT_ID = Shader.PropertyToID("_GrassTrailRT");
        private static readonly int GrassTrailCenter_ID = Shader.PropertyToID("_GrassTrailCenter");
        private static readonly int GrassTrailSize_ID = Shader.PropertyToID("_GrassTrailSize");
        private static readonly int ScrollDelta_ID = Shader.PropertyToID("_ScrollDelta");
        private static readonly int FadeAmount_ID = Shader.PropertyToID("_FadeAmount");
        private static readonly int StampUV_ID = Shader.PropertyToID("_StampUV");
        private static readonly int StampRadius_ID = Shader.PropertyToID("_StampRadius");
        private static readonly int StampStrength_ID = Shader.PropertyToID("_StampStrength");

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorAutoSetup()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.update += EditorUpdate;
        }

        private static void EditorUpdate()
        {
            // Di Edit Mode, cari player dan kirim posisi ke shader agar rumput di Scene view juga langsung interaktif
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj == null) pObj = GameObject.Find("PlayerArmature");
            if (pObj != null)
            {
                Vector3 pos = pObj.transform.position;
                Vector3 fwd = pObj.transform.forward;
                Shader.SetGlobalVector(PlayerTramplePos_ID, new Vector4(pos.x, pos.y + 0.05f, pos.z, 0.65f));
                Shader.SetGlobalVector(PlayerPosition_ID, new Vector4(pos.x, pos.y + 0.05f, pos.z, 0.65f));
                Shader.SetGlobalVector(PlayerForwardDir_ID, new Vector4(fwd.x, fwd.z, 0, 0));
            }
        }

        [UnityEditor.MenuItem("Tools/Wirabaya/Pasang Grass Trail Renderer")]
        public static void AddToScene()
        {
            var existing = FindFirstObjectByType<GrassTrailRenderer>();
            if (existing == null)
            {
                var go = new GameObject("GrassTrailSystem");
                var comp = go.AddComponent<GrassTrailRenderer>();
                comp.EnsurePlayerReference();
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create GrassTrailSystem");
                UnityEditor.Selection.activeGameObject = go;
                Debug.Log("[GrassTrailRenderer] Sukses membuat GrassTrailSystem di scene!");
            }
            else
            {
                existing.EnsurePlayerReference();
                UnityEditor.Selection.activeGameObject = existing.gameObject;
                Debug.Log("[GrassTrailRenderer] GrassTrailRenderer sudah aktif di scene!");
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            if (FindFirstObjectByType<GrassTrailRenderer>() == null)
            {
                var go = new GameObject("GrassTrailSystem (Auto)");
                var comp = go.AddComponent<GrassTrailRenderer>();
                comp.EnsurePlayerReference();
                DontDestroyOnLoad(go);
            }
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        public void EnsurePlayerReference()
        {
            if (playerTransform != null && playerTransform.gameObject.activeInHierarchy) return;

            // 1. Tag "Player"
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) { playerTransform = pObj.transform; return; }

            // 2. Cari nama umum karakter pemain
            var pa = GameObject.Find("PlayerArmature");
            if (pa != null) { playerTransform = pa.transform; return; }

            var pm = GameObject.Find("PlayerManager");
            if (pm != null) { playerTransform = pm.transform; return; }

            // 3. Cari CharacterController aktif
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) { playerTransform = cc.transform; return; }

            // 4. Fallback Main Camera jika mode showcase
            if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        private void Initialize()
        {
            EnsurePlayerReference();

            Shader trailShader = Shader.Find("Hidden/FantasyKingdom/GrassTrailSystem");
            if (trailShader == null)
            {
                Debug.LogError("[GrassTrailRenderer] Shader 'Hidden/FantasyKingdom/GrassTrailSystem' tidak ditemukan!");
                return;
            }

            trailRT_A = CreateRT();
            trailRT_B = CreateRT();

            ClearRT(trailRT_A);
            ClearRT(trailRT_B);

            scrollFadeMat = new Material(trailShader);
            stampMat = new Material(trailShader);

            if (playerTransform != null)
            {
                lastPlayerPos = playerTransform.position;
                rtCenter = new Vector2(playerTransform.position.x, playerTransform.position.z);
            }

            initialized = true;
            UpdateGlobalProperties();
        }

        private RenderTexture CreateRT()
        {
            var rt = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.R8);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.name = "GrassTrailRT";
            rt.Create();
            return rt;
        }

        private void ClearRT(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = prev;
        }

        private void Cleanup()
        {
            if (trailRT_A != null) { trailRT_A.Release(); DestroyImmediate(trailRT_A); }
            if (trailRT_B != null) { trailRT_B.Release(); DestroyImmediate(trailRT_B); }
            if (scrollFadeMat != null) DestroyImmediate(scrollFadeMat);
            if (stampMat != null) DestroyImmediate(stampMat);

            Shader.SetGlobalTexture(GrassTrailRT_ID, Texture2D.whiteTexture);
            Shader.SetGlobalVector(PlayerTramplePos_ID, new Vector4(0, -9999, 0, 0));
            Shader.SetGlobalVector(PlayerPosition_ID, new Vector4(0, -9999, 0, 0));
            initialized = false;
        }

        /// <summary>
        /// Mengecek apakah player sedang melayang tinggi di udara (loncat).
        /// Bernilai true HANYA jika karakter melayang lebih dari 0.7 meter di atas tanah.
        /// </summary>
        public bool IsPlayerAirborne()
        {
            if (playerTransform == null) return false;

            // 1. Raycast ke bawah: jika tanah/terrain berada dalam jarak 0.75m di bawah kaki, player dianggap MENAPAK
            bool nearGround = Physics.Raycast(playerTransform.position + Vector3.up * 0.25f, Vector3.down, 0.75f);
            if (nearGround) return false;

            // 2. Jika raycast tidak kena apa pun di bawahnya, cek apakah ada CharacterController dan memang tidak grounded
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null && !cc.isGrounded)
            {
                return true;
            }

            return false;
        }

        private void Update()
        {
            EnsurePlayerReference();
            if (playerTransform != null)
            {
                bool airborne = IsPlayerAirborne();
                float currentRadius = airborne ? 0f : interactionRadius;
                Vector3 pPos = playerTransform.position + footOffset;
                Vector3 fwd = playerTransform.forward;
                Shader.SetGlobalVector(PlayerTramplePos_ID, new Vector4(pPos.x, pPos.y, pPos.z, currentRadius));
                Shader.SetGlobalVector(PlayerPosition_ID, new Vector4(pPos.x, pPos.y, pPos.z, currentRadius));
                Shader.SetGlobalVector(PlayerForwardDir_ID, new Vector4(fwd.x, fwd.z, 0, 0));
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                Initialize();
                if (!initialized) return;
            }

            EnsurePlayerReference();
            if (playerTransform == null || trailRT_A == null || trailRT_B == null) return;

            Vector3 playerPos = playerTransform.position;

            // 1. Hitung pergeseran (scroll delta)
            Vector2 newCenter = new Vector2(playerPos.x, playerPos.z);
            Vector2 worldDelta = newCenter - rtCenter;
            Vector2 scrollDelta = worldDelta / captureSize;
            rtCenter = newCenter;

            // 2. Hitung fade amount berdasarkan recovery time
            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;
            float fadeAmount = dt / Mathf.Max(trailRecoveryTime, 0.1f);

            // Blit Pass 0: Scroll & Fade ke RT_B
            scrollFadeMat.SetVector(ScrollDelta_ID, scrollDelta);
            scrollFadeMat.SetFloat(FadeAmount_ID, fadeAmount);
            Graphics.Blit(trailRT_A, trailRT_B, scrollFadeMat, 0);

            // 3. Stempel tapak jejak player di tengah capture area (hanya jika player menyentuh tanah)
            bool airborne = IsPlayerAirborne();
            if (!airborne)
            {
                Vector2 playerUV = new Vector2(0.5f, 0.5f);
                stampMat.SetVector(StampUV_ID, playerUV);
                stampMat.SetFloat(StampRadius_ID, stampRadius);
                stampMat.SetFloat(StampStrength_ID, stampStrength);
                Graphics.Blit(trailRT_B, trailRT_A, stampMat, 1);
            }
            else
            {
                // Saat melompat di udara, jangan cetak jejak kaki
                Graphics.Blit(trailRT_B, trailRT_A);
            }

            lastPlayerPos = playerPos;

            // 4. Update global shader properties
            UpdateGlobalProperties();
        }

        private void UpdateGlobalProperties()
        {
            if (trailRT_A != null)
            {
                Shader.SetGlobalTexture(GrassTrailRT_ID, trailRT_A);
            }
            Shader.SetGlobalVector(GrassTrailCenter_ID, new Vector4(rtCenter.x, rtCenter.y, 0, 0));
            Shader.SetGlobalFloat(GrassTrailSize_ID, captureSize);

            if (playerTransform != null)
            {
                bool airborne = IsPlayerAirborne();
                float currentRadius = airborne ? 0f : interactionRadius;
                Vector3 pPos = playerTransform.position + footOffset;
                Vector3 fwd = playerTransform.forward;
                Shader.SetGlobalVector(PlayerTramplePos_ID, new Vector4(pPos.x, pPos.y, pPos.z, currentRadius));
                Shader.SetGlobalVector(PlayerPosition_ID, new Vector4(pPos.x, pPos.y, pPos.z, currentRadius));
                Shader.SetGlobalVector(PlayerForwardDir_ID, new Vector4(fwd.x, fwd.z, 0, 0));
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Vector3 center = (playerTransform != null) ? playerTransform.position : transform.position;

            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.25f);
            Gizmos.DrawWireCube(center, new Vector3(captureSize, 0.5f, captureSize));

            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(center + footOffset, interactionRadius);
        }
    }
}
