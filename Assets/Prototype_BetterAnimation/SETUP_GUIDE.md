# 🎮 Setup Guide — Prototype Better Animation

Panduan langkah demi langkah untuk menyiapkan sistem animasi baru di Unity Editor.

> [!IMPORTANT]  
> **JANGAN** mengubah file asli! Semua langkah di bawah menggunakan file DUPLIKAT.

---

## Daftar Isi

1. [Persiapan Animasi FBX](#1-persiapan-animasi-fbx)
2. [Duplicate Animator Controller](#2-duplicate-animator-controller)
3. [Tambahkan Parameter Baru](#3-tambahkan-parameter-baru)
4. [Setup State & Transition di Animator](#4-setup-state--transition-di-animator)
5. [Duplicate Prefab Player](#5-duplicate-prefab-player)
6. [Testing](#6-testing)

---

## 1. Persiapan Animasi FBX (Import Settings)

Sebelum mulai, pastikan pengaturan import animasi FBX sudah benar agar karakter tidak "terbang" atau bergeser secara aneh (root motion issues).

Klik file FBX di Project window → buka tab **Animation** di Inspector. 
Cari bagian **Clip** (biasanya bernama `mixamo.com` atau nama animasinya) di bagian bawah tab, klik, lalu atur setting berikut:

### A. Animasi Berjalan/Berlari (Harus Loop)
**File:** `Sprint.fbx`, `LariBerpedang.fbx`

| Pengaturan | Nilai / Centang | Keterangan |
|------------|-----------------|------------|
| **Loop Time** | ✅ **ON (Centang)** | Agar animasi terus diputar |
| **Root Transform Rotation** | | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | Mengunci rotasi |
| ↳ Based Upon | **Original** | |
| ↳ Offset | 0 | |
| **Root Transform Position (Y)** | | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | Mengunci sumbu vertikal (tidak terbang) |
| ↳ Based Upon | **Original** | |
| ↳ Offset | 0 | |
| **Root Transform Position (XZ)**| | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | Mengunci sumbu horizontal (agar script yang mengontrol gerak) |
| ↳ Based Upon | **Original** | |

*(Jangan lupa klik tombol **Apply** di kanan bawah setelah mengubah)*

### B. Animasi One-Shot / Transisi (Tidak Loop)
**File:** `Left Turn.fbx`, `Right Turn.fbx`, `Run To Stop.fbx`, `Running Turn 180.fbx`

| Pengaturan | Nilai / Centang | Keterangan |
|------------|-----------------|------------|
| **Loop Time** | ❌ **OFF (Kosong)** | Hanya dimainkan sekali |
| **Root Transform Rotation** | | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | |
| ↳ Based Upon | **Original** | |
| **Root Transform Position (Y)** | | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | |
| ↳ Based Upon | **Original** | |
| **Root Transform Position (XZ)**| | |
| ↳ Bake Into Pose | ✅ **ON (Centang)** | |
| ↳ Based Upon | **Original** | |

> **Catatan:** Untuk animasi yang diunduh dari Mixamo, sangat disarankan untuk selalu **Mencentang (Bake Into Pose)** pada ketiga Root Transform dan mengubah Based Upon menjadi **Original** agar animasi stabil.

---

## 2. Duplicate Animator Controller

1. Di Project window, navigasi ke:
   ```
   Assets/StarterAssets/ThirdPersonController/Character/Animations/
   ```

2. Klik kanan pada `StarterAssetsThirdPerson.controller`

3. Pilih **Edit → Duplicate** (atau `Ctrl+D`)

4. Rename file duplikat menjadi `StarterAssetsThirdPersonV2.controller`

5. **PINDAHKAN** file ini ke:
   ```
   Assets/Prototype_BetterAnimation/
   ```

> [!TIP]
> Drag & drop file ke folder tujuan di Project window.

---

## 3. Tambahkan Parameter Baru

1. Double-click `StarterAssetsThirdPersonV2.controller` untuk membukanya di **Animator** window

2. Klik tab **Parameters** di kiri atas Animator window

3. Tambahkan parameter berikut (klik tombol `+`):

| Nama Parameter | Tipe | Default | Fungsi |
|---------------|------|---------|--------|
| `IsSprinting` | **Bool** | false | Menandai sedang sprint |
| `TurnAngle` | **Float** | 0 | Arah belok (-1 kiri, +1 kanan) |
| `IsStoppingFromRun` | **Trigger** | — | Trigger animasi berhenti |
| `RunTurn180` | **Trigger** | — | Trigger animasi berbalik 180° |
| `IsArmedRunning` | **Bool** | false | Menandai lari sambil pegang pedang |

> [!WARNING]
> Pastikan nama parameter **PERSIS** seperti di tabel (case-sensitive!). Jika salah satu huruf berbeda, script tidak akan bisa mengontrol animasi.

---

## 4. Setup State & Transition di Animator

### Konsep Dasar

Animator controller V2 ini memperluas Blend Tree locomotion yang sudah ada (`Idle Walk Run Blend`) dengan state-state baru untuk animasi yang lebih kaya.

### A. Buat Sub-State Machine "V2 Locomotion"

1. Di Animator window, pastikan Anda di **Base Layer**
2. Klik kanan pada area kosong → **Create Sub-State Machine** → beri nama `V2 Locomotion`
3. Double-click sub-state machine untuk masuk ke dalamnya

### B. Buat State Baru

Di dalam sub-state machine `V2 Locomotion`, buat state-state berikut:

#### State: Sprint
1. Klik kanan → **Create State** → **Empty**
2. Rename menjadi `Sprint`
3. Di Inspector, set **Motion** → drag `Sprint.fbx` dari folder `8-13-2026` (pilih clip animasi di dalamnya)
4. Set **Speed** = 1

#### State: LeftTurn
1. Buat state baru → rename `LeftTurn`
2. Set Motion → `Left Turn.fbx` (clip `mixamo.com`)
3. Speed = 1

#### State: RightTurn
1. Buat state baru → rename `RightTurn`
2. Set Motion → `Right Turn.fbx` (clip `mixamo.com`)
3. Speed = 1

#### State: RunToStop
1. Buat state baru → rename `RunToStop`
2. Set Motion → `Run To Stop.fbx` (clip di dalamnya)
3. Speed = 1

#### State: RunTurn180
1. Buat state baru → rename `RunTurn180`
2. Set Motion → `Running Turn 180.fbx` (clip di dalamnya)
3. Speed = 1

#### State: ArmedRun
1. Buat state baru → rename `ArmedRun`
2. Set Motion → `LariBerpedang.fbx` (clip di dalamnya)
3. Speed = 1

### C. Setup Transisi

Kembali ke **Base Layer** (klik `Base Layer` di breadcrumb atas).

#### Sprint Transitions

**Dari `Idle Walk Run Blend` → `Sprint` (dalam V2 Locomotion):**
1. Klik kanan pada state `Idle Walk Run Blend` → **Make Transition** → klik pada sub-state machine `V2 Locomotion`, kemudian pilih state `Sprint`
2. Klik pada panah transisi yang baru dibuat
3. Di Inspector:
   - **Has Exit Time** = ❌ (uncheck)
   - **Transition Duration** = 0.15
   - **Conditions**: Tambahkan kondisi:
     - `IsSprinting` = `true`
     - `Speed` Greater than `4`

**Dari `Sprint` → `Idle Walk Run Blend`:**
1. Di dalam `V2 Locomotion`, klik kanan pada `Sprint` → **Make Transition** → target: `(Up) Base Layer` lalu pilih `Idle Walk Run Blend`
2. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.2
   - **Conditions**:
     - `IsSprinting` = `false`

#### Running Turn Transitions

**Dari `Any State` → `LeftTurn`:**
1. Klik kanan pada `Any State` → **Make Transition** → `LeftTurn`
2. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.1
   - **Can Transition To Self** = ❌ (PENTING: Jangan dicentang!)
   - **Conditions**:
     - `TurnAngle` Less than `-0.5`
     - `Speed` Greater than `3`
     - `Grounded` = `true` (Agar tidak belok di udara)

**Dari `LeftTurn` → `Idle Walk Run Blend` (atau locomotion):**
1. Settings:
   - **Has Exit Time** = ✅
   - **Exit Time** = 0.85
   - **Transition Duration** = 0.2
   - **Conditions**: (kosong)

**Dari `Any State` → `RightTurn`:**
1. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.1
   - **Can Transition To Self** = ❌
   - **Conditions**:
     - `TurnAngle` Greater than `0.5`
     - `Speed` Greater than `3`
     - `Grounded` = `true`

**Dari `RightTurn` → `Idle Walk Run Blend`:**
1. Settings:
   - **Has Exit Time** = ✅
   - **Exit Time** = 0.85
   - **Transition Duration** = 0.2

#### Run To Stop Transition

**Dari `Any State` → `RunToStop`:**
1. Klik kanan pada `Any State` → **Make Transition** → `RunToStop`
2. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.1
   - **Can Transition To Self** = ❌
   - **Conditions**:
     - `IsStoppingFromRun` (trigger)
     - `Grounded` = `true`

**Dari `RunToStop` → `Idle Walk Run Blend`:**
1. Settings:
   - **Has Exit Time** = ✅
   - **Exit Time** = 0.9
   - **Transition Duration** = 0.25

#### Running Turn 180 Transition

**Dari `Any State` → `RunTurn180`:**
1. Klik kanan pada `Any State` → **Make Transition** → `RunTurn180`
2. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.1
   - **Can Transition To Self** = ❌
   - **Conditions**:
     - `RunTurn180` (trigger)
     - `Grounded` = `true`

**Dari `RunTurn180` → `Idle Walk Run Blend`:**
1. Settings:
   - **Has Exit Time** = ✅
   - **Exit Time** = 0.85
   - **Transition Duration** = 0.2

#### Armed Run Transitions

**Dari `Idle Walk Run Blend` → `ArmedRun`:**
1. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.15
   - **Conditions**:
     - `IsArmedRunning` = `true`

**Dari `ArmedRun` → `Idle Walk Run Blend`:**
1. Settings:
   - **Has Exit Time** = ❌
   - **Transition Duration** = 0.2
   - **Conditions**:
     - `IsArmedRunning` = `false`

> [!TIP]
> **Tips untuk transisi yang mulus:**
> - Gunakan **Transition Duration** antara 0.1 - 0.25 detik
> - Untuk animasi one-shot (turn, stop), selalu gunakan **Has Exit Time** pada transisi kembali
> - Jangan lupa test setiap transisi satu per satu di Play Mode

---

## 5. Duplicate Prefab Player

### A. Duplicate Prefab

1. Di Project window, navigasi ke:
   ```
   Assets/Prefabs/
   ```

2. Klik kanan pada `PlayerManager.prefab` → **Edit → Duplicate** (`Ctrl+D`)

3. Rename menjadi `PlayerManager_V2.prefab`

4. Pindahkan ke:
   ```
   Assets/Prototype_BetterAnimation/
   ```

### B. Tambahkan Script Animasi V2

1. Double-click `PlayerManager_V2.prefab` untuk membukanya di Prefab Mode

2. **Ganti Animator Controller:**
   - Pilih GameObject yang memiliki komponen **Animator** (biasanya root atau child "model")
   - Di Inspector, pada komponen Animator, ganti **Controller** dari `StarterAssetsThirdPerson` ke `StarterAssetsThirdPersonV2`

3. **Tambahkan Script V2 (Add-on):**
   - Pilih root GameObject dari Prefab.
   - **JANGAN HAPUS** script `Third Person Controller` yang asli! Biarkan saja di sana agar script combat tidak error.
   - **Add Component** → cari `ThirdPersonAnimationV2` (dari namespace `StarterAssets.Prototype`).
   - Script ini adalah "Add-on" yang akan mengirimkan animasi baru ke Animator.

4. Klik **Save** di Prefab Mode (atau keluar dari Prefab Mode)

### C. Gunakan di Scene

1. Buka scene yang ingin ditest
2. **Jangan hapus** PlayerManager yang sudah ada
3. **Nonaktifkan** (uncheck) PlayerManager yang lama
4. Drag `PlayerManager_V2.prefab` ke scene
5. Pastikan posisi dan referensi kamera sudah benar

---

## 6. Testing

### Checklist Test

Jalankan Play Mode dan test satu per satu:

- [ ] **Jalan biasa** (WASD) → animasi Walk normal
- [ ] **Sprint** (tahan Shift + W) → harus pakai animasi Sprint.fbx (beda dari Run biasa)
- [ ] **Belok kiri saat lari** (W + A saat sprint) → animasi Left Turn
- [ ] **Belok kanan saat lari** (W + D saat sprint) → animasi Right Turn
- [ ] **Berhenti tiba-tiba dari lari** (lepas semua tombol saat sprint) → animasi Run To Stop
- [ ] **Berbalik 180°** (tahan W sprint lalu tekan S) → animasi Running Turn 180
- [ ] **Lari pegang pedang** (equip pedang + sprint) → animasi LariBerpedang
- [ ] **Lompat** → masih berfungsi normal
- [ ] **Combat/parkour** → masih berfungsi normal

### Troubleshooting

| Masalah | Solusi |
|---------|--------|
| Animasi tidak berubah saat sprint | Cek apakah parameter `IsSprinting` berubah di Animator window saat Play Mode |
| Karakter freeze saat belok | Pastikan transisi kembali dari LeftTurn/RightTurn ke Idle Walk Run Blend punya `Has Exit Time = true` |
| Animasi Run To Stop tidak trigger | Cek apakah `RunSpeedThreshold` di script V2 terlalu tinggi (default: 3.0) |
| Error "parameter does not exist" | Nama parameter di Animator Controller harus PERSIS sama (case-sensitive) |
| Prefab baru tidak bergerak | Pastikan CharacterController dan PlayerInput masih terpasang |

### Tips Tuning

- **`RunningTurnThreshold`** (default: 30°) — Turunkan jika ingin animasi belok lebih mudah terpicu, naikkan jika terlalu sensitif
- **`Turn180Threshold`** (default: 140°) — Sudut minimum untuk trigger 180 turn
- **`RunSpeedThreshold`** (default: 3.0) — Kecepatan minimum agar dianggap "sedang lari"
- **`TurnAngleSmoothSpeed`** (default: 8) — Kecepatan smoothing parameter TurnAngle

---

## Struktur File Akhir

```
Assets/Prototype_BetterAnimation/
├── Scripts/
│   └── ThirdPersonControllerV2.cs    ← Script baru (sudah dibuat)
├── StarterAssetsThirdPersonV2.controller  ← Duplikat animator (buat manual)
├── PlayerManager_V2.prefab           ← Duplikat prefab (buat manual)
└── SETUP_GUIDE.md                    ← File ini
```
