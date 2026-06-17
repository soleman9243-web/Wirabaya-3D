using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public GameObject interactionText;

    public KeyCode interactKey = KeyCode.F;

    private InteractObject currentInteractable;

    [SerializeField] private LayerMask interactMask;

    private InteractObject triggerInteractable;

    private ParkourController parkourController;
    private StarterAssets.ThirdPersonController thirdPersonController;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        parkourController = GetComponent<ParkourController>();
        thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
        
        // 1. Sembunyikan UI saat game baru mulai
        if (interactionText != null)
        {
            interactionText.SetActive(false);
        }
    }

    private void Update()
    {
        // Sembunyikan teks interaksi secara paksa jika pemain sedang sibuk (Parkour, Takedown, atau Dialog)
        bool isBusy = false;
        if (parkourController != null && parkourController.InAction) isBusy = true;
        if (thirdPersonController != null && thirdPersonController.IsInFinisher) isBusy = true;
        if (DialogueManager.Instance != null && DialogueManager.Instance.subtitlePanel != null && DialogueManager.Instance.subtitlePanel.activeSelf) isBusy = true;
        if (TreeCuttingMinigame.Instance != null && TreeCuttingMinigame.Instance.IsPlaying) isBusy = true;

        if (isBusy)
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;
                interactionText.SetActive(false);
            }
            return;
        }

        HandleDetection();
        HandleInput();
    }

    private void HandleDetection()
    {
        InteractObject foundInteractable = null;

        // 1. Prioritas Pertama: Raycast (Bidikan langsung untuk objek yang !useAreaTrigger)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit rayHit, interactionDistance, interactMask))
        {
            InteractObject interactable = rayHit.collider.GetComponent<InteractObject>();
            if (interactable != null && !interactable.useAreaTrigger)
            {
                foundInteractable = interactable;
            }
        }

        // 2. Jika tidak ada yang dibidik, cek apakah Player sedang berdiri di dalam Trigger fisik (OnTriggerEnter)
        if (foundInteractable == null && triggerInteractable != null)
        {
            foundInteractable = triggerInteractable;
        }

        // Update UI
        if (foundInteractable != null)
        {
            string newText = foundInteractable.GetInteractionText();

            // Jika teks kosong (misal karena syarat quest tidak terpenuhi), sembunyikan UI!
            if (string.IsNullOrEmpty(newText))
            {
                if (currentInteractable != null)
                {
                    currentInteractable = null;
                    interactionText.SetActive(false);
                }
            }
            else
            {
                // Tampilkan jika berbeda objek ATAU butuh update teks
                TextMeshProUGUI textComponent = interactionText.GetComponent<TextMeshProUGUI>();
                bool textNeedsUpdate = textComponent != null && textComponent.text != newText;

                if (currentInteractable != foundInteractable || textNeedsUpdate)
                {
                    currentInteractable = foundInteractable;
                    interactionText.SetActive(true);

                    if (textComponent != null)
                    {
                        textComponent.text = newText;
                    }
                }
            }
        }
        else
        {
            // Jika keluar dari area semua objek
            if (currentInteractable != null)
            {
                currentInteractable = null;
                interactionText.SetActive(false);
            }
        }
    }

    private void HandleInput()
    {
        // Untuk testing di komputer menggunakan keyboard
        if (Input.GetKeyDown(interactKey))
        {
            ExecuteInteraction();
        }
    }

    // Method ini bisa dipasang di fungsi OnClick() pada UI Button di layar HP
    public void InteractViaMobileUI()
    {
        ExecuteInteraction();
    }

    private void ExecuteInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        Gizmos.color = Color.red;
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Gizmos.DrawLine(origin, origin + direction * interactionDistance);
        Gizmos.DrawSphere(origin + direction * interactionDistance, 0.05f);
    }

    // Dipanggil otomatis oleh Unity saat Player menyentuh benda ber-Trigger
    private void OnTriggerEnter(Collider other)
    {
        // Pastikan bendanya ada di layer Interact
        if (((1 << other.gameObject.layer) & interactMask) != 0)
        {
            InteractObject interactable = other.GetComponent<InteractObject>();
            if (interactable != null && interactable.useAreaTrigger)
            {
                triggerInteractable = interactable;
            }
        }
    }

    // Dipanggil saat Player keluar dari Trigger tersebut
    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & interactMask) != 0)
        {
            InteractObject interactable = other.GetComponent<InteractObject>();
            if (interactable != null && interactable == triggerInteractable)
            {
                triggerInteractable = null;
            }
        }
    }
}