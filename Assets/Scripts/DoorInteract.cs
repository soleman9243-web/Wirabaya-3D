using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class DoorInteract : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorPivot;          
    public float openAngle = 90f;        
    public float openSpeed = 3f;         
    public bool isOpen = false;          
    public bool isOneWay = false;        
    
    [Tooltip("Jarak maksimal pemain bisa berinteraksi dengan pintu ini")]
    public float interactDistance = 3f;  // <-- Jarak maksimal interaksi

    private Quaternion closedRotation;   
    private Quaternion openRotation;     
    private bool isMoving = false;       

    [Header("Events (Optional)")]
    public UnityEvent OnOpened;          
    public UnityEvent OnClosed;          

    [Header("Outline Settings")]
    public Outline outlineScript;

    // Variabel internal
    private Camera mainCamera;
    private bool isHovered = false;

    void Start()
    {
        if (doorPivot == null)
            doorPivot = transform;

        // Simpan rotasi awal pintu
        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        // Cari Outline otomatis jika belum dimasukkan di Inspector
        if (outlineScript == null)
            outlineScript = GetComponent<Outline>();
            
        // Matikan outline saat game baru dimulai
        if (outlineScript != null)
            outlineScript.enabled = false;

        // Ambil kamera utama di scene
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null) return;

        // 1. Buat garis tak terlihat (Ray) dari titik tengah layar
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // 2. Cek apakah Ray mengenai sebuah objek dan jaraknya tidak lebih dari interactDistance (misal 3 meter)
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Cek apakah objek yang kena Ray adalah pintu ini
            if (hit.collider.gameObject == gameObject)
            {
                // JIKA SEDANG DITATAP (HOVER MASUK)
                if (!isHovered && this.enabled)
                {
                    isHovered = true;
                    if (outlineScript != null) outlineScript.enabled = true;
                }

                // JIKA DIKLIK KIRI SAAT DITATAP
                if (Input.GetMouseButtonDown(0) && this.enabled)
                {
                    ToggleOpenClose();
                }
                
                return; // Hentikan script di sini agar kode di bawah tidak dieksekusi
            }
        }

        // JIKA TIDAK DITATAP (Kamera melengos ke arah lain atau jaraknya kejauhan)
        if (isHovered)
        {
            isHovered = false;
            if (outlineScript != null) outlineScript.enabled = false;
        }
    }

    // ============================================
    // MEMUNCULKAN DOT DI TENGAH LAYAR 
    // ============================================
    void OnGUI()
    {
        // Ukuran dot di layar
        float dotSize = 4f; 
        
        // Mencari posisi tepat di tengah layar
        float posX = (Screen.width / 2) - (dotSize / 2);
        float posY = (Screen.height / 2) - (dotSize / 2);
        
        // Ubah warna Dot jadi Merah kalau sedang menatap pintu, Putih jika tidak
        GUI.color = isHovered ? Color.red : Color.white;
        
        // Gambar Dot-nya
        GUI.DrawTexture(new Rect(posX, posY, dotSize, dotSize), Texture2D.whiteTexture);
    }
    // ============================================

    public void ToggleOpenClose()
    {
        if (isMoving) return; 

        if (isOpen)
        {
            StartCoroutine(CloseDoor());
        }
        else
        {
            StartCoroutine(OpenDoor());
        }
    }

    private System.Collections.IEnumerator OpenDoor()
    {
        isMoving = true;
        float t = 0f;
        Quaternion startRot = doorPivot.localRotation;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            doorPivot.localRotation = Quaternion.Slerp(startRot, openRotation, t);
            yield return null;
        }

        doorPivot.localRotation = openRotation;
        isOpen = true;
        isMoving = false;
        OnOpened?.Invoke();

        if (isOneWay)
        {
            if (outlineScript != null) outlineScript.enabled = false;
            this.enabled = false;
        }
    }

    private System.Collections.IEnumerator CloseDoor()
    {
        isMoving = true;
        float t = 0f;
        Quaternion startRot = doorPivot.localRotation;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            doorPivot.localRotation = Quaternion.Slerp(startRot, closedRotation, t);
            yield return null;
        }

        doorPivot.localRotation = closedRotation;
        isOpen = false;
        isMoving = false;
        OnClosed?.Invoke();
    }
}
