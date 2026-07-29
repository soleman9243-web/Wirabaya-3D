using System.Collections;
using UnityEngine;

public class DissolveObject : MonoBehaviour
{
    [Header("Settings")]
    public float dissolveDuration = 2f;
    [Tooltip("Apakah objek akan dihapus dari scene setelah hancur?")]
    public bool destroyAfterDissolve = true;
    
    [Tooltip("Tuliskan nama Reference dari property DissolveAmount di Shader Graph-mu (biasanya _DissolveAmount)")]
    public string shaderPropertyReference = "_DissolveAmount";

    private Material[] materials;
    private int dissolveAmountID;

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Menggunakan .materials agar membuat copy (instance) unik
            // Sehingga kalau 1 kayu hancur, kayu lain yang materialnya sama tidak ikut hancur
            materials = renderer.materials;
        }

        dissolveAmountID = Shader.PropertyToID(shaderPropertyReference);
    }

    // Fungsi ini dipanggil dari Unity Event OnInteract
    public void StartDissolve()
    {
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        if (materials == null || materials.Length == 0) yield break;

        float elapsedTime = 0f;

        // Animasikan nilai dari 0 ke 1
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float dissolveValue = Mathf.Lerp(0f, 1f, elapsedTime / dissolveDuration);

            foreach (Material mat in materials)
            {
                mat.SetFloat(dissolveAmountID, dissolveValue);
            }

            yield return null;
        }

        // Pastikan nilainya mentok di 1 pada akhir animasi
        foreach (Material mat in materials)
        {
            mat.SetFloat(dissolveAmountID, 1f);
        }

        if (destroyAfterDissolve)
        {
            Destroy(gameObject);
        }
    }
}
