using UnityEngine;
using System.Collections;
using UnityEngine.Events; // <-- untuk event OnOpened & OnClosed
using UnityEngine.UI;

public class InteractDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorPivot;          // Titik poros pintu
    public float openAngle = 90f;        // Sudut pintu terbuka
    public float openSpeed = 3f;         // Kecepatan buka/tutup
    public bool isOpen = false;          // Status pintu terbuka/tutup
    public bool isOneWay = false;        // Jika true, hanya bisa dibuka sekali

    private Quaternion closedRotation;   // Rotasi awal pintu
    private Quaternion openRotation;     // Rotasi terbuka
    private bool isMoving = false;       // Mencegah animasi ganda

    [Header("Events (Optional)")]
    public UnityEvent OnOpened;          // Bisa diisi suara / efek
    public UnityEvent OnClosed;          // Bisa diisi suara / efek

   
    void Start()
    {
        if (doorPivot == null)
            doorPivot = transform;

        // Simpan rotasi awal pintu
        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void ToggleOpenClose()
    {
        if (isMoving) return; // Jangan jalankan dua kali

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
            this.enabled = false;
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
