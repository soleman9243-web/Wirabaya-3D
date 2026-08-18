# Panduan Setup Script & ScriptableObject Project Wirabaya-3D

Dokumen ini berisi panduan lengkap dan terperinci untuk melakukan setup script dan konfigurasi ScriptableObject (SO) di dalam project Wirabaya-3D. 

## 1. Player (Karakter Utama)

*   **PlayerControl (FreeflowCombat):**
    *   Pasang script ini di GameObject Player utama.
    *   Assign komponen `Animator` dan `ThirdPersonController`.
    *   Tentukan `Target` (musuh terdekat) yang digunakan untuk auto-aim combat.
*   **PlayerStatus:**
    *   Mengatur Health, Stamina, dan Mana.
    *   Assign referensi UI Canvas: `bloodOverlay` (Image), `staminaImage` (Image), `manaImage` (Image).
    *   Atur kecepatan regenerasi (`healthRegenRate`, `manaRegenRate`) dan delay (`regenDelay`) secara langsung melalui Inspector.
*   **Setup Skill System (Otomatis):**
    *   Pilih Player GameObject di panel Hierarchy.
    *   Pilih menu di bagian atas Unity: `Wirabaya -> Setup Skill System (Base)`.
    *   Sistem akan secara otomatis menambahkan script `SkillManager`, `SkillAwakening` (Tombol V), dan `SkillSpawnBuaya` (Tombol B).
    *   Pada komponen `SkillSpawnBuaya` yang baru ditambahkan, pastikan Anda menaruh prefab 3D model Buaya ke dalam slot yang disediakan.
*   **PlayerParry:**
    *   Mengatur mekanik hindaran/tangkisan (parry). Pasang di GameObject Player.
    *   Assign indikator visual (Spider Sense GameObject) dan `Animator`.
    *   Isi `parryObjectiveId` jika aksi parry ini terikat dengan tugas quest (contoh: `obj_LakukanParry`).
*   **Sistem Parkour:**
    *   Gunakan ScriptableObject **ParkourAction** (Klik Kanan di Project Window -> Create -> Parkour System -> New Parkour Action).
    *   Isi `Anim Name` dengan nama state animasi yang ada di Animator.
    *   Atur `Min Height` dan `Max Height` untuk menentukan batas ukuran tinggi tembok/rintangan yang sesuai dengan animasi ini.
    *   Centang `Enable Target Matching` dan atur parameter `Match Start Time` serta `Match Target Time` agar posisi tangan karakter bisa pas mendarat di ujung tembok/rintangan.
    *   Masukkan file SO ParkourAction yang telah dibuat ke dalam list di komponen `ParkourController` pada Player.

## 2. Enemy (Sistem Musuh & Boss)

*   **EnemyAI & EnemyPatrol:**
    *   Pastikan arena environment sudah di-Bake NavMesh.
    *   Pasang `EnemyAI` dan `EnemyPatrol` di musuh. Assign `Transform Player` dan `Animator`.
    *   Untuk membuat rute patroli, buat beberapa GameObject kosong sebagai titik (Waypoint) dan masukkan transform tersebut ke dalam array `patrolPoints` di komponen `EnemyPatrol`.
*   **EnemyCombatManager:**
    *   Taruh script ini di satu GameObject Manager tunggal yang ada di Scene.
    *   Atur nilai `Max Simultaneous Attackers` agar jumlah musuh yang memukul secara bersamaan dapat dibatasi (menggunakan sistem Freeflow pertempuran bergiliran ala game Arkham).
*   **ScriptableObject BossData:**
    *   Klik Kanan di Project Window -> Create -> Boss -> Boss Data.
    *   Isi `Boss Name` dan `Max Health`.
    *   Isi list `Phases`. Setiap phase (fase pertempuran) memiliki `Health Threshold Percentage` (Kondisi HP untuk memicu fase ini, rentang 0.0 - 1.0) dan daftar list `Allowed Attacks` (serangan yang bisa dipakai).
*   **ScriptableObject BossAttackPattern:**
    *   Klik Kanan di Project Window -> Create -> Boss -> Attack Pattern.
    *   Digunakan untuk mendefinisikan detail masing-masing serangan bos (contoh: Basic Attack, Dash Attack).
    *   Isi `Animation Trigger` (nama teks ini harus sama persis dengan parameter Trigger di Animator Boss).
    *   Atur `Damage`, `Windup Time` (jeda sebelum memukul), `Recovery Delay` (jeda bos diam setelah menyerang), dan centang `Is Dash Attack` jika serangan tersebut berupa terjang. 
    *   Masukkan file SO Attack Pattern ini ke dalam kolom Allowed Attacks pada file SO BossData.

## 3. Sistem Quest & Item

*   **ScriptableObject QuestData:**
    *   Klik Kanan di Project Window -> Create -> Quest System -> Quest.
    *   Isi `Quest Id` (ID unik), `Title` (Judul di UI), dan `Description`.
    *   Pada list `Objectives`, tambahkan objektif misi yang harus diselesaikan pemain. Setiap objektif wajib memiliki `Objective Id` (unik), tipe misi (Collect, Interact, Kill, Reach, Talk, Cutscene), dan jumlah yang dituju pada `Target Amount`.
*   **QuestManager:**
    *   Taruh script ini di objek manajer di Scene. Masukkan file `QuestData` ke kolom Auto Start jika ingin quest langsung berjalan saat scene dimuat.
*   **ScriptableObject ItemData:**
    *   Klik Kanan di Project Window -> Create -> System -> Item Data.
    *   Digunakan untuk mendefinisikan item material (Resource) atau senjata (Weapon) yang bisa dipungut atau di-drop ke tanah.
    *   Isi `Item Name` dan `Item Type`.
    *   Jika item ini merupakan bagian dari quest pungut barang, isi `Quest Objective Id` yang sama persis dengan `Objective Id` yang ada di dalam `QuestData`.
    *   Masukkan prefab 3D model saat benda jatuh ke tanah di `Dropped Prefab`.
    *   Masukkan nama string persis GameObject mesh item (yang menempel sebagai child di tulang tangan karakter) ke kolom `Held Model Name`, dan isi nama parameter boolean animasi karakter ke kolom `Holding Animator Parameter`.

## 4. Dialogue System

*   **ScriptableObject DialogueData:**
    *   Klik Kanan di Project Window -> Create -> Dialogue System -> Dialogue Data.
    *   Buka SO ini di Inspector dan tambahkan elemen (Node) percakapan satu per satu.
    *   Setiap elemen berisi `Speaker Name` (Nama pembicara), `Dialogue Text` (Isi teks), serta tipe kamera Cinemachine yang ingin difokuskan selama dialog berlangsung (`NPC`, `Player`, `Choice`, atau `None`).
    *   Apabila terdapat opsi pilihan berganda untuk pemain, centang kotak `Has Choices` lalu tambahkan opsi-opsi jawabannya ke dalam list choices.
*   **DialogueManager & NPCDialogueTrigger:**
    *   Taruh `DialogueManager` di root Canvas UI, lalu assign referensi komponen teks Subtitle, teks Nama, dan GameObject Panel khusus pilihan.
    *   Taruh komponen `NPCDialogueTrigger` pada karakter NPC di dalam Scene, dan masukkan SO `DialogueData` ke kolom data yang disediakan agar NPC memicu percakapan tersebut saat diajak berinteraksi.

## 5. Save System

Sistem save diatur secara terpusat untuk menyimpan progress antar scene.

*   **SaveManager:**
    *   Wajib ditempatkan sebagai Root GameObject tunggal di Scene (Bukan child dari objek apapun). Script ini menggunakan metode `DontDestroyOnLoad` untuk bertahan selamanya.
*   **Save Providers:**
    *   Berbagai script pendukung (seperti `PlayerSaveProvider` dan `QuestSaveProvider`) harus ditempelkan secara berurutan pada GameObject Player dan QuestManager. Script-script provider ini bertugas menyiapkan dan mengirimkan data milik mereka masing-masing ke master `SaveManager`.
*   **ScriptableObject ChapterDefinition:**
    *   Klik Kanan di Project Window -> Create -> Save System -> Chapter Definition.
    *   Sistem ini berfungsi untuk memetakan letak scene secara logika ke dalam bab cerita (Chapter). Sangat membantu save system dalam mencatat posisi pemain.
    *   Isi `Chapter Id` (Angka) dan `Chapter Title` (Judul bab).
    *   Pada list `Scenes`, masukkan seluruh urutan nama scene secara beruntun dan sama persis dengan penamaan yang terdaftar di Build Settings Unity (contoh: `2ProtAct1`, `3ProtSceneAct1`).

## 6. Transisi Scene (Game Scene Manager)

*   **ScreenFader (Efek Fade In/Out):**
    *   Buat UI Canvas baru (atau gunakan yang ada) dan buat objek UI Image kotak hitam pekat yang menutupi seluruh layar (Stretch).
    *   Pasang script `ScreenFader` pada objek UI Image tersebut. 
    *   Assign komponen Image hitam tadi ke kolom `Fade Image` di script.
    *   Centang `Fade In On Start` jika Anda ingin layar perlahan terang (Fade-In) secara mulus saat scene baru dimuat.
    *   Atur kecepatan durasi memudar di kolom `Fade Duration`.
*   **GameSceneManager:**
    *   Pusat kontrol pemindahan area/level. Taruh komponen ini di sebuah objek manajer di Scene.
    *   Untuk memindahkan scene secara aman, baik lewat klik UI Button di menu utama maupun setelah karakter menyentuh Trigger transisi area di ujung map, panggil fungsi script `GameSceneManager.Instance.ChangeScene("NamaScene")`.
    *   Sistem ini secara otomatis akan memicu `ScreenFader` yang Anda pasang sebelumnya agar layar memudar menjadi gelap (fade-out), memuat scene baru di latar belakang, dan mencegah freeze mendadak di layar pemain.

## 7. Audio System

*   **AudioManager:**
    *   Manajer utama suara SFX dan musik. Masukkan semua klip audio Anda ke dalam array Sound yang disediakan.
*   **AnimationSoundPlayer:**
    *   Letakkan komponen script ini secara berdampingan dengan Animator (Karakter atau Musuh).
    *   Buka jendela panel Animation Unity (Saat mengedit klip animasi seperti Berjalan atau Menyerang), lalu buat Animation Event keyframe pada frame tertentu yang diinginkan.
    *   Pada panel Event, pilih dan panggil fungsi `PlayRandomFromGroup` (Ketik parameter string nama grupnya di bawah kotak fungsi, misal "Step" untuk suara langkah acak), atau panggil fungsi `PlaySoundByName` (Ketik parameter string nama audio satuan spesifiknya). Suara akan langsung dieksekusi tepat pada frame tersebut, cocok untuk timing suara langkah dan pukulan senjata.

## 8. Cutscene System

Sistem untuk menjalankan dan melewati (skip) cutscene, baik yang berbasis gambar bercerita maupun cutscene dari Timeline.

*   **ImageCutscene (Cutscene Gambar Teks):**
    *   Taruh script ini di GameObject CutsceneManager (atau langsung di Canvas Cutscene).
    *   Assign referensi UI `Text Component` (berbasis TextMeshPro) dan `Image Component`.
    *   Pada array `Frames`, isi urutan adegan cerita. Setiap frame berisi teks narasi dan gambar latar/karakter yang sesuai. Script ini akan merender frame satu per satu secara berurutan sesuai dialog cerita.
*   **TimelineSceneLoader:**
    *   Digunakan khusus jika Anda membuat cutscene menggunakan Unity Timeline (Playable Director) dan ingin otomatis memicu perpindahan scene begitu Timeline selesai diputar.
    *   **Cara Pasang Signal:**
        1.  Tambahkan komponen `TimelineSceneLoader` ke GameObject Playable Director (tempat Timeline Anda berada).
        2.  Buka panel Timeline, klik tombol ikon Peniti/Gembok (Add Marker/Signal Track).
        3.  Klik kanan pada track Signal tersebut di detik terakhir cutscene Anda, lalu pilih *Add Signal Emitter*.
        4.  Buat/Pilih *Signal Asset* baru.
        5.  Pada Signal Receiver yang muncul di Inspector, tambahkan event baru (+).
        6.  Drag GameObject yang memiliki `TimelineSceneLoader` ke kotak objek yang tersedia.
        7.  Pilih fungsi `TimelineSceneLoader -> LoadScene (string)`.
        8.  Ketik nama Scene tujuan Anda ke kotak string parameter yang muncul.
*   **CutsceneSkipper:**
    *   Digunakan agar pemain memiliki opsi menekan (hold) tombol untuk memotong (skip) cutscene panjang.
    *   Taruh script ini di objek manajer cutscene.
    *   Pada array `Skip Inputs`, tambahkan tombol input (misal: tombol `Space` atau `Escape`) beserta GameObject UI Icon lingkaran loading skip-nya.
    *   Atur `Hold Duration` untuk menentukan durasi berapa detik pemain harus menahan tombol tersebut secara konstan hingga cutscene benar-benar dihentikan/di-skip.

## 9. Sistem Interaksi (Pintu & Benda)

*   **InteractObject (Sistem Interaksi Benda/NPC):**
    *   Taruh script ini pada model 3D benda statis (peti, tuas) atau NPC yang bisa diajak berinteraksi.
    *   Centang opsi `Use Area Trigger` jika interaksi terjadi saat pemain memasuki zona dekat benda tersebut (tanpa perlu repot mengarahkan kamera). Jika tidak dicentang, pemain harus menatap persis ke arah benda tersebut menggunakan sistem Raycast (kamera kursor bidik).
    *   Manfaatkan `On Interact` (UnityEvent) untuk memanggil fungsi mekanik secara fleksibel saat pemain menekan tombol aksi. Contoh penggunaannya: memanggil script pembuka pintu, memicu masuk scene dialog NPC, atau memicu transisi scene level baru.
    *   Jika benda/interaksi ini dibatasi (dikunci) oleh quest, isikan `Require Completed Objective Id` dengan ID quest terkait, sehingga benda ini baru bisa diinteraksi hanya setelah pemain menuntaskan quest tersebut.
*   **InteractDoor (Sistem Pintu):**
    *   Script mekanik khusus pintu ayun. Letakkan di induk model pintu.
    *   Isi referensi `Door Pivot` dengan objek engsel pintu (tempat titik poros putaran pintu).
    *   Atur besaran bukaan di `Open Angle` (umumnya 90 derajat) dan kecepatan engsel di `Open Speed`.
    *   Panggil fungsi `ToggleOpenClose()` melalui UnityEvent (misalnya dari klik UI Button, atau dari trigger `InteractObject` di atas) untuk membuka atau menutup pintu tersebut secara dinamis.

---
Catatan: Semua script bawaan utilitas atau demo dari asset luar (contohnya Fantasy Kingdom) telah dipindahkan dan dirapikan letaknya ke dalam folder khusus `Assets/Scripts/_ThirdParty_FantasyKingdom` demi menjaga susunan script internal yang rapi.
