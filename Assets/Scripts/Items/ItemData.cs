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
    public float bounceForce = 5f;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;
}
