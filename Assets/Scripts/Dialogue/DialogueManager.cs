using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using StarterAssets;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Subtitle")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public float typeSpeed = 0.03f;

    [Header("UI Choices")]
    [Tooltip("Masukkan semua Panel Canvas Pilihan (misal: MainChoiceCanvas dan FrontChoiceCanvas)")]
    public GameObject[] choicePanels;
    [Tooltip("Masukkan tempat tombol dimunculkan untuk masing-masing Canvas")]
    public Transform[] choiceContainers;
    [Tooltip("Masukkan Prefab Tombol untuk masing-masing Canvas secara berurutan")]
    public GameObject[] choiceButtonPrefabs;

    [Header("Other UI")]
    [Tooltip("Masukkan GameObjects (misal: Canvas utama, Health Bar, Minimap, dll) yang ingin disembunyikan saat dialog berlangsung.")]
    public GameObject[] uiElementsToHide;

    private DialogueData currentDialogue;
    private int currentNodeIndex;
    private bool isTyping;
    private bool isShowingChoices;
    private Coroutine typingCoroutine;
    
    // Callbacks
    private Action onDialogueComplete;

    // Cameras
    private CinemachineVirtualCameraBase currentNPCCam;
    private CinemachineVirtualCameraBase currentPlayerCam;
    private CinemachineVirtualCameraBase currentChoiceCam;
    private CinemachineVirtualCameraBase activeCam;

    private StarterAssetsInputs playerInputs;
    private ThirdPersonController playerController;
    private Animator playerAnimator;
    private Animator currentNPCAnim;

    private List<DialogueNode> currentBranch;
    private List<int> currentPath = new List<int>();
    private Action<List<int>> onDialogueStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (subtitlePanel) subtitlePanel.SetActive(false);
        if (choicePanels != null)
        {
            foreach (var panel in choicePanels) if (panel) panel.SetActive(false);
        }
    }

    public void StartDialogue(DialogueData data, Action onComplete, CinemachineVirtualCameraBase npcCam, CinemachineVirtualCameraBase playerCam, CinemachineVirtualCameraBase choiceCam, Animator npcAnim, Transform npcTransform = null, List<int> savedPath = null, Action<List<int>> onStateChanged = null)
    {
        currentDialogue = data;
        onDialogueComplete = onComplete;
        currentNPCCam = npcCam;
        currentPlayerCam = playerCam;
        currentChoiceCam = choiceCam;
        currentNPCAnim = npcAnim;
        onDialogueStateChanged = onStateChanged;

        // Sembunyikan UI lain (Health Bar, dll)
        if (uiElementsToHide != null)
        {
            foreach (var uiObj in uiElementsToHide)
            {
                if (uiObj != null) uiObj.SetActive(false);
            }
        }

        currentNodeIndex = 0;
        subtitlePanel.SetActive(true);
        isShowingChoices = false;
        
        if (choicePanels != null)
        {
            foreach (var panel in choicePanels) if (panel) panel.SetActive(false);
        }

        // Cari player inputs & controller jika belum ada
        if (playerInputs == null)
        {
            playerInputs = FindFirstObjectByType<StarterAssetsInputs>();
            playerController = FindFirstObjectByType<ThirdPersonController>();
            if (playerController != null)
            {
                playerAnimator = playerController.GetComponent<Animator>();
            }
        }

        // Cek dan masukkan pedang jika sedang dipegang
        if (playerController != null)
        {
            PlayerControl pc = playerController.GetComponent<PlayerControl>();
            if (pc != null && pc.hasSwordEquipped && !pc.isSwordSheathed)
            {
                pc.ToggleSwordSheath();
            }
        }

        // Matikan gerakan, tapi SEMBUNYIKAN kursor (akan muncul saat ada pilihan)
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = false;
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Paksa animasi kembali ke Idle agar tidak nyangkut di animasi jalan/lari
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetFloat("MotionSpeed", 0f);
        }

        // Bikin Saling Hadap
        if (npcTransform != null && playerController != null)
        {
            Vector3 directionToPlayer = (playerController.transform.position - npcTransform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
                npcTransform.rotation = Quaternion.LookRotation(directionToPlayer);

            Vector3 directionToNPC = (npcTransform.position - playerController.transform.position).normalized;
            directionToNPC.y = 0;
            if (directionToNPC != Vector3.zero)
                playerController.transform.rotation = Quaternion.LookRotation(directionToNPC);
        }

        currentBranch = currentDialogue.nodes;
        currentPath = new List<int>();

        if (savedPath != null && savedPath.Count > 0)
        {
            currentPath.AddRange(savedPath);
            // Melacak ulang cabang terakhir dari memori (Path: [NodeIndex, ChoiceIndex, NodeIndex, ChoiceIndex, ...])
            for (int i = 0; i < currentPath.Count; i += 2)
            {
                int nIdx = currentPath[i];
                int cIdx = currentPath[i + 1];
                currentBranch = currentBranch[nIdx].choices[cIdx].branchNodes;
            }
        }

        currentNodeIndex = 0;
        PlayNode(currentNodeIndex);
    }

    private void PlayNode(int index)
    {
        if (currentBranch == null || index < 0 || index >= currentBranch.Count)
        {
            EndDialogue();
            return;
        }

        // BUG FIX: Update index saat ini!
        currentNodeIndex = index;

        DialogueNode node = currentBranch[index];
        speakerNameText.text = node.speakerName;
        
        // Handle Camera & Animasi
        SwitchCamera(node.cameraID);
        SetTalkingAnimation(node.cameraID);

        // Clear choices
        isShowingChoices = false;

        // Sembunyikan kursor kembali (kursor hanya muncul saat ada pilihan)
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (choiceContainers != null)
        {
            foreach (var container in choiceContainers)
            {
                if (container)
                {
                    foreach (Transform child in container) Destroy(child.gameObject);
                }
            }
        }
        
        if (choicePanels != null)
        {
            foreach (var panel in choicePanels) if (panel) panel.SetActive(false);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(node));
    }

    private IEnumerator TypeSentence(DialogueNode node)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in node.dialogueText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
        
        // Berhenti bicara saat teks selesai
        SetTalkingAnimation("None");

        if (node.hasChoices)
        {
            ShowChoices(node);
        }
    }

    private void Update()
    {
        if (!subtitlePanel.activeSelf) return;

        // Advance dialogue on Left Click, Space, or Enter
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            DialogueNode node = currentBranch[currentNodeIndex];

            if (isTyping)
            {
                // Skip typing
                StopCoroutine(typingCoroutine);
                dialogueText.text = node.dialogueText;
                isTyping = false;
                SetTalkingAnimation("None");
                
                if (node.hasChoices)
                {
                    ShowChoices(node);
                }
            }
            else
            {
                // Advance to next if linear
                if (!node.hasChoices)
                {
                    if (node.endDialogueAfterThis)
                    {
                        EndDialogue();
                    }
                    else if (node.jumpToNode)
                    {
                        PlayNode(node.nextNodeId);
                    }
                    else
                    {
                        currentNodeIndex++;
                        PlayNode(currentNodeIndex);
                    }
                }
            }
        }
    }

    private void ShowChoices(DialogueNode node)
    {
        if (isShowingChoices) return; // Prevent spawning twice
        isShowingChoices = true;

        // Otomatis pindah ke kamera khusus pilihan saat UI pilihan muncul
        SwitchCamera("Choice");

        // Munculkan kursor agar player bisa memilih
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Panel akan diaktifkan secara dinamis oleh ChoiceWallDetector, 
        // tapi kita spawn tombolnya di SEMUA container yang terdaftar.
        
        for (int i = 0; i < node.choices.Length; i++)
        {
            var choice = node.choices[i];
            int cIndex = i; // lambda capture
            int nIndex = currentNodeIndex; // lambda capture

            if (choiceContainers != null)
            {
                for (int c = 0; c < choiceContainers.Length; c++)
                {
                    Transform container = choiceContainers[c];
                    if (container == null) continue;

                    // Ambil prefab yang sesuai dengan index container (fallback ke index terakhir jika kurang)
                    GameObject prefabToUse = null;
                    if (choiceButtonPrefabs != null && choiceButtonPrefabs.Length > 0)
                    {
                        prefabToUse = choiceButtonPrefabs[Mathf.Min(c, choiceButtonPrefabs.Length - 1)];
                    }
                    
                    if (prefabToUse == null) continue;

                    GameObject btnObj = Instantiate(prefabToUse, container);
                    
                    // Reset rotasi dan posisi agar tidak membelakangi atau terbang kemana-mana
                    btnObj.transform.localPosition = Vector3.zero;
                    btnObj.transform.localRotation = Quaternion.identity;
                    btnObj.transform.localScale = Vector3.one;

                    TextMeshProUGUI textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = choice.choiceText;
                    }
                    
                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => OnChoiceSelected(nIndex, cIndex, choice));
                    }
                }
            }
        }
    }

    private void OnChoiceSelected(int nodeIndex, int choiceIndex, DialogueChoice choice)
    {
        // Beritahu NPC bahwa pemain telah membuat pilihan bersarang
        currentPath.Add(nodeIndex);
        currentPath.Add(choiceIndex);
        onDialogueStateChanged?.Invoke(currentPath);
        
        // Pindah cabang atau gunakan logika lawas
        if (choice.branchNodes != null && choice.branchNodes.Count > 0)
        {
            currentBranch = choice.branchNodes;
            currentNodeIndex = 0;
            PlayNode(0);
        }
        else
        {
            if (choice.nextNodeId >= 0) PlayNode(choice.nextNodeId);
            else EndDialogue();
        }
    }

    private void SwitchCamera(string camID)
    {
        if (currentNPCCam) currentNPCCam.Priority = 0;
        if (currentPlayerCam) currentPlayerCam.Priority = 0;
        if (currentChoiceCam) currentChoiceCam.Priority = 0;
        
        // Priority 20 to override the Player's camera
        if (camID == "NPC" && currentNPCCam != null)
        {
            activeCam = currentNPCCam;
            activeCam.Priority = 20;
        }
        else if (camID == "Player" && currentPlayerCam != null)
        {
            activeCam = currentPlayerCam;
            activeCam.Priority = 20;
        }
        else if (camID == "Choice" && currentChoiceCam != null)
        {
            activeCam = currentChoiceCam;
            activeCam.Priority = 20;
        }
        else
        {
            activeCam = null;
        }
    }

    private void SetTalkingAnimation(string camID)
    {
        if (currentNPCAnim != null)
        {
            currentNPCAnim.SetBool("IsTalking", camID == "NPC");
        }
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsTalking", camID == "Player");
        }
    }

    private void EndDialogue()
    {
        subtitlePanel.SetActive(false);
        isShowingChoices = false;
        
        if (choicePanels != null)
        {
            foreach (var panel in choicePanels) if (panel) panel.SetActive(false);
        }
        
        // Kembalikan UI lain
        if (uiElementsToHide != null)
        {
            foreach (var uiObj in uiElementsToHide)
            {
                if (uiObj != null) uiObj.SetActive(true);
            }
        }

        if (activeCam != null)
        {
            activeCam.Priority = 0;
            activeCam = null;
        }

        // Kembalikan gerakan dan sembunyikan kursor
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        onDialogueComplete?.Invoke();
    }
}
