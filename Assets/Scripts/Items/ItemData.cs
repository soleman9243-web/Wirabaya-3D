using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "System/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    public int maxStackSize = 5;

    [Header("Visual Settings")]
    [Tooltip("Nama GameObject yang ada di dalam rig tangan player untuk diaktifkan saat item ini dipegang.")]
    public string heldModelName;
    
    [Tooltip("Prefab untuk item yang jatuh ke tanah.")]
    public GameObject droppedPrefab;

    [Header("Dropped Behavior")]
    public float rotationSpeed = 50f;
    [Tooltip("Daya pantul/lompat saat item didrop dari musuh atau pohon")]
    public float worldDropBounceForce = 5f;
    [Tooltip("Daya pantul/lompat saat item dibuang dari inventory pemain")]
    public float playerDropBounceForce = 2f;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;
    public float floatHeightOffset = 0.5f;

    [Header("Pickup Settings")]
    public float pickupRadius = 2.5f;
    public float pickupDelay = 2.5f;

    [Header("Spawn Settings")]
    [Tooltip("Posisi offset (tambahan jarak) saat item ini spawn dari musuh/pohon")]
    public Vector3 spawnOffset = new Vector3(0, 0.5f, 0);
    [Tooltip("Centang jika ingin memantul saat spawn")]
    public bool autoBounceOnStart = true;
}
