using UnityEngine;

namespace Wirabaya.Rendering
{
    /// <summary>
    /// Menandai seluruh renderer karakter pemain (Wirabaya) dengan MotionVectorGenerationMode.ForceNoMotion,
    /// sehingga URP menghasilkan vektor gerakan nol untuk karakter.
    /// Karakter tidak akan ter-blur, 100% tajam dan bebas ghosting ala game AAA.
    /// </summary>
    public class CharacterMotionBlurMarker : MonoBehaviour
    {
        const string PlayerTag = "Player";
        const string PlayerLayerName = "Player";
        const float ScanInterval = 1.5f;

        static CharacterMotionBlurMarker s_Instance;

        float m_Timer;
        int m_PlayerLayer = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (s_Instance != null)
                return;

            var go = new GameObject("[CharacterMotionBlurMarker]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<CharacterMotionBlurMarker>();
        }

        void Awake()
        {
            m_PlayerLayer = LayerMask.NameToLayer(PlayerLayerName);
            Mark();
        }

        void Start()
        {
            Mark();
        }

        void Update()
        {
            m_Timer -= Time.unscaledDeltaTime;
            if (m_Timer > 0f)
                return;

            m_Timer = ScanInterval;
            Mark();
        }

        public void Mark()
        {
            // 1. Cari via Tag "Player"
            var players = GameObject.FindGameObjectsWithTag(PlayerTag);
            for (int i = 0; i < players.Length; i++)
            {
                ApplyForceNoMotion(players[i]);
            }

            // 2. Cari via Animator untuk memastikan seluruh hierarchy model/armature tercover
            var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            for (int i = 0; i < animators.Length; i++)
            {
                Transform t = animators[i].transform;
                if (t.CompareTag(PlayerTag) || (m_PlayerLayer >= 0 && t.gameObject.layer == m_PlayerLayer))
                {
                    ApplyForceNoMotion(t.gameObject);
                    continue;
                }

                for (Transform p = t.parent; p != null; p = p.parent)
                {
                    if (p.CompareTag(PlayerTag) || (m_PlayerLayer >= 0 && p.gameObject.layer == m_PlayerLayer))
                    {
                        ApplyForceNoMotion(p.gameObject);
                        break;
                    }
                }
            }
        }

        static void ApplyForceNoMotion(GameObject root)
        {
            if (root == null)
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;

                if (renderer.motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion)
                {
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }
            }
        }
    }
}
