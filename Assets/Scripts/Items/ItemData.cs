using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "System/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    public int maxStackSize = 5;

    [Header("Visual Settings")]
    [Tooltip("Nama GameObject yang ada di dalam rig tangan player. Pastikan namanya sama persis.")]
    public string heldModelName;
    [Tooltip("Nama parameter Animator (Bool) yang akan menyala saat menenteng item ini")]
    public string holdingAnimatorParameter = "IsHoldingWood";
    
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

    [Header("Visual Stacking")]
    public bool enableVisualStacking = true;
    public int maxVisualStack = 3;
    [Tooltip("Pergeseran tiap item yang ditumpuk saat drop di tanah (termasuk efek zig-zag)")]
    public Vector3 visualStackOffset = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("Pergeseran tiap item yang ditumpuk saat dipegang di tangan (ditumpuk lurus tanpa zig-zag)")]
    public Vector3 handStackOffset = new Vector3(0f, 0.1f, 0f);

    [Header("Pickup Settings")]
    public float pickupRadius = 2.5f;
    public float pickupDelay = 2.5f;

    [Header("Spawn Settings")]
    [Tooltip("Posisi offset (tambahan jarak) saat item ini spawn dari musuh/pohon")]
    public Vector3 spawnOffset = new Vector3(0, 0.5f, 0);
    [Tooltip("Centang jika ingin memantul saat spawn")]
    public bool autoBounceOnStart = true;
}
