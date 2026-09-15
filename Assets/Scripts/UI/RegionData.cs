using UnityEngine;

/// <summary>
/// ScriptableObject yang menyimpan data region untuk animasi discovery.
/// Buat asset baru via: Create → Game → Region Data
/// </summary>
[CreateAssetMenu(fileName = "NewRegion", menuName = "Game/Region Data")]
public class RegionData : ScriptableObject
{
    [Header("Header Label")]
    [Tooltip("Label di bagian atas (contoh: WILAYAH: DITEMUKAN atau REGION: DISCOVERED)")]
    public string headerText = "WILAYAH: DITEMUKAN";

    [Header("Nama Region")]
    [Tooltip("Nama region dalam aksara Jawa (Unicode Javanese block U+A980–U+A9DF)")]
    public string aksaraJawaName = "ꦮꦶꦫꦧꦪ";

    [Tooltip("Nama region dalam huruf Latin (akan ditampilkan setelah decode)")]
    public string latinName = "WIRABAYA";

    [Header("Deskripsi")]
    [Tooltip("Subtitle/deskripsi region yang muncul di bawah nama")]
    public string subtitle = "Lembah Kuno Para Ksatria Timur";
}
