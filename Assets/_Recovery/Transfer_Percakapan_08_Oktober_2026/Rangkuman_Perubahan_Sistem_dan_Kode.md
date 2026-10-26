# RANGKUMAN LENGKAP PERUBAHAN SISTEM, SCRIPT & ASET (08 OKTOBER 2026 - SEKARANG)
**Proyek:** Wirabaya 3D  
**Target Transfer:** Laptop / Environment Baru  
**Tanggal:** 08 Oktober 2026 s/d 26 Oktober 2026  

---

## 1. DAFTAR FILE & SCRIPT YANG DIUBAH / DIBUAT
1. **`Assets/Scripts/UI/SettingsMenuController.cs`**
   - Script pengontrol utama Menu Settings & Pause kustom sesuai layout asli proyek pengguna.
2. **`Assets/Scripts/Camera/PlayerCameraController.cs`**
   - Sinkronisasi nilai sensitivitas mouse dari `SettingsMenuController.MouseSensitivity`.
3. **`Assets/Scripts/Player/FreeflowCombat/PlayerControl.cs`**
   - Penyesuaian input combat dan freeze control saat menu pause aktif.
4. **`Assets/Scripts/Player/FreeflowCombat/TargetDetectionControl.cs`**
   - Penyesuaian deteksi target agar tidak terkunci saat gameplay dijeda.
5. **`Assets/StarterAssets/InputSystem/StarterAssetsInputs.cs`**
   - Dukungan toggle kursor, pause game, dan unscaled input handling.
6. **`Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs`**
   - Dukungan `LockCameraPosition` dan `DisableMovement` saat menu pause terbuka.
7. **`Assets/_Recovery/0 (34).unity` & `0 (37).unity`**
   - Scene hierarchy setup: Canvas sETTINGS, Logo (panel hijau miring), Background (backdrop gelap), tombol Controls/Audio, slider volume & sensitivitas, dropdowns.

---

## 2. DETAIL FITUR & PERBAIKAN SISTEM

### A. Sistem Pause & Resume Gameplay (ESC Toggle)
- **Tombol ESC:** Membuka dan menutup menu secara mulus.
- **Freeze Pemain & Kamera:**
  - `Time.timeScale = 0f` saat menu dibuka, kembali ke `1f` saat ditutup.
  - Kamera Cinemachine/PlayerCameraController dan pergerakan ThirdPersonController dinonaktifkan (`DisableMovement = true`, `LockCameraPosition = true`) agar karakter pemain tidak berputar atau bergerak saat mengatur sensitivitas.
  - Kursor mouse otomatis dibuka (`Cursor.lockState = CursorLockMode.None`, `Cursor.visible = true`) saat pause, dan dikunci kembali (`Locked`, `visible = false`) saat resume.

### B. Anti-Tabrakan HUD Gameplay
- Saat menu settings terbuka, objek HUD gameplay (seperti `NonDialogue`, `QuestUI`, text quest, health bar, dan bilah `TreeCuttingMinigame`) otomatis disembunyikan agar visual tidak bertumpuk/bertabrakan.
- Status aktif sebelumnya dicatat secara dinamis, sehingga saat menu ditutup, HUD gameplay otomatis dikembalikan ke keadaan semula.

### C. Pengaturan Sensitivitas (Maksimal 100% Dual-Binding)
- **Slider:** Rentang dibatasi `1` hingga `100` (`minValue = 1f`, `maxValue = 100f`, `wholeNumbers = true`).
- **TMP_InputField:** Menampilkan format persentase (contoh: `50%`).
- **Dua Arah (Bidirectional):**
  - Menggeser slider langsung mengupdate teks di input field.
  - Mengetik angka di input field langsung mengupdate slider.
  - Nilai tersimpan di `PlayerPrefs` (`Setting_Sensitivity`).

### D. Text Persentase Volume Suara
- Slider volume (Master, BGM, SFX) otomatis dilengkapi teks persentase `0%` - `100%`.
- Jika komponen teks belum ada di scene, script secara cerdas melakukan auto-detect atau auto-create di samping kanan masing-masing slider.
- Sinkronisasi langsung ke `AudioListener.volume` dan `AudioManager.Instance`.

### E. Perbaikan Dropdown (Screen Mode & Motion Blur)
1. **Dropdown Menutupi ke Atas UI Lain (`Render on Top`):**
   - Template kedua dropdown otomatis diberi `Canvas` mandiri dengan `overrideSorting = true` dan `sortingOrder = 32000` (lebih tinggi dari canvas menu `30000`).
   - Hasilnya: `Dropdown List` yang terbuka selalu dirender di paling depan, menutupi elemen UI di bawahnya (seperti tombol, teks, atau slider) tanpa terpotong atau tertimpa.
2. **Dropdown Menutup Otomatis saat Klik di Sekitar / Klik UI Lain:**
   - Komponen `Blocker` otomatis berada di `sortingOrder = 31999`, menangkap semua klik di luar dropdown.
   - Dilengkapi proteksi ganda di `Update()` berbasis raycast: jika mendeteksi klik mouse di luar list dropdown atau pada UI lain (seperti tab lain atau slider), dropdown yang sedang terbuka otomatis menutup (`CloseAllDropdowns()`).
   - Berpindah tab atau mengklik dropdown lain langsung menutup dropdown yang sedang aktif.

### F. Animasi Fade-Out Tombol UI saat Menu Ditutup
- Seluruh elemen tombol & kontrol interaktif (`controls`, `Audio`, slider volume/sensitivitas, input field, tombol quit) otomatis dikelompokkan dengan `CanvasGroup`.
- Saat menu ditutup: seluruh tombol UI melakukan **fade-out** (alpha 1 -> 0) secara halus selama ~0.2 detik, berdampingan dengan panel hijau miring yang melakukan slide-out ke kiri layar dan backdrop meredup.

### G. Efek Blur Kamera / Dunia 3D (URP Gaussian Depth of Field)
- Script secara runtime membuat `SettingsPauseBlurVolume` (Global Volume dengan `priority = 100f`) berisikan `DepthOfField` mode Gaussian (`gaussianStart = 0.01f`, `gaussianEnd = 0.5f`, `blurMaxRadius = 3.5f`).
- Saat menu pause dibuka, dunia 3D dan karakter pemain di belakang menu otomatis dikaburkan (blur) secara transisi halus, didukung oleh backdrop gelap semi-transparan sehingga teks menu sangat jelas terbaca dan karakter pemain tidak kelihatan mencolok.
- Saat menu ditutup, blur kamera bertransisi kembali ke fokus normal gameplay.

---

## 3. INDEKS FOTO YANG DIKIRIM USER (`Foto_User/`)
Folder `Foto_User/` berisi 20 foto/screenshot yang dikirim oleh pengguna sejak 8 Oktober 2026:
1. `media_1791434839075.png` - Screenshot desain awal menu settings dengan tab controls & audio.
2. `media_1791440339721.png` - Screenshot tampilan hierarki scene sETTINGS dan komponen tombol.
3. `media_1791441056943.png` - Screenshot tombol Quit dan slider volume di inspector.
4. `media_1791441125416.png` - Screenshot error console / pesan unity editor.
5. `media_1791441237723.png` - Screenshot perbaikan script error.
6. `media_1791511997671.png` - Screenshot tampilan slider dan input text sensitivitas.
7. `media_1791512593018.png` - Screenshot layout slider yang offset / bertumpuk di panel.
8. `media_1791515749946.png` - Screenshot gameplay HUD yang bertabrakan saat pause.
9. `media_1791516236428.png` - Screenshot UI NonDialogue yang menutupi menu pause.
10. `media_1791517310585.png` - Screenshot tampilan teks persentase volume.
11. `media_1791517427636.png` - Screenshot panel hijau miring (Logo) dan dropdown Options.
12. `media_1791518353315.png` - Screenshot tampilan dropdown Option C dan Option A di atas dunia 3D.
13. `media_1791519927284.png` - Screenshot hierarki objek sETTINGS di Unity Editor.
14. `media_1791527860517.png` - Screenshot bug dropdown yang tertimpa oleh slider di bawahnya.
15. `media_1791527977638.png` - Screenshot tampilan dropdown list yang berada di belakang UI lain.
16. `media_1791527990841.png` - Screenshot tampilan dropdown motion blur saat dibuka.
17. `media_1791528202252.png` - Screenshot menu pause lengkap dengan panel hijau, dropdown, slider, tombol quit, dan karakter pemain di latar belakang 3D.
18. `media_1791528209636.png` - Screenshot perbandingan tampilan tab Audio dan Controls.
19. `media_1791535617279.png` - Screenshot verifikasi kursor mouse dan gameplay freeze.
20. `media_1791536215207.png` - Screenshot hasil akhir implementasi menu pause & settings.

---

## 4. CARA IMPLEMENTASI DI LAPTOP BARU
1. **Pull / Clone Repositori:**
   Buka repositori melalui GitHub Desktop di laptop tujuan dan lakukan `Fetch origin` -> `Pull`.
2. **Buka Scene:**
   Buka scene utama atau scene `Assets/_Recovery/0 (37).unity` di Unity 6 (6000.3.17f1).
3. **Pastikan Script Terpasang:**
   Objek `sETTINGS` di hierarki harus memiliki komponen `SettingsMenuController`.
4. **Tekan Play:**
   Tekan tombol Play, lalu tekan **ESC** untuk membuka menu pause dan menguji seluruh fungsi.
