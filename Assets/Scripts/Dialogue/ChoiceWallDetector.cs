using UnityEngine;
using Cinemachine;

public class ChoiceWallDetector : MonoBehaviour
{
    [Tooltip("Kamera depan yang akan menyala jika kamera ini terhalang tembok")]
    public CinemachineVirtualCameraBase frontCamera;
    
    [Tooltip("Layer tembok atau rintangan")]
    public LayerMask obstacleLayer;

    [Tooltip("Transform dari karakter Player (contoh: PlayerArmature atau Hips)")]
    public Transform playerTransform;

    [Header("UI Canvas Switching")]
    [Tooltip("Canvas pilihan yang muncul jika kamera belakang aktif (kiri)")]
    public GameObject mainCanvas;
    
    [Tooltip("Canvas pilihan yang muncul jika kamera depan aktif (kanan)")]
    public GameObject frontCanvas;

    private CinemachineVirtualCameraBase backCamera;

    void Awake()
    {
        backCamera = GetComponent<CinemachineVirtualCameraBase>();
    }

    void Update()
    {
        if (backCamera == null || frontCamera == null) return;

        // Script ini hanya bekerja jika kamera belakang sedang diaktifkan oleh DialogueManager
        if (backCamera.Priority > 0)
        {
            bool isBlocked = false;

            // Cek tabrakan dari arah kamera menuju tubuh Player
            if (playerTransform != null)
            {
                // Gunakan posisi player + sedikit offset ke atas (misal setinggi dada) agar tidak menabrak lantai
                Vector3 targetPos = playerTransform.position + Vector3.up * 1.2f;
                Vector3 camPos = transform.position;
                Vector3 dirToTarget = (targetPos - camPos).normalized;
                float distToTarget = Vector3.Distance(camPos, targetPos);

                RaycastHit hit;
                // Tarik garis (Raycast) dari KAMERA ke TARGET (untuk mencegah bug jika target berada di dalam tembok)
                if (Physics.Raycast(camPos, dirToTarget, out hit, distToTarget, obstacleLayer))
                {
                    isBlocked = true;
                }
            }

            if (isBlocked)
            {
                // Jika terhalang tembok, buat kamera depan "mengalahkan" kamera belakang
                frontCamera.Priority = backCamera.Priority + 1;
                if (mainCanvas != null) mainCanvas.SetActive(false);
                if (frontCanvas != null) frontCanvas.SetActive(true);
            }
            else
            {
                // Jika aman, matikan kamera depan
                frontCamera.Priority = 0;
                if (mainCanvas != null) mainCanvas.SetActive(true);
                if (frontCanvas != null) frontCanvas.SetActive(false);
            }
        }
        else
        {
            // Jika kamera belakang mati (Priority 0), pastikan kamera depan dan UI juga mati
            frontCamera.Priority = 0;
            if (mainCanvas != null) mainCanvas.SetActive(false);
            if (frontCanvas != null) frontCanvas.SetActive(false);
        }
    }
}
