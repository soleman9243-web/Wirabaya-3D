# 📖 Dokumentasi Pembaruan Proyek Unity - Wirabaya 3D

File ini berisi catatan rekam jejak (*changelog*) dari setiap pembaruan, modifikasi script, dan penambahan fitur pada proyek game **Wirabaya 3D**.

---

## 📅 [18 Agustus 2026] - Fitur Dynamic Sprint Camera & Movement Audio Slots

### 1. Perubahan / Pembaruan
* **Dynamic Sprint Camera (AAA Zoom Out Effect)**: Menambahkan fitur zoom out sinematik pada kamera (`PlayerFollowCamera`) ketika karakter sedang lari (*sprint*).
* **Movement Audio Slots (Slot SFX Pergerakan)**: Menambahkan slot variabel `AudioClip` & `AudioSource` di seluruh modul pergerakan pemain (berjalan, melompat, mendarat, lari, parkour, dan takedown).

---

### 2. Penjelasan Detail & Daftar File Script

#### A. Dynamic Sprint Camera (`DynamicSprintCamera.cs`)
* **File Dibuat**: [`Assets/Scripts/Camera/DynamicSprintCamera.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/Scripts/Camera/DynamicSprintCamera.cs)
* **Penjelasan**:
  * Script ini menempel pada `PlayerFollowCamera` (Cinemachine Virtual Camera).
  * Secara otomatis mendeteksi saat pemain bergerak dan menekan tombol Sprint (`LeftShift`).
  * Menggunakan rumus `Mathf.Lerp` untuk mengubah *Field of View* (FOV) dari normal `40°` ke `55°` serta menggeser `Camera Distance` dari `4m` ke `5.2m` secara halus (*fluid transition*).
  * Dilengkapi slot **`Sprint Audio Clip`** dan **`AudioSource`** untuk memutar efek suara angin (*whoosh*) atau derap lari kencang secara otomatis saat mulai sprint.

#### B. Slot Audio Pergerakan Kaki & Lompat (`ThirdPersonController.cs`)
* **File Dimodifikasi**: [`Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs)
* **Penjelasan**:
  * Menambahkan slot **`JumpAudioClip`** pada `ThirdPersonController`.
  * Saat karakter melompat (`_input.jump`), suara melompat diputar di posisi 3D karakter menggunakan `AudioSource.PlayClipAtPoint()`.
  * Melengkapi slot array `FootstepAudioClips` (langkah kaki acak) dan `LandingAudioClip` (suara mendarat saat kaki menyentuh tanah).

#### C. Slot Audio Gerakan Parkour (`ParkourAction.cs` & `ParkourController.cs`)
* **File Dimodifikasi**: 
  * [`Assets/Scripts/Player/ParkourMovement/ParkourAction.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/Scripts/Player/ParkourMovement/ParkourAction.cs)
  * [`Assets/Scripts/Player/ParkourMovement/ParkourController.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/Scripts/Player/ParkourMovement/ParkourController.cs)
* **Penjelasan**:
  * Setiap aset `ParkourAction` (seperti *Vault*, *Climb Up*, *Hurdle*) kini memiliki variabel **`Action Audio Clip`** dan **`Audio Volume`**.
  * `ParkourController` memiliki slot **`AudioSource`** dan secara otomatis memutar SFX gerakan parkour yang sesuai begitu animasi parkour dieksekusi via `PlayOneShot()`.

#### D. Slot Audio Eksekusi Takedown (`PlayerTakedown.cs`)
* **File Dimodifikasi**: [`Assets/Scripts/Player/TakedownSystem/PlayerTakedown.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/Scripts/Player/TakedownSystem/PlayerTakedown.cs)
* **Penjelasan**:
  * Menambahkan slot **`Takedown Start Audio Clip`** (suara gerakan sergapan/dash cepat) dan **`Takedown Impact Audio Clip`** (suara pukulan/hantaman musuh).
  * Memutar SFX otomatis saat tombol interaksi takedown (`E`) dieksekusi pada musuh terdekat.

---

### 3. Petunjuk Penggunaan / Cara Setting di Unity Editor
1. **Setting Dynamic Camera & Audio Sprint**:
   * Klik GameObject `PlayerFollowCamera` ➡️ Inspector ➡️ komponen `Dynamic Sprint Camera`.
   * Masukkan file audio lari ke slot **`Sprint Audio Clip`**.
2. **Setting Audio Karakter (Langkah & Lompat)**:
   * Klik GameObject `PlayerArmature` / `PlayerManager` ➡️ Inspector ➡️ komponen `Third Person Controller`.
   * Isi slot **`Jump Audio Clip`**, **`Landing Audio Clip`**, dan array **`Footstep Audio Clips`**.
3. **Setting Audio Parkour**:
   * Klik file aset `ParkourAction` di Project Window (misal `VaultAction`).
   * Isi slot **`Action Audio Clip`** di section *Audio SFX Setup*.
4. **Setting Audio Takedown**:
   * Klik GameObject Karakter ➡️ Inspector ➡️ komponen `Player Takedown`.
   * Isi slot **`Takedown Start Audio Clip`** dan **`Takedown Impact Audio Clip`**.

---

## 📅 [18 Agustus 2026] - Integrasi Smooth Turn Animation System

### 1. Perubahan / Pembaruan
* **Integrasi Parameter Rotasi `Turn` di Script C#**: Memperbarui `ThirdPersonController.cs` agar menghitung kecepatan rotasi badan karakter (`_rotationVelocity`) dan mengirimkannya secara kontinu ke Animator via parameter float **`Turn`**.
* **Dukungan Animasi Berbelok (Idle Turn Left & Right)**: Memungkinkan Animator untuk melakukan *blending* animasi rotasi tubuh saat berbelok sehingga karakter tidak lagi kaku seperti patung saat memutar arah.

---

### 2. Penjelasan Detail & Daftar File Script
* **File Dimodifikasi**: [`Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs`](file:///c:/Users/USER/Documents/GitHub/Wirabaya-3D/Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs)
* **Penjelasan**:
  * Menambahkan hash ID `_animIDTurn = Animator.StringToHash("Turn");`.
  * Di dalam fungsi `Move()`, script menghitung tingkat kecepatan putar rotasi Y dan mengirimkan nilai ter-clamp (`Mathf.Clamp(_rotationVelocity / 180f, -1f, 1f)`) ke Animator.
  * Saat berbelok ke kiri, nilai `Turn` bernilai negatif (-1.0 s/d 0.0), dan saat berbelok ke kanan bernilai positif (0.0 s/d 1.0).

---

### 3. Petunjuk Penggunaan / Cara Setting di Unity Editor
1. **Import File Mixamo**:
   * Import file `.fbx` animasi turn (`Left Turn.fbx` & `Right Turn.fbx`) ke folder Unity (misal `Assets/StarterAssets/ThirdPersonController/Character/Animations/`).
2. **Set Rig ke Humanoid**:
   * Klik masing-masing file `.fbx` ➡️ Inspector tab **Rig** ➡️ Ubah *Animation Type* menjadi **Humanoid** ➡️ Klik **Apply**.
3. **Pasang di Animator Controller**:
   * Buka **`StarterAssetsThirdPerson.controller`**.
   * Tambahkan parameter baru bertipe **Float** dengan nama exact **`Turn`**.
   * Masukkan state animasi `Left Turn` dan `Right Turn` ke dalam Animator Controller / Blend Tree locomotion.

---

### 4. Perbaikan Bug (WASD Turn Looping & AAA Dynamic Arc-Rotation System)
* **Penyebab**: Script bawaan Unity memutar badan `transform.rotation` secara instan (0.12s) saat tombol WASD ditekan (misal putar balik 180°), sehingga badan karakter "nengok duluan/terjeplak", baru kemudian animasi jalannya diputar.
* **Perbaikan AAA Dynamic Arc-Rotation**:
  * Mengintegrasikan sistem kalkulasi rotasi dinamis berbobot (*Dynamic Arc-Rotation*) pada `ThirdPersonController.cs`.
  * Saat berbelok tajam (90° - 180°), durasi rotasi disesuaikan secara proporsional (`dynamicSmoothTime`) sehingga **rotasi badan dan langkah kaki melangkah berbelok BARENGAN secara mulus dalam sebuah lengkungan sinematik (*turn arc*) ala game AAA** (Witcher 3, GTA V, Red Dead Redemption 2).

---

## 18 Agustus 2026

### 5. Refactor: PlayerCameraController (Pengganti DynamicSprintCamera)
* **Perubahan**: File `DynamicSprintCamera.cs` **DIHAPUS**. Semua fitur kamera player dipindahkan ke script baru **`PlayerCameraController.cs`** (`Assets/Scripts/Camera/PlayerCameraController.cs`).
* **Alasan**: Supaya semua fitur kamera (sprint zoom, dan fitur kamera lain di masa depan) ada di SATU script saja, tidak terpisah-pisah.
* **Fitur yang ada di `PlayerCameraController.cs`**:
  * Sprint FOV zoom-out (40° → 55°)
  * Sprint camera distance offset (4m → 5.2m)
  * Sprint audio SFX slot
  * Area komentar untuk menambah fitur kamera baru di kemudian hari
* **Cara Setting di Unity**: Sama persis seperti sebelumnya — pasang komponen `PlayerCameraController` di GameObject **CinemachineVirtualCamera** (yang sudah ada `DynamicSprintCamera` sebelumnya), lalu **hapus komponen `DynamicSprintCamera`** yang lama dari Inspector.

---

### 6. Fitur NoJump Zone
* **Perubahan**: Menambahkan deteksi layer `NoJump` pada `ThirdPersonController.cs`. Player tidak bisa melompat saat menyentuh object yang berlayer `NoJump`.
* **Cara Setting di Unity**:
  1. Buat layer baru bernama **`NoJump`** (jika belum ada) di Edit → Project Settings → Tags and Layers.
  2. Pada object yang ingin mencegah lompat (misal lantai rumah), ubah **Layer** menjadi **`NoJump`**.
  3. Pastikan object tersebut punya **Collider** (Box Collider, Mesh Collider, dll).
  4. Selesai — player otomatis tidak bisa lompat saat berdiri di atas object tersebut.
