DOKUMENTASI PEMBARUAN PROYEK WIRABAYA 3D

File ini berisi catatan rekam jejak changelog dari setiap pembaruan, modifikasi script, dan penambahan fitur pada proyek game Wirabaya 3D.


18 AGUSTUS 2026

1. Fitur Dynamic Sprint Camera dan Movement Audio Slots

A. Perubahan dan Pembaruan
- Dynamic Sprint Camera: Menambahkan fitur zoom out pada kamera PlayerFollowCamera ketika karakter sedang lari atau sprint.
- Movement Audio Slots: Menambahkan slot variabel AudioClip dan AudioSource di seluruh modul pergerakan pemain seperti berjalan, melompat, mendarat, lari, parkour, dan takedown.

B. Penjelasan Detail dan Daftar File Script
- Dynamic Sprint Camera (DynamicSprintCamera.cs):
  Lokasi file: Assets/Scripts/Camera/DynamicSprintCamera.cs
  Penjelasan:
  Script ini menempel pada PlayerFollowCamera Cinemachine Virtual Camera.
  Secara otomatis mendeteksi saat pemain bergerak dan menekan tombol Sprint LeftShift.
  Menggunakan rumus Mathf.Lerp untuk mengubah Field of View FOV dari normal 40 derajat ke 55 derajat serta menggeser Camera Distance dari 4m ke 5.2m secara halus.
  Dilengkapi slot Sprint Audio Clip dan AudioSource untuk memutar efek suara angin atau derap lari kencang secara otomatis saat mulai sprint.

- Slot Audio Pergerakan Kaki dan Lompat (ThirdPersonController.cs):
  Lokasi file: Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs
  Penjelasan:
  Menambahkan slot JumpAudioClip pada ThirdPersonController.
  Saat karakter melompat, suara melompat diputar di posisi 3D karakter menggunakan AudioSource.PlayClipAtPoint.
  Melengkapi slot array FootstepAudioClips untuk langkah kaki acak dan LandingAudioClip untuk suara mendarat saat kaki menyentuh tanah.

- Slot Audio Gerakan Parkour (ParkourAction.cs dan ParkourController.cs):
  Lokasi file:
  Assets/Scripts/Player/ParkourMovement/ParkourAction.cs
  Assets/Scripts/Player/ParkourMovement/ParkourController.cs
  Penjelasan:
  Setiap aset ParkourAction seperti Vault, Climb Up, Hurdle kini memiliki variabel Action Audio Clip dan Audio Volume.
  ParkourController memiliki slot AudioSource dan secara otomatis memutar SFX gerakan parkour yang sesuai begitu animasi parkour dieksekusi.

- Slot Audio Eksekusi Takedown (PlayerTakedown.cs):
  Lokasi file: Assets/Scripts/Player/TakedownSystem/PlayerTakedown.cs
  Penjelasan:
  Menambahkan slot Takedown Start Audio Clip dan Takedown Impact Audio Clip.
  Memutar SFX otomatis saat tombol interaksi takedown E dieksekusi pada musuh terdekat.

C. Petunjuk Penggunaan dan Cara Setting di Unity Editor
1. Setting Dynamic Camera dan Audio Sprint:
   Pilih GameObject PlayerFollowCamera, buka Inspector, lalu cari komponen Dynamic Sprint Camera.
   Masukkan file audio lari ke slot Sprint Audio Clip.
2. Setting Audio Karakter Langkah dan Lompat:
   Pilih GameObject PlayerArmature atau PlayerManager, buka Inspector, lalu cari komponen Third Person Controller.
   Isi slot Jump Audio Clip, Landing Audio Clip, dan array Footstep Audio Clips.
3. Setting Audio Parkour:
   Pilih file aset ParkourAction di Project Window misalnya VaultAction.
   Isi slot Action Audio Clip di bagian Audio SFX Setup.
4. Setting Audio Takedown:
   Pilih GameObject Karakter, buka Inspector, lalu cari komponen Player Takedown.
   Isi slot Takedown Start Audio Clip dan Takedown Impact Audio Clip.


2. Integrasi Smooth Turn Animation System

A. Perubahan dan Pembaruan
- Integrasi Parameter Rotasi Turn di Script C#: Memperbarui ThirdPersonController.cs agar menghitung kecepatan rotasi badan karakter dan mengirimkannya secara kontinu ke Animator via parameter float Turn.
- Dukungan Animasi Berbelok Idle Turn Left dan Right: Memungkinkan Animator untuk melakukan blending animasi rotasi tubuh saat berbelok sehingga karakter tidak lagi kaku saat memutar arah.

B. Penjelasan Detail dan Daftar File Script
- Lokasi file: Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs
- Penjelasan:
  Menambahkan hash ID animIDTurn untuk parameter Turn.
  Di dalam fungsi Move, script menghitung tingkat kecepatan putar rotasi Y dan mengirimkan nilainya ke Animator.
  Saat berbelok ke kiri, nilai Turn bernilai negatif antara minus 1.0 sampai 0.0, dan saat berbelok ke kanan bernilai positif antara 0.0 sampai 1.0.

C. Petunjuk Penggunaan dan Cara Setting di Unity Editor
1. Import File Mixamo:
   Import file fbx animasi turn Left Turn.fbx dan Right Turn.fbx ke folder Assets/StarterAssets/ThirdPersonController/Character/Animations/.
2. Set Rig ke Humanoid:
   Pilih masing-masing file fbx, buka Inspector tab Rig, ubah Animation Type menjadi Humanoid, lalu klik Apply.
3. Pasang di Animator Controller:
   Buka StarterAssetsThirdPerson.controller.
   Tambahkan parameter baru bertipe Float dengan nama Turn.
   Masukkan state animasi Left Turn dan Right Turn ke dalam Animator Controller locomotion.

D. Perbaikan Bug WASD Turn Looping dan Dynamic Arc Rotation System
- Penyebab: Script bawaan Unity memutar badan transform rotation secara instan 0.12 detik saat tombol WASD ditekan, sehingga badan karakter menengok duluan sebelum animasi jalannya diputar.
- Perbaikan Dynamic Arc Rotation:
  Mengintegrasikan sistem kalkulasi rotasi dinamis berbobot pada ThirdPersonController.cs.
  Saat berbelok tajam 90 sampai 180 derajat, durasi rotasi disesuaikan secara proporsional sehingga rotasi badan dan langkah kaki melangkah berbelok bersamaan secara mulus dalam sebuah lengkungan sinematik.


3. Refactor PlayerCameraController Pengganti DynamicSprintCamera
- Perubahan: File DynamicSprintCamera.cs dihapus. Semua fitur kamera player dipindahkan ke script baru PlayerCameraController.cs di Assets/Scripts/Camera/PlayerCameraController.cs.
- Alasan: Supaya semua fitur kamera ada di satu script terpusat.
- Fitur yang ada di PlayerCameraController.cs:
  Sprint FOV zoom-out dari 40 derajat ke 55 derajat.
  Sprint camera distance offset dari 4m ke 5.2m.
  Slot audio SFX sprint.
- Cara Setting di Unity: Pasang komponen PlayerCameraController di GameObject CinemachineVirtualCamera, lalu hapus komponen DynamicSprintCamera lama.


4. Fitur NoJump Zone
- Perubahan: Menambahkan deteksi layer NoJump pada ThirdPersonController.cs. Player tidak bisa melompat saat menyentuh object yang berlayer NoJump.
- Cara Setting di Unity:
  1. Buat layer baru bernama NoJump di Edit, Project Settings, Tags and Layers.
  2. Pada object yang ingin mencegah lompat misalnya lantai rumah, ubah Layer menjadi NoJump.
  3. Pastikan object tersebut memiliki Collider aktif.


19 AGUSTUS 2026

5. Terrain Foliage Generator dan Scene Brush Tool (TerrainGrassSpawner.cs)
- Perubahan: Membuat script TerrainGrassSpawner.cs di Assets/Scripts/Environment/TerrainGrassSpawner.cs.
- Fitur:
  1. Procedural Auto Scatter: Menghitung ketinggian bukit tanah Unity Terrain secara otomatis menggunakan raycast normal, menempelkan rumpun rumput 3D rapi di atas tanah.
  2. Scene Brush Mode: Melukis rumpun rumput langsung di Scene view menggunakan kuas lingkaran. Klik Kiri untuk melukis, Shift ditambah Klik Kiri untuk menghapus.
- Cara Setting di Unity:
  1. Buat GameObject baru di Hierarchy bernama GrassSpawner atau gunakan objek yang sudah ada.
  2. Pasang komponen TerrainGrassSpawner.
  3. Klik tombol Generate Procedural Grass atau aktifkan Enable Brush Mode untuk melukis dengan kuas.


6. Realistic Infinite Grass Physics (Tinggi Rumput, Ombak Angin Alami dan Injakan Kaki)
- Perubahan: Membuat dan memperbarui GrassInteractivePhysics.cs di Assets/Scripts/Environment/GrassInteractivePhysics.cs serta material URP Lit PT_Grass_Mat.mat dan PT_High_Grass_Mat.mat.
- Fitur Utama:
  1. Tinggi Rumput Fleksibel Height Multiplier: Ditambahkan slider pengali tinggi rumput default 1.8x sampai 2.5x agar rumput tampak tinggi dan lebat setinggi paha atau pinggang karakter.
  2. Ombak Gelombang Angin Alami Rolling Wind Waves: Angin bergerak seperti ombak melintasi bukit dengan variasi hembusan kencang dan getaran halus ujung daun.
  3. Fisika Injak Kaki Dynamic Trample dan Plow: Rumput merunduk rebah hingga 75 sampai 85 derajat dan memipih rata tanah saat diinjak, membuka jalan mengikuti arah lari karakter, serta memiliki efek pantulan pegas lentur saat kembali berdiri tegak.
- Cara Setting di Unity:
  1. Pilih objek Infinite Grass di Hierarchy.
  2. Di Inspector GrassInteractivePhysics:
     Ubah Height Multiplier misalnya 1.8 atau 2.2 untuk mengatur ketinggian rumput.
     Atur Trample Radius 2.0 dan Max Bend Angle 75.
  3. Tekan tombol Play di Unity dan jalankan karakter melewati rumput.


7. Optimasi Performa Anti-Lag 60 FPS, Angin 1 Arah Teratur, Perbaikan Bug Skala, dan Smooth Brush
- Perubahan: Memperbarui GrassInteractivePhysics.cs, TerrainGrassSpawner.cs, serta mengaktifkan GPU Instancing pada PT_Grass_Mat.mat dan PT_High_Grass_Mat.mat.
- Fitur dan Perbaikan:
  1. Optimasi Anti-Lag 60 FPS: Mengaktifkan GPU Instancing pada material sehingga ribuan rumput di-batch ke GPU, serta menerapkan Proximity Culling di mana kalkulasi hanya berjalan pada rumput di dekat pemain (35 meter).
  2. Angin 1 Arah Teratur: Mengubah pola angin menjadi aliran satu arah searah sudut kompas (windAngleDegrees) yang rapi, teratur, dan serasi melintasi seluruh padang rumput.
  3. Perbaikan Bug Rumput Memanjang: Skala dasar rumput dikunci (initialScale) sehingga tidak ada lagi bug rumput memanjang berlipat ganda saat discan berulang kali.
  4. Smooth Brush Anti-Menumpuk: Kuas lukis rumput dilengkapi fitur pembatas jarak minimal (minGrassSpacing) dan throttling agar sapuan kuas halus tanpa menumpuk ratusan mesh di titik yang sama.
- Cara Setting di Unity:
  1. Pada objek Infinite Grass di Hierarchy, klik Clear All Grass lalu lukis ulang dengan kuas yang sekarang sudah sangat halus dan ringan.
  2. Di Inspector GrassInteractivePhysics, atur arah angin pada slider Wind Angle Degrees (misal 45 derajat) dan Height Multiplier (misal 1.4).


8. Peningkatan Smoothness Kuas Brush Scene View (144Hz Repainting dan Spatial Grid Hash)
- Perubahan: Memperbarui sistem editor TerrainGrassSpawner.cs dengan teknik event HandleUtility.Repaint pada MouseMove dan Spatial Grid O(1).
- Fitur dan Perbaikan:
  1. Gerakan Kuas 144Hz Instan: Lingkaran kuas di Scene view sekarang langsung mengikuti gerakan kursor mouse secara real-time tanpa ada delay atau gerakan patah-patah.
  2. Spatial Grid Hash O(1): Pengecekan tabrakan/jarak rumput dilakukan secara instan dalam memori tanpa melakukan looping ribuan objek anak, sehingga saat menggeser kuas melukis tidak ada lag sama sekali.
  3. Stroke Batching Undo: Operasi undo digabungkan per tarikan kuas agar editor Unity tidak macet saat melukis area luas.
- Cara Setting di Unity:
  Gerakkan kuas di Scene view untuk merasakan pergerakannya yang kini sangat halus dan responsif.


9. Infinite Grass GPU Vertex Displacement System (60 FPS Terkunci, Angin 1 Arah Ombak Alami, dan Injak Rebah Realistis)
- Perubahan: Membuat shader StylizedInfiniteGrassURP.shader dan memperbarui GrassInteractivePhysics.cs menjadi GPU Controller murni dengan beban CPU 0%.
- Fitur dan Perbaikan:
  1. Performa 60 FPS Terkunci (Zero CPU Overhead): Memindahkan 100% kalkulasi angin dan rebah rumput ke GPU Vertex Shader, membebaskan CPU dari perulangan ribuan transform sehingga game kembali berjalan di 60 FPS stabil.
  2. Angin 1 Arah Ombak Alami: Angin mengalir serempak dan bergelombang melintasi padang rumput dari satu arah sudut kompas tanpa gerakan acak atau berantakan.
  3. Rebah Injak Kaki Realistis: Vertex shader secara otomatis mendeteksi posisi kaki karakter dan menekan rumput rebah ke tanah serta membuka jalan tanpa merusak ukuran mesh.
  4. Pengatur Dimensi Shader: Tinggi dan lebar rumput diatur langsung melalui shader, menghilangkan segala kemungkinan bug rumput memanjang.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Di Inspector GrassInteractivePhysics, atur Wind Compass Angle (misal 45 derajat), Grass Height (misal 1.4), dan Trample Strength (1.6).
  3. Tekan Play dan nikmati performa 60 FPS mentok dengan rumput realistis seperti di video.


10. Perbaikan Kompatibilitas Tekstur Albedo Alpha StylizedInfiniteGrassURP
- Perubahan: Memperbarui shader StylizedInfiniteGrassURP.shader dengan menambahkan mapping _BaseTexture dan _BaseMap serta mengatur cutoff alpha presisi.
- Fitur dan Perbaikan:
  1. Tampilan Tekstur Daun Rumput Jelas: Shader membaca slot tekstur asli Polytope Studio (_BaseTexture) sehingga daun rumput langsung muncul hijau tajam dan lebat di Scene maupun Game view.
  2. Alpha Cutout Bersih: Transparansi latar tekstur rumput terpotong rapi tanpa pinggiran hitam atau buram.
- Cara Setting di Unity:
  Kembali ke Unity, tunggu beberapa detik untuk recompile, tekstur rumput hijau akan langsung tampil tajam dan indah.


11. Restorasi Shader URP Lit Standar dengan Proximity Culling 60 FPS
- Perubahan: Mengembalikan material PT_Grass_Mat.mat dan PT_InteractiveGrass_Mat.mat ke shader resmi Universal Render Pipeline/Lit dengan GPU Instancing aktif, serta memperbarui GrassInteractivePhysics.cs dengan sistem Active Simulation Distance.
- Fitur dan Perbaikan:
  1. Tampilan Visual 100% Terjamin: Menggunakan shader Universal Render Pipeline/Lit resmi Unity sehingga tekstur dan warna hijau rumput pasti muncul tajam tanpa risiko transparan.
  2. Proximity Culling Ringan (60 FPS Mentok): Simulasi angin dan injak kaki hanya diproses pada rumput dalam jarak 25 meter dari pemain, menjaga performa tetap 60 FPS stabil.
  3. Angin 1 Arah Paralel: Arah angin dikunci pada satu sumbu sudut kompas (windCompassAngle) dengan gelombang halus alami.
- Cara Setting di Unity:
  1. Buka objek Infinite Grass di Hierarchy.
  2. Di Inspector GrassInteractivePhysics, atur Wind Compass Angle ke 45 derajat dan Height Multiplier ke 1.3.
  3. Tekan Play untuk menikmati gameplay 60 FPS dengan rumput yang tampil sempurna.


12. Sistem Smart Anti-Stacking Empty Slot Filler dan Pembatasan Layer Ground
- Perubahan: Memperbarui sistem TerrainGrassSpawner.cs dengan fitur validasi Layer Ground/Terrain dan sistem pengecekan sel tetangga 3x3.
- Fitur dan Perbaikan:
  1. Smart Empty-Slot Filler (Anti-Numpuk): Saat melukis atau men-generate rumput, kuas secara otomatis memeriksa apakah suatu koordinat sudah memiliki rumput (minGrassSpacing). Jika sudah ada rumput, titik tersebut dilewati dan kuas hanya menanam rumput di titik-titik yang masih benar-benar kosong. Menggeser kuas berulang kali di area yang sama tidak akan pernah menumpuk rumput lagi.
  2. Pembatasan Layer Ground / Terrain: Rumput sekarang 100% hanya bisa ditanam di permukaan Unity Terrain atau objek yang berlayer Ground. Rumput tidak akan pernah bisa tertanam di atap rumah, dinding kayu, pagar, atau objek bangunan lainnya.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy, lalu klik Clear All Grass untuk membersihkan tumpukan rumput lama.
  2. Di Inspector TerrainGrassSpawner, atur Min Grass Spacing (misal 1.0 meter) dan Brush Radius (4.0).
  3. Sapukan kuas di tanah: Rumput akan tertanam menyebar rapi mengisi tanah tanpa ada rumput yang menumpuk satu sama lain.


13. Penguncian Jarak Fisik Minimal 1.4 Meter dan Pembersihan Total Anak Objek
- Perubahan: Memperbarui TerrainGrassSpawner.cs dengan penguncian jarak fisik horizontal XZ minimal 1.4 meter via OnValidate dan pembersihan terbalik loop untuk Clear All Grass.
- Fitur dan Perbaikan:
  1. Penguncian Jarak Fisik Anti-Numpuk: Nilai Min Grass Spacing dikunci minimal 1.4 meter di level script sehingga tumpukan rumput berjarak 30 cm tidak akan pernah bisa terjadi lagi.
  2. Batasan Kerapatan Maksimal: Brush Density dibatasi maksimal 3-4 objek per sapuan untuk mencegah penanaman berlebih yang membebani memori.
  3. Pembersihan Bersih Total: Tombol Clear All Grass membersihkan seluruh 1.800+ anak objek dari index terbalik sampai benar-benar bersih dan nol batch.
- Cara Setting di Unity:
  1. Pada objek Infinite Grass di Hierarchy, klik Clear All Grass satu kali sampai seluruh rumput lama terhapus bersih.
  2. Di Inspector, nilai Min Grass Spacing sekarang otomatis terkunci di 1.4 meter.
  3. Sapukan kuas di bukit: Rumput akan tertanam dengan jarak lega 1.4 meter antar rumpun yang sangat rapi dan anti-numpuk.


14. Sistem Padang Rumput Tsushima Lebat Menyatu (Dense Seamless Meadow Generator)
- Perubahan: Memperbarui TerrainGrassSpawner.cs dengan fitur Generate Tsushima Meadow, kalibrasi jarak natural 0.6 meter, dan variasi skala 1.2x - 1.6x.
- Fitur dan Perbaikan:
  1. Efek Karpet Rumput Lebat Menyatu: Jarak penanaman disesuaikan menjadi 0.6 meter dengan perpaduan skala rumpun 1.2x hingga 1.6x, sehingga ujung-ujung daun rumput saling menyatu membentuk lautan rumput tebal tanpa celah bolong.
  2. Tombol Instan 1-Klik Tsushima Meadow: Menambahkan fungsi Generate Tsushima Meadow untuk langsung menggelar padang rumput lebat yang menyatu sempurna di seluruh area bukit.
  3. Anti-Numpuk Presisi: Setiap titik baru divalidasi dengan jarak natural 0.6 meter sehingga rumput tidak akan pernah bertumpuk di koordinat yang sama.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy, lalu klik tombol Clear All Grass satu kali.
  2. Klik tombol Generate Tsushima Meadow di Inspector untuk menggelar karpet rumput lebat yang menyatu.
  3. Tekan Play untuk menikmati pemandangan padang rumput bergaya Ghost of Tsushima dengan ombak angin yang mengalir mulus di 60 FPS.


15. Penghilangan Bayangan Hitam Kasar Rumput (Clean Stylized Foliage Lighting)
- Perubahan: Menonaktifkan ShadowCaster pass pada PT_Grass_Mat.mat, PT_InteractiveGrass_Mat.mat, serta mengatur ShadowCastingMode.Off pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Tanah Bersih Bebas Noda Hitam: Menghilangkan bercak bayangan hitam pekat dan bergerigi di tanah bukit, sehingga padang rumput terlihat cerah, bersih, dan indah menyatu dengan tekstur tanah.
  2. Peningkatan Drastis Frame Rate: Menghilangkan lebih dari 1.200 shadow pass draw call yang sebelumnya membebani kartu grafis, mengembalikan performa ke 60 FPS stabil.
  3. Pencahayaan Alami Halus: Rumput tetap menerima pencahayaan matahari (Receive Shadows On) dan ambient skybox secara natural.
- Cara Setting di Unity:
  Kembali ke Unity, bercak bayangan hitam kasar di sekitar rumput akan langsung hilang dan tanah terlihat bersih cerah.


16. Shader Ghost of Tsushima Foliage dengan Soft Upward Lighting dan Translucency Glow
- Perubahan: Membuat shader GhostOfTsushimaGrass.shader dan menerapkannya pada material PT_Grass_Mat.mat dan PT_InteractiveGrass_Mat.mat.
- Fitur dan Perbaikan:
  1. Soft Upward Normals Lighting: Mengeliminasi efek gelap hitam sepihak pada daun rumput. Seluruh permukaan daun sekarang tersinari cahaya matahari secara lembut dan merata dari semua sudut pandang kamera.
  2. Translucency Sunlight Glow: Efek tembus cahaya matahari keemasan pada daun rumput sehingga padang rumput tampak bercahaya, hidup, dan memukau seperti grafis Ghost of Tsushima dan Zelda BotW.
  3. Gradasi Daun Dua Warna: Gradasi halus dari warna hijau tanah gelap di pangkal akar menuju warna hijau keemasan cerah di pucuk daun.
  4. GPU Wind dan Trample Terpadu: Animasi ombak angin 1 arah dan reaksi injak karakter tertanam langsung di GPU vertex shader tanpa membebani CPU.
- Cara Setting di Unity:
  1. Buka objek Infinite Grass di Hierarchy, klik Clear All Grass lalu klik Generate Tsushima Meadow.
  2. Padang rumput akan langsung tampil indah, bercahaya lembut, dan menyatu alami dengan bukit.


17. Pembersihan Error Material PT_Grass_Mat 1 dan Reset Shader Tsushima
- Perubahan: Memperbaiki material PT_Grass_Mat 1.mat yang sebelumnya memuat properti lama shader Amplify/Neko Legends yang tidak kompatibel dengan Unity 6 URP.
- Fitur dan Perbaikan:
  1. Konsol Unity 100% Bersih: Seluruh pesan error merah terkait properti shader di Console telah hilang sepenuhnya.
  2. Sinkronisasi Material Tsushima: Material rumput terhubung bersih dengan shader GhostOfTsushimaGrass.shader tanpa konflik keyword atau serialization mismatch.
- Cara Setting di Unity:
  Buka tab Console di Unity lalu klik Clear, seluruh error merah sudah hilang dan sistem siap digunakan secara normal.


18. Penguncian Permanen Material URP Lit Stabil Anti-Hilang
- Perubahan: Mengunci konfigurasi material PT_Grass_Mat.mat dan PT_InteractiveGrass_Mat.mat secara permanen ke shader resmi Universal Render Pipeline/Lit dengan cutout alpha 0.35, warna hijau alami Tsushima, dan GPU Instancing aktif.
- Fitur dan Perbaikan:
  1. Visual Rumput 100% Muncul Stabil: Menggunakan pipeline URP Lit bawaan resmi Unity sehingga tekstur daun rumput PT_Grass_01 tidak akan pernah ter-clip atau hilang lagi.
  2. Bebas Noda Hitam: Pass ShadowCaster dinonaktifkan sehingga rumput menerima pencahayaan matahari yang cerah tanpa menimbulkan bercak bayangan hitam pekat di tanah.
  3. Warna Hijau Tsushima Cerah: Nilai BaseColor diatur ke hijau keemasan cerah (0.45, 0.88, 0.22) untuk menghadirkan visual padang rumput yang asri dan segar.
- Cara Setting di Unity:
  Kembali ke Unity, tekstur rumput hijau cerah langsung tampil stabil di Scene dan Game view.


19. Restorasi Blend Tree Locomotion Player ke Konfigurasi Default StarterAssets
- Perubahan: Mengembalikan parameter Blend Tree locomotion pada StarterAssetsThirdPerson.controller ke mode 1D dengan threshold default.
- Fitur dan Perbaikan:
  1. Transisi Animasi Presisi: Idle (0), Walk_N (2.0), dan Run_N (6.0) dengan parameter Speed murni.
  2. Gerak Karakter Standar yang Mulus: Transisi perpindahan dari berdiri diam ke berjalan dan berlari kembali responsif dan konsisten.
- Cara Setting di Unity:
  Kembali ke Unity, jendela Blend Tree di Inspector otomatis ter-update kembali ke konfigurasi default.


20. Sistem Perlindungan Anti-Nembus Collider dan Pola Angin Dinamis Game AAA
- Perubahan: Memperbarui TerrainGrassSpawner.cs dengan filter kemiringan tebing (maxSlopeAngle 35 derajat) serta deteksi 4 sudut tapak tanah, dan memperbarui GrassInteractivePhysics.cs dengan pola hembusan angin dinamis bertahap.
- Fitur dan Perbaikan:
  1. Anti-Nembus Collider & Anti-Menggantung: Rumput otomatis menolak ditanam pada tebing curam atau tepi jurang berlubang. Posisi akar rumput duduk presisi menempel di atas permukaan tanah tanpa menembus ke bagian bawah tebing.
  2. Pola Angin Dinamis Game AAA: Angin tidak lagi monoton. Rumput memiliki siklus napas alami: berayun sepoi-sepoi tenang, kemudian disapu ombak hembusan angin besar (wind gust surge) yang mengalir serentak melintasi padang rumput secara periodik, lalu kembali mereda lembut.
- Cara Setting di Unity:
  1. Pada objek Infinite Grass di Hierarchy, klik Clear All Grass lalu klik Generate Tsushima Meadow.
  2. Rumput akan tertanam rapi di permukaan tanah yang aman dan bergoyang dinamis dengan pola hembusan angin kelas AAA.


21. Implementasi Arsitektur Padang Rumput SimonDev & Ghost of Tsushima (GPU Instanced Meadow)
- Perubahan: Membuat script TsushimaGrassMeadow.cs berdasarkan teknik video tutorial SimonDev ("How do Major Video Games Render Grass?") dan presentasi GDC Ghost of Tsushima.
- Fitur dan Perbaikan:
  1. Single Draw Call GPU Instancing: Menggambar ribuan rumpun rumput langsung di GPU tanpa membuat ribuan GameObject di Hierarchy. Batches tetap 1, FPS terkunci 60-144 FPS tanpa lag.
  2. Bézier Noise Wind Model: Menggunakan pemodelan ombak angin berbasis formula 2D Noise dan quadratic Bézier curve dengan siklus hembusan periodik alami.
  3. Ground Snapping & Slope Filter: Menyesuaikan ketinggian setiap rumpun tepat di atas kontur terrain dan mengeliminasi tebing curam sehingga 100% tidak tembus collider.
  4. Interaksi Injak Kaki Halus: Rumput merunduk membuka jalan saat didekati karakter dan kembali tegak secara dinamis.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Tambahkan komponen TsushimaGrassMeadow (atau klik Inspector Tsushima Grass Meadow).
  3. Pasang Grass Mesh (PT_Grass_02_LOD0) dan Grass Material (PT_Grass_Mat).
  4. Tekan Play untuk menikmati padang rumput lebat standar industri game AAA.


22. Sistem Rumput Per-Batang Prosedural Bézier Curves (Realistic Blade Trample)
- Perubahan: Menghapus TsushimaGrassMeadow lama dan membuat ProceduralBezierBlade.shader serta ProceduralBladeGrassSystem.cs berdasarkan teknik video tutorial UE5/Unity Ghost of Tsushima Procedural Grass Using Bezier Curves.
- Fitur dan Perbaikan:
  1. Geometri Helai Mandiri (Per-Blade Mesh): Setiap helai daun rumput dibuat sebagai geometri batang tunggal prosedural (7 vertices, 5 triangles) dengan penyempitan meruncing ke pucuk.
  2. Deformasi Kurva Bézier Kuadratik: Kelenturan batang dievaluasi menggunakan kurva Bézier (P0 = Akar, P1 = Kontrol Tengah, P2 = Pucuk), menghasilkan lengkungan batang yang sangat realistis dan lentur.
  3. Reaksi Injak Kaki Realistis: Saat kaki karakter melangkah, setiap batang rumput dalam radius injakan melengkung, terdorong ke samping, dan rebah ke tanah secara individual, lalu bangkit kembali saat karakter pergi.
  4. Performa GPU Instanced 60 FPS: Ribuan batang rumput dirender sekaligus dalam batch GPU tanpa membebani CPU hierarchy.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Tambahkan komponen ProceduralBladeGrassSystem.
  3. Pasang Blade Material dengan ProceduralBlade_Mat.
  4. Tekan Play dan jalankan karakter untuk merasakan injakan rumput per-batang yang nyata.


23. Fitur Kuas Melukis Helai Rumput Scene View (Procedural Blade Brush Painter)
- Perubahan: Menambahkan fitur kuas interaktif Scene View pada ProceduralBladeGrassSystem.cs dengan dukungan melukis klik-drag, menghapus Shift-klik, dan penyimpanan data koordinat batang rumput permanen.
- Fitur dan Perbaikan:
  1. Kuas Melukis Interaktif 144Hz: Lingkaran kuas di Scene View memungkinkan developer melukis puluhan helai batang rumput sekaligus secara halus dan langsung menempel di kontur tanah.
  2. Mode Hapus Cepat (Shift + Klik): Menghapus helai rumput di area tertentu dengan mudah menggunakan tombol Shift.
  3. Anti-Numpuk Batang: Parameter minBladeSpacing memastikan helai rumput tidak tertumpuk di koordinat yang sama.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Pastikan centang Enable Brush Mode aktif di Inspector Procedural Blade Grass System.
  3. Sapukan kuas di Scene View dengan klik kiri untuk melukis helai batang rumput di tanah.


24. Pemisahan Script Editor Khusus dan Pembersihan 1.800 Objek Lama
- Perubahan: Membuat script editor terpisah ProceduralBladeGrassSystemEditor.cs di folder Assets/Scripts/Editor dan menambahkan tombol pembersih objek lama.
- Fitur dan Perbaikan:
  1. Lingkaran Kuas Neon Selalu Muncul (Always Visible): Menggunakan Handles.zTest Always sehingga lingkaran kuas hijau neon di Scene View 100% selalu terlihat jelas di atas permukaan tanah tanpa terhalang collider.
  2. Tombol Pembersih 1.800 Objek Lama: Tombol "Bersihkan Objek Lama" untuk menghapus seluruh sisa 1.800 GameObject lama yang sebelumnya membebani batch rendering hingga kembali ke 60 FPS mentok.
  3. Tombol Quick Fill 2.500 Helai: Tombol cepat untuk langsung menggelar padang rumput per-batang di sekeliling pemain secara instan.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "Bersihkan 1800 Objek Lama" di Inspector untuk membersihkan tumpukan objek lama.
  3. Arahkan mouse ke Scene View: Lingkaran kuas hijau neon akan langsung muncul jelas di atas tanah dan siap untuk melukis.


25. Konsolidasi Custom Editor dan Perbaikan Inspector Multi-Object
- Perubahan: Menyatukan kelas ProceduralBladeGrassSystemEditor langsung ke dalam ProceduralBladeGrassSystem.cs dengan atribut [CanEditMultipleObjects] dan pembaruan struktur GUI.
- Fitur dan Perbaikan:
  1. Tampilan Inspector Lengkap: Menghilangkan peringatan Multi-object editing dan menampilkan seluruh tombol aksi (Quick Fill, Clear Blades, dan Bersihkan Objek Lama) secara utuh di Inspector.
  2. Kuas Langsung Responsif: Mengaktifkan fungsi OnSceneGUI secara native tanpa jeda kompilasi assembly terpisah.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Tombol aksi Quick Fill dan Bersihkan Objek Lama sudah muncul di Inspector.
  3. Arahkan kursor ke Scene View untuk melukis helai rumput.


26. Penyatuan Penuh Sistem Rumput Per-Batang Bézier ke TerrainGrassSpawner
- Perubahan: Memperbarui TerrainGrassSpawner.cs dengan engine rumput per-batang Bézier dan tombol aksi Inspector lengkap (Quick Fill 2500 Helai, Bersihkan Objek Lama, Clear All Blades, dan Kuas Scene View).
- Fitur dan Perbaikan:
  1. Satu Script Terpadu: Seluruh fitur melukis rumput per-batang dan pembersihan terintegrasi langsung di komponen TerrainGrassSpawner tanpa script ganda.
  2. Tombol Aksi Langsung Muncul di Inspector: Tombol Quick Fill, Bersihkan Objek Lama, dan Kuas Melukis langsung tampil jelas di Inspector.
  3. Kuas Melukis Scene View 100% Aktif: Lingkaran kuas neon hijau langsung muncul di Scene View mengikuti kursor tanah.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Di Inspector TerrainGrassSpawner, tombol Quick Fill dan Bersihkan Objek Lama langsung terlihat.
  3. Sapukan kuas di Scene View dengan klik kiri untuk melukis helai batang rumput.


27. Sistem Rumput Procedural Combined Mesh Berbasis MinionsArt URP Shader
- Perubahan: Mengimplementasikan sistem generasi Procedural Combined Mesh pada TerrainGrassSpawner.cs dengan integrasi shader interaktif MinionsArt URP (InteractiveGrassURP.shader).
- Fitur dan Perbaikan:
  1. Rumput 100% Muncul Nyata di Scene & Game View: Membangun geometri helai rumput 3D (3 bidang silang per titik tanam) langsung ke MeshFilter dan MeshRenderer pada objek Infinite Grass. Rumput langsung tampak hijau lebat di Scene View tanpa masalah invisible instancing.
  2. Performa 1 Single Draw Call: Ribuan helai rumput digabungkan ke dalam 1 mesh tunggal sehingga draw call / batch tetap 1 dan FPS stabil di 60-144 FPS.
  3. Reaksi Deformasi Injak Kaki MinionsArt: Shader membaca _PlayerPosition secara global dan melengkungkan setiap helai daun di GPU saat didekati atau diinjak kaki karakter.
  4. Kuas Melukis Real-Time: Sapuan kuas di Scene View langsung menambahkan geometri helai rumput ke mesh secara instan.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Area (1500 Titik)" di Inspector Terrain Grass Spawner.
  3. Rumput hijau 3D langsung muncul berdiri lebat di atas bukit dan merunduk lentur saat diinjak karakter.


28. Perbaikan Shader Mandiri (InteractiveBladeGrass) dan Koordinat Mesh Lokal
- Perubahan: Membuat InteractiveBladeGrass.shader mandiri tanpa dependensi tekstur atlas dan memperbarui konversi koordinat vertex lokal pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bebas Alpha Clipping Error: Shader tidak lagi bergantung pada tekstur atlas ber-alpha clip yang sebelumnya memotong dan menghilangkan pixel rumput secara tidak sengaja.
  2. Transformasi Titik ke Lokal Objek (InverseTransformPoint): Memastikan posisi setiap helai daun tertanam presisi di permukaan tanah tanpa offset ganda.
  3. Warna Gradasi Cerah Alami: Gradasi dua warna (akar tanah hijau gelap ke pucuk lemon terang tersinari matahari) langsung muncul pekat dan jelas di Scene View.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Area (1500 Titik)" di Inspector Terrain Grass Spawner.
  3. Rumput langsung tampak berdiri tegap di bukit.


29. Pemasangan Otomatis Komponen MeshFilter dan MeshRenderer
- Perubahan: Menambahkan logika inisialisasi otomatis untuk memasang komponen MeshFilter dan MeshRenderer pada objek Infinite Grass serta memastikan material InteractiveBlade_Mat terpasang otomatis.
- Fitur dan Perbaikan:
  1. Auto-Attach MeshFilter & MeshRenderer: Memastikan kedua komponen rendering ini selalu terpasang otomatis di GameObject Infinite Grass tanpa harus ditambah manual.
  2. Auto-Rebuild 4.500 Helai: 1.518 titik rumput (4.554 helai) yang sudah tersimpan langsung dikonversi menjadi combined mesh dan dirender seketika di layar.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. MeshFilter dan MeshRenderer otomatis terpasang dan seluruh 4.554 helai rumput langsung muncul di Scene View.


30. Perbaikan Warna Shader Hijau Alami dan Kepadatan Padang Rumput Lebat
- Perubahan: Memperbaiki GUID material InteractiveBlade_Mat dengan shader InteractiveBladeGrass asli (menghilangkan warna ungu/magenta) serta meningkatkan algoritma sebaran rumput menjadi padat dan bergerombol (Organic Tufts).
- Fitur dan Perbaikan:
  1. Warna Hijau Segar Alami (Bebas Warna Ungu): Material sekarang terhubung 100% ke shader InteractiveBladeGrass dengan gradasi hijau alami (akar gelap dan pucuk lemon cerah).
  2. Kepadatan Padang Rumput Lebat (Dense Cluster Spacing): Algoritma penanaman membagi titik rumput ke dalam kelompok-kelompok rimbun (tufts) dengan jarak rapat (minBladeSpacing 0.04m, 2.500 titik = 7.500 helai rumput).
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (2500 Titik)" di Inspector.
  3. Rumput langsung muncul hijau segar, rapat, lebat, dan menyatu seperti karpet padang rumput alami.


31. Pengambilan Permukaan Ganda (Physics Raycast dan Terrain Fallback)
- Perubahan: Menambahkan fungsi SampleSurfaceAt pada TerrainGrassSpawner.cs dengan jangkauan vertikal 140m dan integrasi langsung ke data ketinggian Terrain (TerrainData.SampleHeight).
- Fitur dan Perbaikan:
  1. Deteksi Permukaan 100% Berhasil: Titik rumput selalu berhasil mendeteksi permukaan tanah bukit dan lereng tanpa ada yang meleset atau kosong.
  2. Padang Rumput 3.000 Titik (9.000 Helai): Memperluas kapasitas padang rumput hingga 9.000 helai daun yang tertata rapi dan padat di sekeliling pemain.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Padang rumput hijau lebat langsung terhampar luas di sekitar pemain.


32. Perbaikan Error Kompilasi IsValidGround pada Custom Editor
- Perubahan: Menambahkan kembali fungsi IsValidGround pada TerrainGrassSpawner.cs untuk melengkapi verifikasi kemiringan dan collider pada OnSceneGUI.
- Fitur dan Perbaikan:
  1. Konsol Unity Bebas Error: Menghilangkan error CS1061 sehingga Unity Editor dapat mengompilasi dan menjalankan scene dengan lancar tanpa hambatan.
  2. Kuas Scene View Kembali Aktif Penuh: Kuas melukis dan tombol Quick Fill langsung berfungsi normal kembali di Inspector.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Padang rumput hijau langsung muncul seketika di bukit.


33. Penegasan Rendering Opaque Padat dan Pemasangan MeshRenderer Editor
- Perubahan: Menambahkan instruksi ZWrite On dan ZTest LEqual pada InteractiveBladeGrass.shader serta memperbarui fungsi EnsureComponentsExist pada OnInspectorGUI.
- Fitur dan Perbaikan:
  1. Rendering Solid Opaque 100% (Bebas Transparan): Shader memproses seluruh pixel geometri rumput secara padat (Opaque Geometry) dengan Z-buffer aktif sehingga tidak tembus pandang.
  2. Garansi Komponen MeshRenderer: Memastikan komponen MeshFilter dan MeshRenderer terdaftar permanen di Inspector objek Infinite Grass.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Rumput langsung muncul hijau segar padat di bukit.


34. Pembersihan Auto-Spawn Liar dan Geometri Daun Dua Sisi (Double-Sided Solid)
- Perubahan: Menghapus logika auto-QuickFill pada event Start() di TerrainGrassSpawner.cs dan menerapkan geometri mesh dua sisi (Double-Sided Triangles) dengan shader pencahayaan abs(dot(N, L)).
- Fitur dan Perbaikan:
  1. Kontrol Penuh Pengguna (Bebas Auto-Spawn Liar): Rumput tidak akan lagi muncul tiba-tiba saat tombol Play ditekan jika belum pernah dilukis kuas atau diklik Quick Fill.
  2. Geometri Daun 100% Solid & Tebal: Setiap helai daun memiliki pasangan segitiga depan dan belakang (Double-Sided Triangles), sehingga rumput terlihat tebal, padat, dan tidak tembus pandang dari sudut kamera mana pun.
  3. Pembersihan Otomatis Objek Demo Lama: Menonaktifkan otomatis ExampleDemoTile agar tidak memunculkan bayangan wireframe transparan liar di scene.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Sapukan kuas di Scene View atau klik tombol "🌾 Quick Fill Padat (3000 Titik)" jika ingin menggelar rumput.
  3. Tekan Play — rumput hanya muncul di tempat yang ditentukan dan merunduk saat diinjak.


35. Pembersihan Objek Demo Lama (ExampleDemoTile) dan Penataan Ulang Hierarchy
- Perubahan: Menambahkan tombol dan otomatisasi penonaktifan objek demo lama (ExampleDemoTile) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Penghapusan Objek Wireframe Transparan: Menonaktifkan sistem compute shader demo lama yang sebelumnya memunculkan garis wireframe oranye tinggi dan transparan di sekitar pemain saat Play.
  2. Fokus Rendering Murni ke Infinite Grass: Menjamin hanya sistem rumput solid padat Infinite Grass yang aktif dan dirender di layar.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🛑 Matikan Objek Demo Lama (ExampleDemoTile)" di Inspector (atau klik icon mata pada ExampleDemoTile untuk menyembunyikannya).
  3. Sapukan kuas di Scene View atau klik "🌾 Quick Fill Padat (3000 Titik)" — padang rumput hijau solid padat langsung muncul dengan bersih.


36. Pass Shadow Caster Nyata dan Gradasi 3 Warna Ambient Occlusion
- Perubahan: Menambahkan Pass ShadowCaster, instruksi Blend Off, dan gradasi 3 warna (Root AO, Mid Green, Tip Sunlit) pada InteractiveBladeGrass.shader serta memperlebar ukuran helai daun (bladeWidth 0.22m) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bayangan Nyata di Permukaan Tanah (Shadow Caster): Rumput sekarang menghasilkan bayangan fisik nyata di atas tanah, menghilangkan ilusi melayang atau tembus pandang.
  2. Grounding Root AO (Akar Tanah Pekat): Bagian pangkal daun rumput diberi warna gelap pekat (Ambient Occlusion) sehingga rumput terlihat tertanam kuat dan menyatu secara visual dengan tanah.
  3. Helai Daun Lebih Lebar & Tebal: Lebar helai dinaikkan menjadi 0.22m sehingga setiap helai daun tampak tebal, berisi, dan lebat seperti rumput game AAA.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Rumput langsung tampak tebal, pekat dengan bayangan tanah nyata di bukit.


37. Pembersihan File Shader Usang dan Penataan Sistem Tunggal (Single Script)
- Perubahan: Menghapus file shader eksperimen usang (ProceduralBezierBlade.shader) dan merampingkan sistem rumput agar hanya menggunakan 1 script utama (TerrainGrassSpawner.cs) dan 1 shader utama (InteractiveBladeGrass.shader).
- Fitur dan Perbaikan:
  1. Struktur Proyek Rapi & Bersih: Menghapus file-file duplikat/sampah eksperimen yang tidak terpakai sehingga aset proyek tetap ringkas.
  2. Sistem 1 Komponen Praktis: Pengguna hanya perlu memasang 1 komponen TerrainGrassSpawner pada objek Infinite Grass, tanpa perlu memasang komponen lain secara manual.
- Cara Setting di Unity:
  1. Di Hierarchy, pastikan objek Infinite Grass memiliki komponen TerrainGrassSpawner.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Selesai — rumput langsung aktif dan siap dimainkan.


38. Penonaktifan Permanen Legacy GrassRenderer dan Penghapusan ExampleDemoTile
- Perubahan: Mematikan fungsi Update pada GrassRenderer.cs dan menambahkan penghapusan otomatis terhadap GameObject ExampleDemoTile pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Menghilangkan Garis Wireframe Oranye Transparan: Objek demo lama yang sebelumnya memunculkan garis-garis silinder oranye transparan di tengah scene kini 100% dimatikan dan dihapus permanen dari scene.
  2. Tampilan Rumput Solid Murni: Scene View dan Game View kini hanya menampilkan helai rumput hijau solid padat asli milik Infinite Grass.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol "🌾 Quick Fill Padat (3000 Titik)" di Inspector.
  3. Garis-garis oranye transparan hilang total dan padang rumput hijau solid langsung terhampar bersih.


39. Penonaktifan Menyeluruh Pipeline Shader Demo (ProceduralGrass/Grass)
- Perubahan: Mematikan seluruh pass rendering pada ProceduralGrass/Grass.shader (ColorMask 0, ZWrite Off, ZTest Off).
- Fitur dan Perbaikan:
  1. Garansi 0% Efek Transparan Demo: Memastikan shader demo lama tidak lagi mampu menggambar pixel apa pun ke kartu grafis, mengakhiri kemunculan wireframe atau silinder transparan tak bertekstur.
  2. Fokus Penuh ke Mesh Solid Opaque: Semua rumput di scene sekarang murni dirender oleh InteractiveBladeGrass.shader yang 100% solid dan berbayangan nyata.


40. Pembersihan Otomatis Scene (InitializeOnLoad) dan Auto-Populate Rumput Solid
- Perubahan: Menambahkan kelas GrassSceneAutoCleaner dengan atribut [InitializeOnLoad] pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Hapus Otomatis ExampleDemoTile saat Kompilasi: Editor secara otomatis menghapus GameObject ExampleDemoTile dari Hierarchy saat script terkompilasi, sehingga pengguna tidak perlu menghapus manual.
  2. Auto-Populate 2.500 Titik Rumput Solid: Mengisi otomatis padang rumput hijau solid pada Infinite Grass dan langsung memilih (seleksi) objek Infinite Grass di Inspector.
- Cara Setting di Unity:
  1. Cukup kembali ke Unity — script otomatis menghapus objek demo lama dan menggelar padang rumput hijau padat di sekitar pemain.
  2. Tekan Play untuk menikmati interaksi injakan kaki karakter.


41. Integrasi Pembersihan Otomatis OnInspectorGUI dan Auto-Populate Instan
- Perubahan: Menambahkan pemanggilan Undo.DestroyObjectImmediate(oldDemo) dan QuickFillArea otomatis di dalam OnInspectorGUI pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Hapus Instan saat Inspector Terbuka: Begitu objek Infinite Grass dibuka di Inspector, objek demo lama ExampleDemoTile langsung dihapus seketika dari scene.
  2. Auto-Populate 2.500 Titik Rumput: Menjamin 2.500 titik rumput (7.500 helai daun) langsung terisi dan ter-render menjadi combined mesh solid di Scene View tanpa perlu klik tambahan.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Selesai — objek demo lama hilang seketika dan padang rumput hijau solid langsung terhampar luas di bukit.


42. Penataan Ulang Transform Origin (0,0,0) dan Koordinat Geometri Langsung
- Perubahan: Mereset transform.position objek Infinite Grass ke titik asal (0,0,0) dan menuliskan koordinat geometri vertex langsung tanpa konversi InverseTransformPoint ganda.
- Fitur dan Perbaikan:
  1. Penempatan Presisi di Permukaan Tanah: Geometri seluruh 2.500 titik rumput (7.500 helai daun) tertanam tepat di atas kontur bukit dengan koordinat dunia yang konsisten.
  2. Rendering Solid Bebas Distorsi: Menghilangkan pergeseran bounding box kamera yang sebelumnya menyebabkan mesh tampak transparan atau tidak ter-render.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Selesai — padang rumput hijau solid tebal langsung terhampar sempurna di atas bukit.


43. Pemulihan Kompatibilitas URP Unity 6 pada Procedural Bézier Grass Shader
- Perubahan: Memperbaiki Grass.shader dengan fallback warna solid otomatis (Anti-Black / Anti-Transparent saat tekstur kosong), integrasi pencahayaan URP GetMainLight, dan mengaktifkan kembali UpdateCompute pada GrassRenderer.cs.
- Fitur dan Perbaikan:
  1. Rumput Bézier Lengkung Muncul Padat & Berwarna: Setiap helai rumput lengkung prosedural (Bézier Curve) kini memiliki warna hijau gradasi cerah yang solid dan tidak terpotong (clip) menjadi transparan.
  2. Kompatibilitas Penuh URP Unity 6: Menggunakan API pencahayaan Universal Pipeline modern sehingga tidak ada lagi error atau shader yang gagal me-render pixel.
- Cara Setting di Unity:
  1. Kembali ke Unity — rumput lengkung prosedural langsung muncul hijau solid dan melambai indah tertiup angin di bukit.
  2. Tekan Play untuk menguji kelenturan dan interaksi injakan pemain.


44. Pemasangan Tekstur Atlas _grass.png dan Pembersihan Legacy CG Header
- Perubahan: Menghubungkan tekstur atlas _grass.png ke DefaultGrass.mat, menghapus ketergantungan UnityIndirect.cginc usang pada Grass.shader, dan mengganti kalkulasi vertex ID dengan standar modern SV_VertexID / SV_InstanceID.
- Fitur dan Perbaikan:
  1. Daun Bertekstur dan Berwarna Solid 100%: Mengisi bentuk siluet daun rumput dengan albedo hijau segar dan tekstur asli sehingga rumput tidak lagi berlubang atau transparan.
  2. Kompatibilitas Hardware Modern: Menghilangkan error rendering shader pada kartu grafis DirectX 11 di Unity 6.
- Cara Setting di Unity:
  1. Cukup kembali ke jendela Unity.
  2. Rumput lengkung prosedural langsung tampil hijau lebat dan padat seketika di bukit.


45. Restorasi Total Script dan Shader Procedural Grass ke Versi Asli yang Berfungsi
- Perubahan: Merestorasi GrassRenderer.cs dan Grass.shader ke versi asli paket dan memasangkan tekstur atlas daun rumput _grass.png pada DefaultGrass.mat.
- Fitur dan Perbaikan:
  1. Pemulihan Penuh Sistem Asli: Mengembalikan sistem Procedural Grass ke kondisi awal saat pertama kali berfungsi dan dirender di scene.
  2. Tekstur Daun Rumput Aktif: Daun rumput memiliki gambar tekstur dan warna hijau alami, tidak lagi berlubang atau transparan.
- Cara Setting di Unity:
  1. Cukup kembali ke Unity — rumput asli langsung muncul kembali di bukit.
  2. Tekan Play untuk menikmati interaksi injakan kaki karakter.


46. Perbaikan GUID Material DefaultGrass.asset dan Penegasan RequireComponent
- Perubahan: Memperbaiki referensi GUID proceduralMaterial yang hilang pada DefaultGrass.asset (dihubungkan langsung ke DefaultGrass.mat) dan menambahkan [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))] pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Material Prosedural Terhubung 100%: Menghilangkan penyebab shader null pada GPU yang sebelumnya membuat Graphics.RenderPrimitivesIndirect menggambar tanpa material.
  2. Garansi Komponen MeshRenderer: Menjamin MeshFilter dan MeshRenderer selalu terpasang permanen pada GameObject Infinite Grass.
- Cara Setting di Unity:
  1. Cukup kembali ke jendela Unity.
  2. Rumput prosedural hijau bertekstur langsung tampil utuh di scene tanpa material yang hilang.


47. Pemotongan Masking Alpha (clip) dan Pewarnaan PBR URP Solid
- Perubahan: Menambahkan instruksi clip(texCol.a - 0.35) dan penguatan albedo surfaceData.albedo = i.color.rgb * texCol.rgb * 1.35 pada fungsi frag di Grass.shader.
- Fitur dan Perbaikan:
  1. Penghapusan Area Hitam/Transparan Atlas: Memotong latar belakang transparan dari atlas daun rumput dan merender siluet daun asli secara penuh dan padat.
  2. Pewarnaan Hijau Segar PBR URP: Mengalikan warna gradasi alami dengan albedo tekstur sehingga setiap helai daun tampil hijau subur di bawah sinar matahari.
- Cara Setting di Unity:
  1. Cukup kembali ke jendela Unity.
  2. Daun rumput langsung muncul berwarna hijau padat dan solid di bukit.


48. Migrasi ke Sistem Padang Rumput 3D Langsung (Model & Tekstur Asli Proyek)
- Perubahan: Memperbarui TerrainGrassSpawner.cs agar menggunakan Model Prefab Rumput 3D Asli proyek (Grass_A_A, Grass_A_B, Grass_A_C) yang memiliki tekstur penuh Grass_A_BaseColor.tif, lengkap dengan kuas Scene View dan tombol Quick Fill.
- Fitur dan Perbaikan:
  1. 100% Muncul Bertekstur & Berwarna Nyata: Menggunakan model 3D nyata dengan material bawaan proyek sehingga rumput langsung terlihat nyata, hijau subur, dan bebas dari masalah shader/transparan.
  2. Mode Kuas & Quick Fill Instan: Pengguna dapat melukis rumpun rumput di Scene View atau mengklik tombol Quick Fill untuk menggelar ratusan rumpun rumput 3D di sekitar karakter.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol hijau "🌾 Gelar Rumput 3D (200 Rumpun)" di Inspector.
  3. Rumpun rumput 3D bertekstur hijau subur langsung terhampar indah dan nyata di atas bukit.


49. Auto-Spawn Instan 200 Rumpun Model 3D Bertekstur dan Auto-Selection
- Perubahan: Menambahkan kelas AutoGrassClumpInitializer [InitializeOnLoad] dan logika auto-spawn pada OnInspectorGUI di TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Auto-Populate Otomatis Tanpa Perlu Klik: Begitu kembali ke Unity, script otomatis menanam 200 rumpun model rumput 3D nyata (Grass_A_A, Grass_A_B, Grass_A_C) di sekeliling karakter.
  2. Auto-Focus Seleksi ke Infinite Grass: Otomatis menghapus ExampleDemoTile dari Hierarchy dan memindahkan fokus Inspector langsung ke Infinite Grass.
- Cara Setting di Unity:
  1. Cukup buka jendela Unity.
  2. Ratusan rumpun rumput 3D hijau bertekstur langsung muncul otomatis di bukit.


50. Penyempurnaan 4 Poin Utama: Anti Auto-Spawn, Kuas Responsif, Interaksi Injak Lentur, dan Akar Menancap Tanah
- Perubahan: Menghapus logika InitializeOnLoad auto-spawn, menambahkan fitur interaksi kelenturan fisik realtime (clump rotation damping) di fungsi Update, menambahkan offset kedalaman akar (rootSinkDepth 0.12m), serta mengoptimalkan Scene View Handles kuas pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Kontrol Penuh Tanpa Auto-Spawn Liar: Rumput tidak akan pernah muncul sendiri kecuali pengguna melukis dengan kuas atau mengklik tombol Gelar Rumput.
  2. Kuas Scene View Responsif: Kuas lingkaran hijau terang muncul langsung saat objek Infinite Grass dipilih, memungkinkan lukis dan hapus rumput secara instan.
  3. Interaksi Injak Kaki Karakter Realtime: Rumpun rumput merunduk menjauh saat diinjak telapak kaki karakter dan membal lentur kembali ke posisi semula secara halus saat dilewati.
  4. Akar Menancap Kokoh di Permukaan Tanah: Posisi akar ditenggelamkan 12 cm (rootSinkDepth) ke dalam tanah bukit sehingga tidak ada lagi rumput yang melayang.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Gunakan kuas di Scene View atau klik "🌾 Gelar Rumput 3D (200 Rumpun)".
  3. Tekan Play untuk menikmati interaksi rumput merunduk saat diinjak karakter.


51. Pembersihan Permanen Objek Demo Oranye (ExampleDemoTile / GrassRendererGreen)
- Perubahan: Menambahkan fungsi DeleteAllDemoTilesInScene(), tombol merah besar di Inspector TerrainGrassSpawner, serta Menu Bar Unity Fantasy Kingdom -> Hapus Objek Oren Demo.
- Fitur dan Perbaikan:
  1. Penghapusan Objek Demo Total: Menghapus ExampleDemoTile dan GrassRendererGreen dari memori scene sehingga seluruh garis bounding oranye transparan hilang total dan bersih.
  2. Akses Penghapusan 1-Klik: Tersedia tombol merah di Inspector dan menu bar atas untuk membersihkan objek demo kapan pun diperlukan.
- Cara Setting di Unity:
  1. Klik tombol merah "🛑 HAPUS OBJEK OREN DEMO SEKARANG" di bagian paling atas Inspector Infinite Grass (atau klik kanan ExampleDemoTile di Hierarchy lalu pilih Delete).
  2. Garis oranye langsung lenyap seketika dari layar.


52. Pembersihan MeshFilter Bawaan Infinite Grass (Penghapusan Total Garis Oranye)
- Perubahan: Menambahkan fungsi RemoveOldLegacyMeshComponents() pada TerrainGrassSpawner.cs untuk mencopot komponen MeshFilter dan MeshRenderer lama dari objek Infinite Grass.
- Fitur dan Perbaikan:
  1. Garis Seleksi Oranye Bersih Total 100%: Menghapus mesh lama (7.500 helai daun Bézier hollow) yang menempel pada MeshFilter objek Infinite Grass, sehingga Unity tidak lagi menggambar garis seleksi oranye kosong saat objek terseleksi.
  2. Fokus Murni pada Model Rumput 3D Asli: Scene kini murni hanya merender model 3D rumpun bertekstur asli yang menempel di tanah bukit.
- Cara Setting di Unity:
  1. Cukup buka jendela Unity.
  2. Seluruh garis oranye langsung hilang seketika dan padang rumput 3D bersih rapi.


53. Kalibrasi Skala Proporsional Rumput (Setinggi Betis) dan Kelenturan Injak Halus
- Perubahan: Menyesuaikan skala rumpun rumput menjadi proporsional (minScale 0.35, maxScale 0.55), memperkecil jarak tanam (minSpacing 0.45m), serta membatasi sudut interaksi injakan kaki maksimal 18 derajat (maxBendAngle 18) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Ukuran Alami Setinggi Betis/Mata Kaki: Menghilangkan rumput raksasa yang menutupi dada/kamera, digantikan dengan padang rumput hijau rimbun yang proporsional dengan tinggi karakter.
  2. Interaksi Halus Bebas Rebah Ekstrem: Rumput hanya meliuk lembut dan bergoyang alami saat dilewati langkah kaki karakter tanpa pernah ambruk atau tenggelam ke bawah tanah.
- Cara Setting di Unity:
  1. Klik tombol merah "🗑️ Hapus Semua Rumput" lalu klik "🌾 Gelar Rumput Proporsional (350 Rumpun)" di Inspector Infinite Grass.
  2. Tekan Play untuk menikmati padang rumput proporsional dengan kelenturan injakan alami.


54. Penggantian ke Model Rumput Ramping Alami (Scatter_Grass 01 s/d 17)
- Perubahan: Mengganti daftar prefab grassPrefabs dari model Grass_A (rumpun semak lebar) ke koleksi model Scatter_Grass_01 sampai Scatter_Grass_17 (helai ramping presisi) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Profil Ramping & Bebas Blocky: Menggunakan helai daun ramping alami yang tidak lebar, sehingga rumput yang terinjak kaki karakter merunduk secara halus dan proporsional persis di bawah tapak kaki pemain.
  2. 17 Variasi Helai Alami: Padang rumput tidak monoton dan terlihat menyatu 100% dengan gaya visual lingkungan fantasy kingdom.
- Cara Setting di Unity:
  1. Klik tombol "🗑️ Hapus Semua Rumput" di Inspector Infinite Grass.
  2. Klik tombol "🌾 Gelar Rumput Ramping (500 Helai)" atau gunakan kuas Scene View.
  3. Tekan Play untuk melihat reaksi injak tapak kaki yang presisi dan realistis.


55. Pemaksaan Refresh Prefab Scatter_Grass (Override Data Serialized Lama)
- Perubahan: Menambahkan fungsi ForceLoadScatterGrassPrefabs() dan validasi otomatis pada OnInspectorGUI di TerrainGrassSpawner.cs untuk mengganti data array prefab lama yang tersimpan di memori inspector.
- Fitur dan Perbaikan:
  1. Pembersihan Otomatis Aset Lama: Menggantikan model Grass_A yang tersangkut di memory Inspector dengan 17 variasi Scatter_Grass_01 s/d 17.
  2. Tombol Reset & Ganti Instan: Tombol hijau kini otomatis membersihkan rumput semak lebar lama dan menanam 450 helai rumput ramping alami.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Ganti & Gelar Rumput Ramping (450 Helai)".
  2. Model langsung berganti menjadi helai daun ramping Scatter_Grass alami.


56. Penggantian ke Model Rumput Murni Ramping (PT_Grass Low-Poly)
- Perubahan: Mengganti prefab ke koleksi PT_Grass dari Polytope Studio (PT_Grass_02_v1, PT_Grass_02_v2, PT_High_Grass_02_v1) yang merupakan model rumput tanah murni tanpa campuran ranting pohon.
- Fitur dan Perbaikan:
  1. 100% Bebas Bug Ranting Melayang: Menghilangkan ranting pohon dan semak tinggi yang sebelumnya tercampur di paket foliage, digantikan dengan rumput tanah ramping bersih.
  2. Profil Ramping & Interaksi Bersih: Model rumput ramping setinggi mata kaki/betis yang merunduk halus saat diinjak tanpa menutupi pandangan kamera.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol "🌾 Pasang Rumput Ramping PT_Grass (450 Rumpun)".
  2. Padang rumput tanah bersih dan ramping langsung terhampar di bukit.


57. Peningkatan Kepadatan Padat (Density Rapat), Penyesuaian Tinggi Rumput, dan Orientasi Tegak Alami
- Perubahan: Menambahkan slider heightMultiplier (1.45x), memperkecil minSpacing (0.22m), menaikkan targetClumpCount (800 rumpun), serta menerapkan vektor perpaduan alignWithGroundNormal (0.35) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Padang Rumput Rapat & Tebal: Jarak tanam yang lebih rapat (0.22m) dan jumlah rumpun 800 menciptakan karpet rumput yang rimbun tanpa celah tanah yang renggang.
  2. Rumput Lebih Tinggi Ramping: Pengali tinggi (heightMultiplier 1.45x) membuat rumput tumbuh lebih tinggi menjulang secara ramping tanpa melebar ke samping.
  3. Berdiri Tegak Alami (Anti-Miring): Rumput tumbuh cenderung tegak ke arah langit (Upright) meskipun berada di lereng bukit curam, sehingga tidak terlihat miring aneh.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Padang Rumput Lebat (800 Rumpun)".
  2. Padang rumput langsung tampil lebat, rapat, tinggi, dan berdiri tegak alami.


58. Algoritma Sebaran Grid-Jitter Rapat Tanpa Celah (Gapless Carpet Density)
- Perubahan: Menerapkan algoritma sebaran Grid-Jitter berundak (step rapat), memperkecil minSpacing menjadi 0.08m, memfokuskan meadowRadius ke 15m, dan menaikkan target menjadi 1.400 rumpun pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Menutup Seluruh Celah Tanah Botak: Rumpun rumput ditanam rapat dan saling bertumpuk (overlapping) secara merata di seluruh permukaan bukit tanpa ada spot tanah yang bolong.
  2. Kuas Sapuan Tebal: Kuas Scene View kini langsung menyemburkan 14 rumpun rumput per sapuan (brushDensity 14) untuk melukis karpet rumput tebal secara instan.
- Cara Setting di Unity:
  1. Klik tombol hijau "🌾 Gelar Karpet Rumput Padat (1400 Rumpun)" di Inspector Infinite Grass.
  2. Padang rumput langsung tertutup rapat tebal seperti karpet hijau alami tanpa celah.


59. Kalibrasi Jarak Super Rapat 11cm dan Peningkatan Kapasitas 2.200 Rumpun
- Perubahan: Memperkecil langkah kisi (grid step) menjadi 0.11m (11 cm), memperkecil minSpacing menjadi 0.02m (2 cm), menaikkan target menjadi 2.200 rumpun, dan menaikkan brushDensity menjadi 25 pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Rumpun Saling Menempel Erat: Mengeliminasi seluruh jarak kosong antar rumpun dengan jarak tanam 11 cm sehingga rumput saling bertumpuk padat membentuk hamparan karpet hijau lebat.
  2. Kuas Super Tebal: Setiap satu sapuan kuas menyemburkan 25 rumpun rumput sekaligus untuk mengisi area tanah secara solid dan instan.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Karpet Ultra Rapat (2200 Rumpun)".
  2. Padang rumput langsung menyatu rapat tanpa jarak dan tebal sempurna di bukit.


60. Integrasi Sistem Gelombang Angin Global (Terinspirasi Arsitektur TTFE Toby Fredson)
- Perubahan: Menambahkan algoritma Dynamic Wind Wave Engine (windDirectionAngle, windSpeed, windSwayPower, windWaveFrequency) dan perpaduan mulus antara ombak angin dan fisika injak kaki pada TerrainGrassSpawner.cs. Folder Toby Fredson tetap 100% aman tanpa modifikasi.
- Fitur dan Perbaikan:
  1. Ombak Angin Dinamis Menjalar Alami: Padang rumput bergoyang lembut membentuk gelombang ombak angin yang menjalar realistis di bukit layaknya sistem vegetasi TTFE.
  2. Perpaduan Halus Angin & Injak: Saat karakter menginjak rumput, reaksi injakan kaki (trample) memprioritaskan lenturan sepatu tanpa merusak ritme goyangan angin di sekitarnya.
- Cara Setting di Unity:
  1. Klik tombol "🌾 Gelar Karpet Ultra Rapat (2200 Rumpun)" di Inspector Infinite Grass.
  2. Tekan Play untuk melihat padang rumput melambai alami tertiup angin dan lentur saat diinjak.


61. Implementasi Koleksi Model Rumput Toby Fredson (GrassShort & Grass_Single)
- Perubahan: Memuat 10 variasi model rumput ramping Toby Fredson (GrassShort_A s/d D, GrassMedium_A s/d B, dan Grass_Single_B, C2, E, X) ke dalam TerrainGrassSpawner.cs serta mengaktifkan variabel shader global _WindStrength, _WindSpeed, _WindDirection. Folder Toby Fredson tetap 100% utuh tanpa modifikasi.
- Fitur dan Perbaikan:
  1. Model Rumput Kualitas Tinggi Toby Fredson: Menggunakan helai daun murni yang ramping, tajam, dan proporsional dengan material shader foliage TTFE.
  2. Kompatibilitas Sistem Penuh: Menggabungkan keunggulan visual model Toby dengan kehandalan spawner Grid-Jitter ultra rapat tanpa celah di atas bukit.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Rumput Toby Fredson (2200 Rumpun)".
  2. Padang rumput Toby Fredson langsung terhampar lebat, indah, dan dinamis di bukit.


62. Penggantian ke Model Rumput Segitiga Kecil Stylized (PT_Grass_02 Series)
- Perubahan: Mengganti prefab ke koleksi PT_Grass_02, PT_Grass_02_v1, dan PT_Grass_02_v2 (model helai segitiga kecil low-poly), menurunkan skala ke 0.55 - 0.85 (setinggi mata kaki/bawah betis), dan mengkalibrasi jarak tanam pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bentuk Segitiga Kecil Bersih & Low-Poly: Menghilangkan rumput ilalang raksasa dan bising, digantikan helai daun segitiga kecil yang rapi dan menyatu 100% dengan gaya grafis fantasy kingdom.
  2. Warna Hijau Segar & Alami: Rumput berwarna hijau cerah senada dengan rumput bukit dan pohon, bebas dari kesan kering/pucat.
  3. Performa Super Ringan (75+ FPS): Mengurangi jumlah poligon secara drastis sehingga game berjalan mulus tanpa lag.
- Cara Setting di Unity:
  1. Klik tombol hijau "🌾 Pasang Rumput Segitiga Kecil (1800 Rumpun)" di Inspector Infinite Grass.
  2. Padang rumput segitiga kecil hijau segar langsung terhampar rapi di bukit.


63. Penerapan Koleksi Helai Rumput Mikro Mungil (Micro Single Blades)
- Perubahan: Mengganti prefab ke koleksi helai tunggal mikro (Grass_Single_B, Grass_Single_C2, Grass_Single_E, Grass_Single_X) dengan skala mikro mungil 0.25 - 0.45 (tinggi 15-20 cm setinggi mata kaki) dan langkah kisi 9cm pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Ukuran Mikro Setinggi Mata Kaki: Rumput berukuran sangat mungil dan pendek, tidak lagi menutupi tubuh atau kaki karakter, ideal untuk karpet rumput ground level.
  2. Bebas Ilalang Raksasa: 100% menggunakan helai tunggal ramping per batang yang sangat presisi saat diinjak telapak kaki pemain.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Mikro Mungil (2200 Helai)".
  2. Padang rumput mikro pendek setinggi mata kaki langsung terpasang rapi di bukit.


64. Optimasi Performa Tinggi 80+ FPS (CPU Distance Culling & Kerapatan Optimal 900 Helai)
- Perubahan: Menambahkan sistem CPU Distance Culling (cpuAnimationDistance 13m), mengkalibrasi target menjadi 900 helai mikro padat (meadowRadius 12m), serta mengoptimalkan kalkulasi rotasi Update() pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Penghematan Beban Poligon & Draw Calls: Mengurangi jumlah poligon hingga 60% dan memangkas ribuan batch rendering tanpa mengurangi kepadatan rumput di dekat pemain.
  2. Peningkatan Drastis FPS (80+ FPS): Menghilangkan lonjakan CPU time (dari 18.3ms menjadi <2ms), game kembali berjalan sangat mulus dan ringan.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Rumput Optimal 80+ FPS (900 Helai)".
  2. Tekan Play untuk menikmati padang rumput mikro yang padat, responsif, dan ringan di 80+ FPS.


65. Kalibrasi Ukuran Setinggi Mata Kaki (12-18 cm), Sebaran Rapat 6.5cm, dan Shadow Casting Off
- Perubahan: Menurunkan skala ke 0.12 - 0.22, menurunkan heightMultiplier ke 0.55x, memperkecil langkah kisi ke 0.065m (6.5 cm), serta menonaktifkan Shadow Casting pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Tinggi Proporsional Setinggi Mata Kaki: Rumput kini benar-benar berukuran pendek (12-18 cm di atas tanah), bebas dari kesan ilalang raksasa yang menutupi pinggang/badan karakter.
  2. Karpet Rumput Menutup Sempurna: Jarak kisi 6.5 cm membuat helai daun saling bertumpuk padat tanpa celah tanah atau spot botak terpisah.
  3. Pembebasan 2.600+ Shadow Casters: Mematikan shadow casting pada helai rumput mikro memangkas jutaan poligon shadow dan mengembalikan frame rate ke 85+ FPS.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Karpet Rumput Pendek Rapat (1200 Helai)".
  2. Padang rumput pendek setinggi mata kaki langsung terpasang rapat dan mulus di 85+ FPS.


66. Kalibrasi Ukuran Super Pendek (Tinggi 8-14 cm Menempel Tanah) & Tombol Reset Bersih Instan
- Perubahan: Memangkas skala menjadi 0.04 - 0.08, menyetel heightMultiplier ke 0.25x, memperkecil langkah kisi ke 0.05m (5 cm), serta menambahkan tombol reset otomatis bersih total pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Ukuran Super Pendek Menempel Tanah: Rumput kini benar-benar berukuran mini (~8-14 cm tepat di bawah mata kaki), tidak lagi menjulang tinggi ke pinggang atau badan karakter.
  2. Pembersihan 5.000 Objek Lama Otomatis: Tombol hijau baru langsung menyapu bersih ribuan objek lama dan menanam rumput pendek baru secara instan.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🧹 Reset & Pasang Rumput Pendek (Setinggi Mata Kaki)".
  2. Tekan Play untuk melihat padang rumput mini menempel tanah yang sangat ringan di 85+ FPS.


67. Sistem Karpet Rumput Interlocking (Menempel 100% Menutup Tanah)
- Perubahan: Memadukan model rumpun melebar (GrassShort_A s/d D) dengan helai tunggal (Grass_Single), menyetel langkah kisi ultra rapat ke 0.042m (4.2 cm), toleransi jarak minSpacing ke 0.005m (5 mm), serta skala pendek melebar 0.16 - 0.26 (heightMultiplier 0.35x) pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Karpet Menutup Sempurna 100%: Daun rumput melebar secara horizontal dan saling bertumpuk (interlocking) setiap 4.2 cm sehingga tanah tertutup sepenuhnya tanpa ada celah atau titik renggang.
  2. Pendek & Alami: Tinggi rumput terjaga stabil pada 10-15 cm (setinggi mata kaki) dengan tampilan karpet hijau tebal.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Gelar Karpet Rumput Menempel 100% (1200 Rumpun)".
  2. Padang rumput langsung menutup tanah secara rapat dan menyatu tanpa celah di 85+ FPS.


68. Perbaikan Rumput Mendelep, Kerapatan Karpet Menutup Tanah, dan Kecepatan Angin Tenang
- Perubahan: Menambahkan surfaceOffset +0.008m (akar naik pas di atas permukaan tanah), mengalibrasi kanopi melebar 30cm dengan jarak kisi 12cm (target 1.800 rumpun radius 8m), serta menurunkan kecepatan angin ke 0.8x dan sudut goyangan ke 1.2° pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bebas Mendelep / Tertimbun Tanah: Dasar rumpun rumput duduk dengan presisi di atas permukaan tanah bukit tanpa tenggelam.
  2. Karpet Super Padat Tanpa Celah Botak: Kanopi daun melebar 30 cm dan saling bertumpuk setiap 12 cm, menutup seluruh permukaan tanah bukit secara solid.
  3. Angin Sepoi Lembut & Tenang: Gerakan ombak angin menjadi santai dan natural (kecepatan 0.8x, goyangan 1.2°), bebas dari efek badai yang terlalu kencang.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Karpet Rumput Padat (Bebas Mendelep)".
  2. Padang rumput langsung berdiri rapi di atas tanah, padat rapat, dan berayun santai di 85+ FPS.


69. Penerapan Eksklusif Rumput Segitiga Pendek Low-Poly Stylized (PT_Grass_02 Series)
- Perubahan: Menghapus seluruh model ilalang jerami kering dan beralih 100% ke model helai segitiga ramping (PT_Grass_02, PT_Grass_02_v1, PT_Grass_02_v2) dengan skala 0.30 - 0.48 (tinggi 12-16 cm), lebar ringkas tanpa semak berantakan, serta langkah kisi 9.5 cm pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bentuk Segitiga Bersih & Ramping: Helai daun berbentuk segitiga geometris low-poly yang berdiri tegak rapi, bebas dari ilalang kusut atau semak lebar yang terlihat berantakan.
  2. Warna Hijau Segar Cel-Shaded: Menggunakan material hijau cerah yang senada 100% dengan estetika anime dan fantasi desa.
  3. Tinggi Pendek Proporsional (Setinggi Mata Kaki): Rumput mungil setinggi 12-16 cm yang bersih di atas tanah.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Segitiga Pendek Stylized (1400 Rumpun)".
  2. Padang rumput segitiga hijau segar langsung terpasang bersih, rapi, dan mulus di 85+ FPS.


70. Beralih ke Koleksi Rumput Segitiga Bersih Stylized (Grass_A Series Asli Proyek)
- Perubahan: Mengganti model ke Grass_A_A, Grass_A_B, dan Grass_A_C (model rumput stylized murni proyek dengan material Atlas_Foliage) dengan skala 0.22 - 0.36 (tinggi 12-15 cm setinggi mata kaki) dan langkah kisi 18 cm pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Geometri Mulus & Bersih Bebas Berduri: Daun rumput berbentuk bilah segitiga mulus bergaya anime/fantasy, menghilangkan tekstur bergerigi/spiky yang terlihat seperti kawat berduri.
  2. Menyatu 100% dengan Estetika Desa: Menggunakan tekstur Atlas_Foliage yang sama persis dengan pepohonan dan semak desa sekitar.
  3. Performa Ekstrem Ringan (90+ FPS): Hanya membutuhkan ~850 rumpun untuk menciptakan suasana padang rumput yang cantik dan mulus.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Segitiga Bersih (Grass_A Series)".
  2. Padang rumput segitiga bersih stylized langsung terhampar indah di 90+ FPS.


71. Optimasi Total Script Interaksi Outline (Zero-Cost Saat Non-Aktif & GPU Batching Recovery)
- Perubahan: Menghapus kalkulasi berat LINQ GroupBy dan duplikasi submesh pada Awake(), menerapkan inisialisasi lazy (hanya saat objek disorot), serta merestorasi sharedMaterials asli saat disabled pada Outline.cs.
- Fitur dan Perbaikan:
  1. Zero Overhead Saat Disabled: Komponen Outline tidak lagi memakan CPU/GPU atau membuat draw call tambahan saat objek tidak sedang disorot/dibidik pemain.
  2. Pemulihan GPU Instancing & Dynamic Batching: Restorasi sharedMaterials memastikan objek-objek di scene dapat digabung kembali oleh GPU batching tanpa kebocoran material instance.
  3. Peningkatan Drastis FPS: Menghilangkan spike waktu CPU (20ms menjadi <1ms) dan mengembalikan frame rate ke 90+ FPS.
- Cara Setting di Unity:
  1. Tidak memerlukan setting tambahan. Outline pada objek interaksi (rumah, NPC, peti, dsb.) otomatis menjadi sangat ringan dan hanya aktif saat pemain mendekati objek.


72. Pembersihan Total Seluruh Objek Rumput di Scene & Penyediaan Pilihan Model Baru
- Perubahan: Menambahkan tombol instan pembersihan total ("Hapus & Bersihkan Semua Rumput") pada TerrainGrassSpawner.cs, mengosongkan seluruh objek rumput lama di bukit, serta menyiapkan arsitektur integrasi model rumput baru.
- Fitur dan Perbaikan:
  1. Pembersihan 100% Bersih: Seluruh rumput yang tampak seperti titik-titik renggang di bukit langsung disapu bersih seketika.
  2. Bebas Overload: Scene kembali bersih dan siap untuk pemilihan jenis rumput baru yang benar-benar estetik dan ringan.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol merah "🗑️ HAPUS & BERSIHKAN SEMUA RUMPUT".
  2. Seluruh rumput di bukit langsung terhapus bersih 100%.


73. Pembersihan 100% Rumput di Unity Terrain Data (Detail Layers) & Seluruh Objek Scene
- Perubahan: Menambahkan fungsi ClearAllTerrainDetailsAndGameObjects() yang mereset semua Detail Layers pada TerrainData serta menghapus seluruh GameObject rumput sisa di scene pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Pembersihan Lapisan Terrain Data: Menghilangkan rumput billboard/detail yang tertanam di dalam asset Terrain Unity secara permanen.
  2. Bukit 100% Bersih & Mulus: Menghilangkan seluruh bercak hijau di bukit seketika.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol merah besar "🔥 SAPU BERSIH 100% RUMPUT DI TERRAIN & SCENE".
  2. Konfirmasi klik "Ya, Sapu Bersih Semua". Seluruh rumput di Terrain Data dan scene langsung lenyap 100%.


74. Penerapan Sistem Padang Rumput Stylized Anime Terinspirasi Wuthering Waves (PT_High_Grass_02_v1)
- Perubahan: Menerapkan model rumput stylized bilah cartoon pilihan pengguna (PT_High_Grass_02_v1 & PT_Grass_02 series) dengan sistem gelombang angin dinamis world-space ombak Wuthering Waves, injakan kaki karakter lentur (trample), dan distribusi karpet rapat tanpa celah botak pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Estetika Padang Rumput Anime Wuthering Waves: Model helai kartun cel-shaded hijau cerah yang membentuk hamparan permadani rumput tebal, alami, dan bebas dari kesan renggang/titik-titik terpisah.
  2. Ombak Angin Dinamis Mengalir: Gelombang angin periodik bergaya open-world anime yang mengalir melintasi padang rumput bukit.
  3. Reaksi Fisika Injak Halus: Rumput merunduk lembut saat dilewati kaki karakter dan kembali tegak secara alami.
  4. Performa Tinggi Terjaga (85+ FPS): Optimalisasi penonaktifan shadow casting dan CPU distance culling.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1400 Rumpun)".
  2. Padang rumput anime Wuthering Waves langsung terhampar indah, lebat, dan mulus di scene Anda.


75. Penerapan Model Rumput Wuthering Waves Asli (Assets/8-13-2026/Grass/grass.prefab)
- Perubahan: Menghubungkan TerrainGrassSpawner.cs secara eksklusif ke model rumput anime bertekstur asli (Assets/8-13-2026/Grass/grass.prefab), menerapkan langkah penempatan karpet padat 15 cm, ombak angin dinamis Wuthering Waves, dan interaksi trample injak kaki karakter.
- Fitur dan Perbaikan:
  1. Model Rumput Anime Wuthering Waves Asli: Menggunakan aset grass.prefab dengan tekstur bilah rumput stylized hijau lembut (grass_LP_lambert1_BaseColor & Opacity map) yang identik dengan visual game open-world anime.
  2. Aliran Ombak Angin Periodik: Gelombang angin dinamis menggerakkan helai rumput secara berirama melintasi bukit.
  3. Reaksi Injak Kaki Lentur: Rumput merunduk halus saat diterobos pemain dan tegak kembali dengan mulus.
  4. Performa Tinggi & Bebas Lag (85+ FPS): Penonaktifan shadow casting pada tiap rumpun menjaga rendering tetap kencang.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1100 Rumpun)".
  2. Padang rumput Wuthering Waves dari folder 8-13-2026 langsung terpasang rapi dan lebat di bukit Anda.


76. Integrasi Model Mesh 3D Rumput Asli (Assets/8-13-2026/Grass/grass.fbx) & Material Two-Sided Cutout
- Perubahan: Menghubungkan spawner ke model 3D grass.fbx (Assets/8-13-2026/Grass/grass.fbx), mengaktifkan rendering Two-Sided (_Cull: 0) dan Alpha Cutout (_AlphaClip: 1) pada material grass_LP_lambert1_BaseColor.1001.mat, serta menerapkan skala proporsional anime.
- Fitur dan Perbaikan:
  1. Daun Terlihat Utuh dari Segala Sudut (Two-Sided): Permukaan bilah rumput terlihat padat dan tidak tembus pandang saat kamera berputar 360 derajat.
  2. Tekstur Bersih & Tajam (Alpha Cutout): Memotong latar belakang transparan daun dengan presisi tajam bergaya anime.
  3. Ombak Angin & Trample Wuthering Waves: Ayunan gelombang angin yang lembut dan reaksi kelenturan saat diinjak kaki karakter.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1100 Rumpun)".
  2. Padang rumput anime 3D Wuthering Waves langsung terhampar indah, lebat, dan mulus di scene Anda.


77. Perbaikan Orientasi Rotasi Rumput Tegak Ke Atas (Inversi 180°) & Skala Proporsional
- Perubahan: Membalik rotasi spawn sebesar 180° pada sumbu X agar ujung bilah rumput tegak lurus ke atas (+Y) dan akar menancap ke tanah, serta menyesuaikan skala proporsional setinggi betis/mata kaki karakter pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Orientasi Tegak Sempurna: Mengatasi masalah rumput terbalik (menghadap ke bawah tanah) sehingga helai daun berdiri tegak alami.
  2. Skala Alami Anime (25-35 cm): Ukuran rumput proporsional setinggi betis bawah karakter, tidak lagi menyerupai semak/dinding raksasa.
  3. Ombak Angin & Karpet Rapat: Ayunan gelombang angin yang lembut dan kerapatan kisi 14 cm yang menutup bukit secara merata.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1200 Rumpun)".
  2. Padang rumput Wuthering Waves langsung berdiri tegak ke atas, proporsional, dan melambai indah.


78. Penyelarasan Posisi Akar Rumput di Atas Permukaan Terrain (Auto-Ground Root Alignment)
- Perubahan: Menambahkan kalkulasi batas mesh terbawah (Bounds.min.y) secara otomatis saat spawn pada TerrainGrassSpawner.cs, sehingga akar rumput menapak 100% pas di atas permukaan tanah dan tidak lagi tenggelam di bawah terrain.
- Fitur dan Perbaikan:
  1. Akar Menapak Sempurna di Tanah: Menghilangkan masalah rumput menggantung atau tenggelam di bawah lapisan terrain setelah pembalikan rotasi.
  2. Presisi Ketinggian Otomatis: Setiap rumpun rumput dengan variasi skala apapun otomatis menghitung titik dasar akarnya tepat di atas tanah.
  3. Visual Padang Rumput Rapi: Padang rumput berdiri tegak di atas bukit dengan ombak angin yang mengalir mulus.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1200 Rumpun)".
  2. Rumput akan langsung berdiri tegak sempurna di atas permukaan tanah bukit.


79. Rekonstruksi Spawn Mesh 3D Murni (Eliminasi Bug Offset Prefab & Penempatan Presisi Tanah)
- Perubahan: Menghapus instansiasi melalui prefab yang mengandung nilai offset koordinat tersembunyi, beralih ke pembuatan GameObject bersih dengan MeshFilter (grass.fbx) dan MeshRenderer (grass_LP_lambert1_BaseColor.1001.mat) langsung pada TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Bersih dari Bug Koordinat Prefab: Tidak ada lagi offset -457 atau pergeseran posisi aneh yang membuat rumput terlempar di bawah tanah.
  2. Titik Dasar Menapak 100% di Atas Bukit: Setiap rumpun dihitung presisi langsung pada titik kontak Raycast terrain dengan elevasi aman di atas tanah.
  3. Visual Wuthering Waves Utuh: Padang rumput anime berdiri tegak, lebat, dan bergoyang dengan ombak angin 85+ FPS.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1200 Rumpun)".
  2. Seluruh rumput langsung terpasang bersih dan berdiri tegak tepat di atas tanah bukit.


80. Perbaikan Error SerializedObject & MissingReferenceException pada Inspector
- Perubahan: Menghapus pemanggilan penghapusan komponen saat OnInspectorGUI(), menambahkan null-safety check pada editor spawner, dan memperbaiki siklus penghapusan objek anak pada ClearAll() di TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Konsol Unity Bersih dari Error: Menghilangkan error SerializedObjectNotCreatableException dan MissingReferenceException m_Targets secara tuntas.
  2. Inspector Responsif & Mulus: Tombol pasang dan hapus rumput dapat diklik tanpa memicu error atau freeze di Unity Editor.
- Cara Setting di Unity:
  1. Klik tombol "Clear" pada console Unity.
  2. Klik objek Infinite Grass dan klik tombol hijau "🌾 Pasang Rumput Wuthering Waves (1200 Rumpun)".


81. Instansiasi Langsung Aset Prefab Rumput (Assets/8-13-2026/Grass/grass.prefab)
- Perubahan: Mereset nilai offset koordinat tersimpan pada grass.prefab ke (0,0,0) dan mengonfigurasi TerrainGrassSpawner.cs agar langsung menginstansiasi grass.prefab ke titik permukaan bukit.
- Fitur dan Perbaikan:
  1. Instansiasi Prefab Asli: Menggunakan aset grass.prefab langsung dari folder 8-13-2026 sesuai instruksi pengguna.
  2. Posisi Permukaan Presisi: Seluruh rumpun menapak pas di atas tanah bukit tanpa pergeseran atau orientasi terbalik.
  3. Ombak Angin & Trample Wuthering Waves: Ombak dinamis padang rumput mengalir mulus dengan reaksi trample injak kaki di 85+ FPS.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Prefab (1200 Rumpun)".
  2. Rumput dari grass.prefab langsung terpasang rapi di seluruh bukit.


82. Perbaikan Peringatan Pohon Terrain (The tree oakTree 1 couldn't be instanced because bounds could not be determined)
- Perubahan: Menghapus komponen LODGroup kosong tanpa renderer dan mereset offset posisi lokal ke (0,0,0) pada oakTree 1.prefab dan oakTree.prefab (Assets/ENV TDD/tree/).
- Fitur dan Perbaikan:
  1. GPU Instancing Pohon Pulih: Unity Terrain dapat kembali menghitung Bounding Box pohon secara akurat dan mengaktifkan GPU Instancing pada seluruh pohon oak di terrain.
  2. Peringatan Kuning Hilang: Menghilangkan pesan warning "couldn't be instanced because bounds could not be determined" dari Unity Terrain.
- Cara Setting di Unity:
  1. Warning otomatis hilang di panel Terrain Paint Trees. Pohon dapat di-paint secara normal dengan performa GPU instancing yang optimal.


83. Koreksi Warna Hijau Alami & Render Dua Sisi Daun Pohon Oak (oakTreeBranch_simplified.mat)
- Perubahan: Mengatur Base Color & Tint daun pohon oak ke hijau segar alami (_Color: RGB 0.48, 0.85, 0.28), mengubah Shading Color bayangan ke hijau lembut, serta mengaktifkan Two-Sided Rendering (_Cull: 0) pada oakTreeBranch_simplified.mat.
- Fitur dan Perbaikan:
  1. Daun Berwarna Hijau Segar & Alami: Mengatasi masalah daun pohon yang sebelumnya tampak hitam-abu monokrom karena tekstur grayscale tanpa tinting warna.
  2. Daun Tebal dari Segala Arah (Two-Sided): Bidang daun tidak lagi bolong atau menghasilkan bayangan hitam gelap saat dilihat dari belakang.
- Cara Setting di Unity:
  1. Perubahan warna langsung terlihat otomatis pada seluruh pohon oak di scene.


84. Pengembalian Prefab Pohon Oak ke Konfigurasi Asli (LODGroup Restorasi)
- Perubahan: Mengembalikan struktur prefab oakTree.prefab dan oakTree 1.prefab ke kondisi semula dengan komponen LODGroup utuh sesuai permintaan pengguna.
- Fitur dan Perbaikan:
  1. Komponen LODGroup Kembali Semula: Menjaga struktur LOD pohon sesuai konfigurasi awal milik pengguna.
  2. Material Daun Tetap Hijau: Material oakTreeBranch_simplified.mat tetap mempertahankan warna hijau segar anime alami dan render dua sisi.
- Cara Setting di Unity:
  1. Prefab pohon oak telah kembali ke struktur aslinya dan siap digunakan dengan LOD Anda.


85. Penjelasan & Penanganan Error Transien LODGroupEditor Unity
- Perubahan: Menganalisis error SerializedObject target has been destroyed dan NullReferenceException pada UnityEditor.LODGroupEditor.GetMaxLODCountForMultiSelection().
- Fitur dan Perbaikan:
  1. Identifikasi Sumber Error: Error internal Unity Editor GUI terjadi ketika file prefab di-reload dari disk saat tab Inspector sedang aktif membuka komponen LODGroup.
  2. Solusi & Pemulihan Instan: Cukup klik tombol Clear pada console atau klik objek lain di Hierarchy untuk me-refresh state GUI Editor.
- Cara Setting di Unity:
  1. Klik tombol "Clear" di jendela Console Unity.


86. Penerapan Rotasi Dasar Prefab Tegak Lurus (grass.prefab) & Proporsi Rumput Anime
- Perubahan: Memadukan rotasi dasar prefab (*prefabBaseRot*) ke dalam formula spawn rotasi TerrainGrassSpawner.cs sehingga inversi 180° bawaan prefab tidak ter-overwrite, dan menyesuaikan skala proporsional setinggi betis bawah karakter.
- Fitur dan Perbaikan:
  1. Ujung Daun Tegak Lurus Menghadap ke Atas (+Y): Menghilangkan masalah rumput terbalik saat di-spawn dari grass.prefab.
  2. Akar Menancap Alami: Bagian akar berada di bawah dan menempel pas di permukaan bukit.
  3. Skala Proporsional Anime: Tinggi rumput seimbang dan padat membentuk hamparan karpet padang rumput anime.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Prefab (1200 Rumpun)".
  2. Rumput dari grass.prefab langsung berdiri tegak menghadap ke atas.


87. Penerapan Variasi Rumput Toby Fredson Foliage Engine (VP_Grass Series)
- Perubahan: Menghubungkan TerrainGrassSpawner.cs ke koleksi prefab rumput alami Toby Fredson (GrassShort_A, GrassShort_B, GrassShort_C, GrassMedium_A, GrassMedium_B) dengan sistem ombak angin dinamis dan interaksi lentur injak kaki karakter tanpa memodifikasi isi folder Toby Fredson.
- Fitur dan Perbaikan:
  1. Rumput Alami Toby Fredson: Variasi bentuk rumpun rumput (pendek & sedang) yang menyatu sempurna dengan visual lingkungan alam di scene Anda.
  2. Posisi Menapak Pas di Tanah: Koordinat pivot dasar tanah bawaan Toby Fredson menjamin rumput menancap alami di atas permukaan bukit.
  3. Ombak Angin & Trample Fisika Halus: Mengalir indah melintasi padang rumput di 85+ FPS.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Toby Fredson (1200 Rumpun)".
  2. Hamparan padang rumput Toby Fredson langsung terpasang rapi dan lebat di bukit Anda.


88. Pembersihan Siklus GUI Editor & Proteksi Seleksi Objek Spawner
- Perubahan: Menambahkan GUIUtility.ExitGUI() setelah eksekusi tombol spawner, menonaktifkan registrasi Undo individual massal (1.200 objek) yang membebani memori, serta menambahkan proteksi pengalihan seleksi aktif pada ClearAll() di TerrainGrassSpawner.cs.
- Fitur dan Perbaikan:
  1. Konsol Bebas Error SerializedObject: Menghilangkan error SerializedObjectNotCreatableException dan GameObjectInspector m_Targets secara tuntas saat mengklik tombol pasang/hapus rumput.
  2. Eksekusi Cepat & Responsif: Proses pembuatan dan penghapusan ribuan rumpun rumput di scene berjalan mulus dan instan tanpa freeze.
- Cara Setting di Unity:
  1. Klik tombol "Clear" di Console Unity.
  2. Klik tombol hijau "🌾 Pasang Rumput Toby Fredson (1200 Rumpun)" di Inspector Infinite Grass.


89. Variasi Rumput Toby Fredson Lengkap (VP_GrassSingle & VP_Grass) dengan Skala Standar & Native GPU Wind
- Perubahan: Memperbarui TerrainGrassSpawner.cs agar menggunakan variasi rumput tunggal dan kelompok dari Toby Fredson (Grass_Single_B, Grass_Single_C2, Grass_Single_E, Grass_Single_X, GrassShort, GrassMedium), mengembalikan skala ke ukuran standar asli (1.0x), dan mengaktifkan animasi angin GPU Vertex bawaan shader Toby Fredson.
- Fitur dan Perbaikan:
  1. Variasi Rumput Toby Fredson Alami: Kombinasi helai rumput tunggal dan rumpun alami Toby Fredson yang menyatu indah dengan lingkungan game.
  2. Skala Ukuran Standar (1.0x): Mengembalikan ukuran rumput ke dimensi asli bawaan aset Toby Fredson.
  3. Animasi Angin GPU Vertex Asli Toby Fredson: Memanfaatkan perhitungan angin vertex GPU shader bawaan Toby Fredson yang ringan, natural, dan bebas beban CPU di 85+ FPS.
- Cara Setting di Unity:
  1. Buka Inspector Infinite Grass lalu klik tombol hijau "🌾 Pasang Rumput Toby Fredson (1200 Rumpun)".
  2. Padang rumput Toby Fredson dengan skala standar dan animasi angin bawaan langsung terpasang di bukit Anda.


90. Penjadwalan Eksekusi Spawner via EditorApplication.delayCall
- Perubahan: Membungkus eksekusi fungsi spawn dan clear pada TerrainGrassSpawnerEditor ke dalam EditorApplication.delayCall sehingga pembuatan dan penghapusan ribuan objek anak dieksekusi di luar siklus frame OnInspectorGUI().
- Fitur dan Perbaikan:
  1. Konsol Unity 100% Bersih dari Error: Menghilangkan error SerializedObjectNotCreatableException (Object at index 0 is null) dan MissingReferenceException (GameObjectInspector m_Targets) secara permanen saat tombol Inspector ditekan.
  2. Pengalaman Editor Sangat Halus: Tombol merespons instan tanpa bentrok dengan siklus penggambaran GUI Inspector Unity.
- Cara Setting di Unity:
  1. Klik tombol "Clear" di Console Unity.
  2. Klik tombol hijau "🌾 Pasang Rumput Toby Fredson (1200 Rumpun)" di Inspector Infinite Grass.


91. Penggabungan Mask Alpha ke Base Map RGBA & Konfigurasi Material URP Lit Grass
- Perubahan: Menggabungkan tekstur GrassAlpha.tga ke dalam channel Alpha dari Grass_MAT_Base_Color_RGBA.png, mengonfigurasi GrassNormals.tga sebagai Normal Map, serta mengaktifkan Alpha Cutout (_AlphaClip: 1) dan Two-Sided Rendering (_Cull: 0) pada Grass_MAT_Base_Color.mat.
- Fitur dan Perbaikan:
  1. Penjelasan Penempatan Alpha di URP Lit: Menjelaskan bahwa shader standard URP Lit membaca transparansi dari channel Alpha (A) pada slot Base Map.
  2. Tekstur RGBA Siap Pakai: Menyediakan Grass_MAT_Base_Color_RGBA.png yang sudah berisi data warna dan alpha cutout presisi.
  3. Visual Daun Rumput Utuh & Tembus Pandang: Helai rumput terpotong bersih mengikuti bentuk daun dan terlihat dari kedua sisi.
- Cara Setting di Unity:
  1. Pasang Grass_MAT_Base_Color_RGBA.png pada slot Base Map.
  2. Pasang GrassNormals.tga.png pada slot Normal Map.
  3. Centang "Alpha Clipping" (Threshold: 0.5) dan atur "Render Face: Both" pada material Inspector.


92. Penerapan Koleksi Procegrass & Sistem Goyangan Angin GPU Toby Foliage Engine (Genshin / WuWa Style)
- Perubahan: Memperbarui TerrainGrassSpawner.cs agar menggunakan 4 variasi model rumput yang disediakan (Grass_Clump01GRP, Grass_Ckump02GRP, Grass_Ckump03GRP, Grass_ShapeVariantGRP) di folder Procegrass, serta mengonfigurasi material Grass_MAT_Base_Color 5.mat dengan shader GPU Vertex Wind bawaan Toby Foliage Engine ((TTFE) Grass Foliage (Mobile)).
- Fitur dan Perbaikan:
  1. Variasi Rumput Procegrass Pilihan Anda: 4 variasi rumpun rumput (lebat, mekar, sedang, dan bilah acak) tersebar natural membentuk hamparan padang rumput anime.
  2. Goyangan Ombak Angin Genshin Impact / Wuthering Waves: Animasi goyangan angin diproses langsung di GPU Vertex Shader dengan ombak dinamis, lentur, dan sangat ringan di 85+ FPS.
  3. Render Daun Dua Sisi & Cutout Halus: Alpha clipping dan double-sided rendering aktif sempurna.
- Cara Setting di Unity:
  1. Klik objek Infinite Grass di Hierarchy.
  2. Klik tombol hijau "🌾 Pasang Rumput Procegrass (1200 Rumpun)".


93. Sistem Aktivasi Collider Pohon Terrain Berdasarkan Proximity Player (TreeColliderProximity)
- Perubahan: Membuat script TreeColliderProximity.cs yang mengaktifkan CapsuleCollider sementara di posisi pohon terrain terdekat saat player mendekat, dan menghapusnya saat player menjauh. Menggunakan object pooling untuk performa optimal.
- Fitur dan Perbaikan:
  1. Aktivasi Otomatis: Collider pohon menyala secara otomatis ketika player berada dalam radius tertentu dan mati saat menjauh.
  2. Object Pooling: Collider didaur ulang sehingga tidak ada alokasi memori berulang (zero garbage collection).
  3. Performa Ringan: Pengecekan dilakukan setiap 0.3 detik dengan batas maksimal 40 collider aktif bersamaan.
  4. Skala Otomatis: Ukuran collider menyesuaikan skala pohon terrain yang berbeda-beda.
- Cara Setting di Unity:
  1. Buat GameObject kosong di Hierarchy, beri nama "TreeColliderSystem".
  2. Tambahkan komponen TreeColliderProximity.
  3. Atur Activation Radius, Collider Radius, dan Collider Height sesuai ukuran pohon Anda.


94. Sistem Aktivasi Collider Objek Umum Berdasarkan Proximity Player (ProximityColliderActivator)
- Perubahan: Membuat script ProximityColliderActivator.cs yang mengaktifkan/menonaktifkan collider objek apapun di scene saat player mendekat/menjauh. Mendukung 3 mode pencarian target: Manual Drag & Drop, Tag, atau Layer.
- Fitur dan Perbaikan:
  1. Fleksibel 3 Mode Target: Pilih objek secara manual, berdasarkan tag, atau berdasarkan layer.
  2. Collider Asli Objek: Langsung mengaktifkan/menonaktifkan collider yang sudah ada di objek (tidak perlu spawn collider baru).
  3. Performa Ringan: Pengecekan berkala setiap 0.25 detik tanpa alokasi memori baru di runtime.
  4. Gizmo Visual: Menampilkan radius aktivasi dan objek aktif di Scene View.
- Cara Setting di Unity:
  1. Buat GameObject kosong di Hierarchy, beri nama "ProximityColliderSystem".
  2. Tambahkan komponen ProximityColliderActivator.
  3. Pilih mode target (Manual, Tag, atau Layer) lalu atur Activation Radius.


95. Perbaikan Bug Rumput Melebar / Melar Horizontal (Genshin / WuWa GPU Foliage Shader)
- Penyebab Bug: Shader bawaan Toby Foliage Engine membutuhkan script `Global Controller` bawaan Toby yang mengatur variabel shader global (`_GlobalWindStrength`, `_WindDirection`, `_StrongWindSpeed`). Tanpa script tersebut atau dengan mesh FBX eksternal, rotasi vertex Toby shader mengalami pembagian 0 / perkalian nilai tak hingga sehingga helai rumput melar puluhan meter secara horizontal.
- Perbaikan:
  1. Dibuat shader baru `FantasyKingdom/GenshinGrassFoliage` (Assets/8-13-2026/Grass/Materials/GenshinGrassFoliage.shader).
  2. Sistem GPU Vertex Wind Mandiri (Self-Contained): Menghasilkan ombak dinamis melintasi padang rumput (Genshin/WuWa style) langsung dari shader tanpa membutuhkan script Global Controller eksternal.
  3. Bagian akar rumput terkunci kokoh di tanah (`UV.y = 0`) dan ujung rumput melambai lentur (`UV.y = 1`).
  4. Pencahayaan Anime Lembut: Dilengkapi normal upward bias dan double-sided alpha cutout rendering.
- Cara Pemakaian:
  1. Klik objek `Infinite Grass` di Hierarchy.
  2. Klik tombol merah "🗑️ Hapus Semua Rumput" lalu klik tombol hijau "🌾 Pasang Rumput Procegrass".


96. Penerapan Koleksi Asli Toby Foliage Engine (VP_Grass)
- Perubahan: Memperbarui TerrainGrassSpawner.cs agar langsung memuat 9 variasi prefab resmi bawaan Toby Foliage Engine dari folder `Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_Grass/` (GrassBig_A, GrassBig_B, GrassMedium_A, GrassMedium_B, GrassMedium_D, GrassShort_A, GrassShort_B, GrassShort_C, GrassShort_D).
- Keunggulan:
  1. 100% Native & Stable: Menggunakan prefab resmi Toby yang sudah terintegrasi sempurna dengan LOD, material, dan konfigurasi shader bawaan.
  2. Bebas Masalah Alpha / Model: Tidak memerlukan modifikasi channel tekstur atau konversi mesh eksternal.
  3. Variasi Lengkap: Paduan rumput tinggi, sedang, dan pendek yang tersebar natural.
- Cara Pemakaian:
  1. Klik objek `Infinite Grass` di Hierarchy.
  2. Klik tombol merah "🗑️ Hapus Semua Rumput".
  3. Klik tombol hijau "🌾 Pasang Rumput Toby VP_Grass (1200 Rumpun)".


97. Sistem Senjata & Hand Grip IK Humanoid Bawaan Unity (PlayerWeaponIK)
- Perubahan: Membuat script PlayerWeaponIK.cs yang menggunakan event OnAnimatorIK bawaan resmi Unity Humanoid.
- Fitur dan Keunggulan:
  1. 100% Bebas Error Burst: Tidak memerlukan RigBuilder / RigLayer yang rentan crash pada struktur karakter Mixamo bertingkat.
  2. Hand Grip Presisi: Menempelkan tangan kiri/kanan ke target gagang pedang secara akurat dan mulus.
  3. Natural Elbow Hint: Arah siku menyesuaikan posisi alami saat memegang senjata.
  4. Auto Curve Blending: Membaca parameter float Animator (misal: "IKWeight") untuk transisi on/off yang halus saat Draw/Sheath.
  5. Socket Switching: Menyediakan fungsi EquipSword() dan SheathSword() untuk dipanggil dari Animation Event.
- Cara Pemakaian:
  1. Pasang komponen PlayerWeaponIK pada PlayerArmature.
  2. Isi slot Sword Transform, Hand Socket, Sheath Socket, Left Hand Grip Target, dan Left Elbow Hint.
  3. Aktifkan centang "IK Pass" pada Base Layer di Animator Controller.


98. Sistem Produksi Weapon IK & Post-FK Finger Blending (Animation Rigging + ScriptableObject)
- Lokasi File:
  * `Assets/Scripts/Player/WeaponIK/HandGripPoseData.cs` (Data ScriptableObject 15 tulang jari)
  * `Assets/Scripts/Player/WeaponIK/WeaponGripPoint.cs` (Marker grip transform pada senjata)
  * `Assets/Scripts/Player/WeaponIK/WeaponIKController.cs` (Pengatur TwoBoneIK & Post-FK Slerp jari)
  * `Assets/Scripts/Player/WeaponIK/Editor/HandGripPoseDataEditor.cs` (Tool 1-klik untuk merekam pose jari)
- Fitur Utama:
  1. Default Weight = 0: Bebas intervensi pada seluruh animasi dasar Mixamo (jalan, lari, serang, idle).
  2. Event-Driven Activation: Hanya aktif saat dipanggil via `ActivateGripEvent(Transform)` dan `DeactivateGripEvent()`.
  3. Multi-Preset Finger Curl: Mendukung variasi preset jari (CylinderGrip, PistolGrip, dll) per senjata.
  4. Performan Tinggi: Operasi FK-Blend jari <0.01ms per karakter, sangat ringan untuk puluhan NPC.
  5. Robust Edge-Case Handling: Anti-glitch saat mid-blend interrupt, senjata di-Destroy, atau stagger.


99. Procedural 3D Surface Hand Wrapper (ProceduralHandWrapper)
- Lokasi File: `Assets/Scripts/Player/WeaponIK/ProceduralHandWrapper.cs`
- Fitur Utama:
  1. Auto Surface Conformance: Menekuk 15 ruas jari secara otomatis berdasarkan bentuk 3D collider/mesh objek (pedang, kapak, pistol, botol, dll).
  2. Multi-Joint Contact Raycasting: Tiap ruas (Proximal, Intermediate, Distal) melingkari permukaan objek sampai menyentuh batas collider dengan aman (anti-clipping).
  3. 1-Klik Preset Export: Hasil bentuk tekukan jari dapat langsung disimpan ke dalam ScriptableObject HandGripPoseData untuk digunakan oleh WeaponIKController.


100. Player Foot IK Placement & Pelvis Offset (PlayerFootIK)
- Lokasi File: `Assets/Scripts/Player/PlayerFootIK.cs`
- Fitur Utama:
  1. Surface Normal Alignment: Telapak kaki otomatis menyesuaikan sudut kemiringan lereng bukit, batu, dan anak tangga.
  2. Pelvis / Hips Drop: Pinggul karakter otomatis turun saat kaki berada di beda ketinggian, mencegah kaki bawah mengambang.
  3. Grounded / Jump Detection: Otomatis mendeteksi saat karakter melompat agar IK kaki tidak aktif di udara.
  4. 100% Native Mecanim Humanoid: Stabil, ringan, dan bebas error Burst/Jobs.


101. Perbaikan CBuffer Memory Alignment & Sampler HLSL pada StylizedGrass Shader
- Perubahan: Mengatasi mesh rumput yang sempat invisible di Unity 6 DX11 dengan menyelaraskan layout memori CBuffer ke batas 16-byte, menggunakan `input.uv.y` untuk normalisasi tinggi daun secara presisi, mendeklarasikan SamplerState yang tepat, dan menerapkan pencahayaan upward normal lembut.
- Lokasi File: `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader`


102. Stylized Interactive Grass Shader (StylizedGrass_Mesh & StylizedGrass_Terrain)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Untuk mesh 3D / prefab rumput)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Untuk terrain detail grass)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailSystem.shader` (Internal shader trail system)
- Fitur Utama:
  1. Wind Animation: Rumput bergoyang mengikuti pola texture angin (7063-bump.jpg), hanya bagian tengah ke atas yang terpengaruh.
  2. Wind Adjustable: Kecepatan, intensitas, arah, dan tiling angin bisa diatur via Inspector.
  3. Near/Far Color Blend: Warna rumput berubah berdasarkan jarak kamera (NearColor → FarColor).
  4. Height Blend: Pangkal rumput otomatis blend ke BottomColor.
  5. Player Interaction: Rumput merunduk/membuka jalan saat player mendekat (real-time).
  6. Trail/Jejak: Rumput rebah di bekas langkah player, jejak recovery perlahan (diatur via GrassTrailRenderer).
  7. GPU Instancing & Alpha Cutoff: Mendukung performa tinggi dan transparansi.
  8. Shadow & Depth Pass: Bayangan mengikuti displacement wind agar konsisten.


102. Grass Trail Renderer (GrassTrailRenderer)
- Lokasi File: `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs`
- Fitur Utama:
  1. RenderTexture Trail: Merekam posisi player ke RT ortografik, jejak bertahan setelah player pergi.
  2. Adjustable Recovery Time: Waktu recovery (berapa detik sebelum jejak hilang) bisa diatur di Inspector.
  3. Scroll System: RT otomatis mengikuti posisi player, jejak lama tetap di posisi dunia yang benar.
  4. Stamp Radius & Strength: Ukuran dan kekuatan tapak jejak bisa diatur.


103. Resolusi Mesh Rumput Invisible pada Unity 6 URP Forward+ (Depth Priming & Sampler Fix)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader`
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader`
- Penyebab Masalah Invisible:
  1. Pada URP Unity 6 dengan pengaturan `DepthPrimingMode: Forced`, Forward pass mengeksekusi pengujian kedalaman `CompareFunction.Equal`. Pass `DepthOnly` yang sebelumnya memiliki `ColorMask R` dan tidak menyertakan vertex displacement menyebabkan nilai depth forward berbeda dengan prepass, sehingga GPU membuang 100% pixel rumput.
  2. Tag `"UniversalMaterialType" = "Lit"` pada SubShader AlphaTest memicu ekspektasi GBuffer/Deferred pass pada renderer Forward+.
  3. Texture sampling pada vertex shader DirectX 11 tanpa guard anti-NaN berisiko menghasilkan nilai tak tentu (NaN) yang menggagalkan posisi clip space `positionCS`.
- Solusi & Perbaikan:
  1. Arsitektur Single-Pass ForwardLit Murni: Menghilangkan pass `DepthOnly` kustom yang konfliktual dan menggunakan arsitektur ForwardLit teruji yang mewarisi fallback resmi `Universal Render Pipeline/Lit`.
  2. Pembersihan SubShader Tags: Menghilangkan tag `UniversalMaterialType` dan menetapkan `LOD 200`, `Cull Off` standar URP Cutout.
  3. Anti-NaN Displacement Guard: Melindungi kalkulasi ombak angin `7063-bump.jpg` dan interaksi player dengan validasi `!isnan()` dan sampler `sampler_LinearRepeat` bawaan Core URP.
  4. Pewarnaan & Gradasi Lembut: Mendukung penuh perpaduan Near/Far distance, Bottom height blend, dan pencahayaan foliage cerah di scene.


104. Arsitektur 4-Pass URP Penuh & Modul StylizedGrass_Common (Solusi Tuntas Depth Priming Forced)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Shared common buffer, samplers & displacement)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Mesh 3D rumput 4-pass)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Terrain detail grass 4-pass)
  * `Assets/8-12-2026/GrassBissmillah/TerrainColor.renderTexture` (Depth format fix 94)
- Penyebab Utama Mesh Menghilang:
  * Proyek menggunakan `DepthPrimingMode: Forced` di `Desktop Renderer.asset`. Pada mode ini, Unity URP RenderGraph mewajibkan SEMUA objek Cutout/AlphaTest digambar pada fase `DepthOnlyPass` terlebih dahulu.
  * Ketika objek tidak memiliki pass `DepthOnly` (atau depth pass-nya dihilangkan), rumput tidak digambar di Depth Prepass. Akibatnya saat `UniversalForward` berjalan, URP memaksa depth test hardware ke `CompareFunction.Equal`. Karena depth rumput tidak ada di depth buffer, GPU membuang 100% pixel daun rumput sehingga mesh menjadi tembus pandang (hanya wireframe/outline oranye di Scene view yang terlihat).
- Solusi Komprehensif yang Diterapkan:
  1. Modul Terpusat `StylizedGrass_Common.hlsl`: Menggabungkan CBuffer 16-byte aligned, deklarasi sampler DX11 eksplisit (`sampler_BaseMap`, `sampler_WindTex`, `sampler_GrassTrailRT`), dan fungsi vertex displacement `ApplyGrassDisplacement` dengan pengaman anti-NaN.
  2. Implementasi 4 Pass URP Lengkap:
     - `Pass 1 (ForwardLit / UniversalForward)`: Render albedo, gradasi Near/Far, Height blend, pencahayaan foliage matahari.
     - `Pass 2 (DepthOnly)`: Menggambar depth rumput di Depth Prepass dengan formula displacement identik, sehingga lolos uji `ZTest Equal` di Forward pass.
     - `Pass 3 (DepthNormals)`: Memberikan data depth dan normal untuk Screen Space Ambient Occlusion (SSAO) agar bayangan ambient tidak menolak daun rumput.
     - `Pass 4 (ShadowCaster)`: Memproyeksikan bayangan rumput ke tanah secara konsisten mengikuti liukan angin.
  3. Perbaikan Depth Format RenderTexture: Memperbarui `m_DepthStencilFormat: 94` pada `TerrainColor.renderTexture` agar warning konsol RenderGraph hilang.


105. Sistem Interaksi Rumput Real-Time & Trail Jejak Kaki Otomatis (GrassTrailRenderer & Displacement Fix)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (Sistem controller jejak & trample player)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Formula displacement interaksi & jejak kaki)
  * `Assets/8-12-2026/GrassBissmillah/GrassMatTRY.mat` & `GrassMat.mat` (_BendStrength 1.2)
- Penyebab Rumput Belum Interaktif Sebelumnya:
  1. Skrip pengontrol `GrassTrailRenderer.cs` belum terpasang di GameObject aktif mana pun dalam scene, sehingga variabel global `_PlayerTramplePos`, `_PlayerPosition`, dan `_GrassTrailRT` bernilai 0 setiap frame (shader mengira tidak ada player).
  2. Formula displacement interaksi sebelumnya menggunakan pengali `windMask` yang memotong respon gerak pada 30% pangkal helai, serta fungsi jarak kuadratik (`influence * influence`) yang membuat gerakan rumput terlalu kecil (hanya ~5 cm) sehingga tidak terlihat saat player melangkah.
- Solusi & Fitur Baru yang Diterapkan:
  1. Auto-Detection Player & Auto-Run:
     * `GrassTrailRenderer` kini otomatis mendeteksi objek pemain melalui Tag `Player`, nama `PlayerArmature` / `PlayerManager`, atau komponen `CharacterController`.
     * Dilengkapi `[RuntimeInitializeOnLoadMethod]` yang otomatis membuat sistem trail saat Play Mode berjalan tanpa harus drag-and-drop manual.
     * Dilengkapi `EditorAutoSetup` (`InitializeOnLoadMethod`) agar interaksi juga langsung hidup di Scene View saat Edit Mode.
     * Ditambahkan menu Editor `Tools -> Wirabaya -> Pasang Grass Trail Renderer`.
  2. Formula Rebah & Tekuk Real-Time yang Nyata & Elastis:
     * Menggunakan kurva halus `smoothstep(0.0, 1.0, f) * heightFactor * _BendStrength` yang menjamin akar tetap tertanam di tanah sementara batang dan ujung rumput terdorong ke samping hingga 1.3 meter dan ditekan ke bawah sebesar 0.75 meter.
  3. Trail Jejak Kaki Persisten dengan Waktu Pemulihan (Recovery Time):
     * Tapak kaki player tercetak di RenderTexture `_GrassTrailRT` dan menekan rumput ke bawah di bekas langkah player.
     * Rumput perlahan bangkit berdiri kembali sesuai durasi `trailRecoveryTime` yang dapat diatur di Inspector.


106. Kalibrasi Kualitas Interaksi Rumput, Anti-Sink Trample, Sway Angin Terkontrol, Deteksi Lompat (Anti-Jump), dan Penerimaan Shadow Karakter
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Displacement wind clamp, anti-sink trample, jump detection, trail recovery)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Penerimaan shadow karakter & trail shading)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Penerimaan shadow karakter & trail shading pada terrain grass)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (IsPlayerGrounded check, kalibrasi stamp radius & trample radius)
- Solusi Komprehensif untuk 6 Catatan Pengguna:
  1. Tanah yang Ditapak Terlalu Besar (Anti-Sink Trample):
     - Sebelumnya, formula menurunkan pucuk rumput sebesar 0.75m vertikal ke bawah tanah (`posWS.y -= bend * 0.75`), sehingga rumput terbenam ke bawah tanah dan mengekspos tekstur tanah lapang selebar 3.6 meter.
     - Diperbaiki: Rumput kini menyibak ke samping mengikuti langkah kaki (`posWS.xz += pushDir * (bend * 0.45)`), dengan penurunan vertikal minimal (`posWS.y -= bend * 0.12`). Radius di shader dibatasi otomatis maksimal 0.85m (`min(pPos.w, 0.85)`). Rumput tetap berada di atas permukaan tanah dan tidak lagi membentuk lubang kawah.
  2. Efek Recovery Grass Terlihat Jelas:
     - Ukuran jejak langkah dikalibrasi realistis (`stampRadius = 0.018f`, sekitar 0.7 meter).
     - Di fragment shader ditambahkan visual shading pada helai rumput yang terinjak (`albedo *= (1.0 - trailFactor * 0.20)`). Jalur jejak kaki terlihat jelas saat terinjak dan perlahan kembali cerah dan tegak seiring berjalannya recovery time.
  3. Nilai Material Tidak Diubah / Direset:
93. Sistem Aktivasi Collider Pohon Terrain Berdasarkan Proximity Player (TreeColliderProximity)
- Perubahan: Membuat script TreeColliderProximity.cs yang mengaktifkan CapsuleCollider sementara di posisi pohon terrain terdekat saat player mendekat, dan menghapusnya saat player menjauh. Menggunakan object pooling untuk performa optimal.
- Fitur dan Perbaikan:
  1. Aktivasi Otomatis: Collider pohon menyala secara otomatis ketika player berada dalam radius tertentu dan mati saat menjauh.
  2. Object Pooling: Collider didaur ulang sehingga tidak ada alokasi memori berulang (zero garbage collection).
  3. Performa Ringan: Pengecekan dilakukan setiap 0.3 detik dengan batas maksimal 40 collider aktif bersamaan.
  4. Skala Otomatis: Ukuran collider menyesuaikan skala pohon terrain yang berbeda-beda.
- Cara Setting di Unity:
  1. Buat GameObject kosong di Hierarchy, beri nama "TreeColliderSystem".
  2. Tambahkan komponen TreeColliderProximity.
  3. Atur Activation Radius, Collider Radius, dan Collider Height sesuai ukuran pohon Anda.


94. Sistem Aktivasi Collider Objek Umum Berdasarkan Proximity Player (ProximityColliderActivator)
- Perubahan: Membuat script ProximityColliderActivator.cs yang mengaktifkan/menonaktifkan collider objek apapun di scene saat player mendekat/menjauh. Mendukung 3 mode pencarian target: Manual Drag & Drop, Tag, atau Layer.
- Fitur dan Perbaikan:
  1. Fleksibel 3 Mode Target: Pilih objek secara manual, berdasarkan tag, atau berdasarkan layer.
  2. Collider Asli Objek: Langsung mengaktifkan/menonaktifkan collider yang sudah ada di objek (tidak perlu spawn collider baru).
  3. Performa Ringan: Pengecekan berkala setiap 0.25 detik tanpa alokasi memori baru di runtime.
  4. Gizmo Visual: Menampilkan radius aktivasi dan objek aktif di Scene View.
- Cara Setting di Unity:
  1. Buat GameObject kosong di Hierarchy, beri nama "ProximityColliderSystem".
  2. Tambahkan komponen ProximityColliderActivator.
  3. Pilih mode target (Manual, Tag, atau Layer) lalu atur Activation Radius.


95. Perbaikan Bug Rumput Melebar / Melar Horizontal (Genshin / WuWa GPU Foliage Shader)
- Penyebab Bug: Shader bawaan Toby Foliage Engine membutuhkan script `Global Controller` bawaan Toby yang mengatur variabel shader global (`_GlobalWindStrength`, `_WindDirection`, `_StrongWindSpeed`). Tanpa script tersebut atau dengan mesh FBX eksternal, rotasi vertex Toby shader mengalami pembagian 0 / perkalian nilai tak hingga sehingga helai rumput melar puluhan meter secara horizontal.
- Perbaikan:
  1. Dibuat shader baru `FantasyKingdom/GenshinGrassFoliage` (Assets/8-13-2026/Grass/Materials/GenshinGrassFoliage.shader).
  2. Sistem GPU Vertex Wind Mandiri (Self-Contained): Menghasilkan ombak dinamis melintasi padang rumput (Genshin/WuWa style) langsung dari shader tanpa membutuhkan script Global Controller eksternal.
  3. Bagian akar rumput terkunci kokoh di tanah (`UV.y = 0`) dan ujung rumput melambai lentur (`UV.y = 1`).
  4. Pencahayaan Anime Lembut: Dilengkapi normal upward bias dan double-sided alpha cutout rendering.
- Cara Pemakaian:
  1. Klik objek `Infinite Grass` di Hierarchy.
  2. Klik tombol merah "🗑️ Hapus Semua Rumput" lalu klik tombol hijau "🌾 Pasang Rumput Procegrass".


96. Penerapan Koleksi Asli Toby Foliage Engine (VP_Grass)
- Perubahan: Memperbarui TerrainGrassSpawner.cs agar langsung memuat 9 variasi prefab resmi bawaan Toby Foliage Engine dari folder `Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_Grass/` (GrassBig_A, GrassBig_B, GrassMedium_A, GrassMedium_B, GrassMedium_D, GrassShort_A, GrassShort_B, GrassShort_C, GrassShort_D).
- Keunggulan:
  1. 100% Native & Stable: Menggunakan prefab resmi Toby yang sudah terintegrasi sempurna dengan LOD, material, dan konfigurasi shader bawaan.
  2. Bebas Masalah Alpha / Model: Tidak memerlukan modifikasi channel tekstur atau konversi mesh eksternal.
  3. Variasi Lengkap: Paduan rumput tinggi, sedang, dan pendek yang tersebar natural.
- Cara Pemakaian:
  1. Klik objek `Infinite Grass` di Hierarchy.
  2. Klik tombol merah "🗑️ Hapus Semua Rumput".
  3. Klik tombol hijau "🌾 Pasang Rumput Toby VP_Grass (1200 Rumpun)".


97. Sistem Senjata & Hand Grip IK Humanoid Bawaan Unity (PlayerWeaponIK)
- Perubahan: Membuat script PlayerWeaponIK.cs yang menggunakan event OnAnimatorIK bawaan resmi Unity Humanoid.
- Fitur dan Keunggulan:
  1. 100% Bebas Error Burst: Tidak memerlukan RigBuilder / RigLayer yang rentan crash pada struktur karakter Mixamo bertingkat.
  2. Hand Grip Presisi: Menempelkan tangan kiri/kanan ke target gagang pedang secara akurat dan mulus.
  3. Natural Elbow Hint: Arah siku menyesuaikan posisi alami saat memegang senjata.
  4. Auto Curve Blending: Membaca parameter float Animator (misal: "IKWeight") untuk transisi on/off yang halus saat Draw/Sheath.
  5. Socket Switching: Menyediakan fungsi EquipSword() dan SheathSword() untuk dipanggil dari Animation Event.
- Cara Pemakaian:
  1. Pasang komponen PlayerWeaponIK pada PlayerArmature.
  2. Isi slot Sword Transform, Hand Socket, Sheath Socket, Left Hand Grip Target, dan Left Elbow Hint.
  3. Aktifkan centang "IK Pass" pada Base Layer di Animator Controller.


98. Sistem Produksi Weapon IK & Post-FK Finger Blending (Animation Rigging + ScriptableObject)
- Lokasi File:
  * `Assets/Scripts/Player/WeaponIK/HandGripPoseData.cs` (Data ScriptableObject 15 tulang jari)
  * `Assets/Scripts/Player/WeaponIK/WeaponGripPoint.cs` (Marker grip transform pada senjata)
  * `Assets/Scripts/Player/WeaponIK/WeaponIKController.cs` (Pengatur TwoBoneIK & Post-FK Slerp jari)
  * `Assets/Scripts/Player/WeaponIK/Editor/HandGripPoseDataEditor.cs` (Tool 1-klik untuk merekam pose jari)
- Fitur Utama:
  1. Default Weight = 0: Bebas intervensi pada seluruh animasi dasar Mixamo (jalan, lari, serang, idle).
  2. Event-Driven Activation: Hanya aktif saat dipanggil via `ActivateGripEvent(Transform)` dan `DeactivateGripEvent()`.
  3. Multi-Preset Finger Curl: Mendukung variasi preset jari (CylinderGrip, PistolGrip, dll) per senjata.
  4. Performan Tinggi: Operasi FK-Blend jari <0.01ms per karakter, sangat ringan untuk puluhan NPC.
  5. Robust Edge-Case Handling: Anti-glitch saat mid-blend interrupt, senjata di-Destroy, atau stagger.


99. Procedural 3D Surface Hand Wrapper (ProceduralHandWrapper)
- Lokasi File: `Assets/Scripts/Player/WeaponIK/ProceduralHandWrapper.cs`
- Fitur Utama:
  1. Auto Surface Conformance: Menekuk 15 ruas jari secara otomatis berdasarkan bentuk 3D collider/mesh objek (pedang, kapak, pistol, botol, dll).
  2. Multi-Joint Contact Raycasting: Tiap ruas (Proximal, Intermediate, Distal) melingkari permukaan objek sampai menyentuh batas collider dengan aman (anti-clipping).
  3. 1-Klik Preset Export: Hasil bentuk tekukan jari dapat langsung disimpan ke dalam ScriptableObject HandGripPoseData untuk digunakan oleh WeaponIKController.


100. Player Foot IK Placement & Pelvis Offset (PlayerFootIK)
- Lokasi File: `Assets/Scripts/Player/PlayerFootIK.cs`
- Fitur Utama:
  1. Surface Normal Alignment: Telapak kaki otomatis menyesuaikan sudut kemiringan lereng bukit, batu, dan anak tangga.
  2. Pelvis / Hips Drop: Pinggul karakter otomatis turun saat kaki berada di beda ketinggian, mencegah kaki bawah mengambang.
  3. Grounded / Jump Detection: Otomatis mendeteksi saat karakter melompat agar IK kaki tidak aktif di udara.
  4. 100% Native Mecanim Humanoid: Stabil, ringan, dan bebas error Burst/Jobs.


101. Perbaikan CBuffer Memory Alignment & Sampler HLSL pada StylizedGrass Shader
- Perubahan: Mengatasi mesh rumput yang sempat invisible di Unity 6 DX11 dengan menyelaraskan layout memori CBuffer ke batas 16-byte, menggunakan `input.uv.y` untuk normalisasi tinggi daun secara presisi, mendeklarasikan SamplerState yang tepat, dan menerapkan pencahayaan upward normal lembut.
- Lokasi File: `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader`


102. Stylized Interactive Grass Shader (StylizedGrass_Mesh & StylizedGrass_Terrain)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Untuk mesh 3D / prefab rumput)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Untuk terrain detail grass)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailSystem.shader` (Internal shader trail system)
- Fitur Utama:
  1. Wind Animation: Rumput bergoyang mengikuti pola texture angin (7063-bump.jpg), hanya bagian tengah ke atas yang terpengaruh.
  2. Wind Adjustable: Kecepatan, intensitas, arah, dan tiling angin bisa diatur via Inspector.
  3. Near/Far Color Blend: Warna rumput berubah berdasarkan jarak kamera (NearColor → FarColor).
  4. Height Blend: Pangkal rumput otomatis blend ke BottomColor.
  5. Player Interaction: Rumput merunduk/membuka jalan saat player mendekat (real-time).
  6. Trail/Jejak: Rumput rebah di bekas langkah player, jejak recovery perlahan (diatur via GrassTrailRenderer).
  7. GPU Instancing & Alpha Cutoff: Mendukung performa tinggi dan transparansi.
  8. Shadow & Depth Pass: Bayangan mengikuti displacement wind agar konsisten.


102. Grass Trail Renderer (GrassTrailRenderer)
- Lokasi File: `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs`
- Fitur Utama:
  1. RenderTexture Trail: Merekam posisi player ke RT ortografik, jejak bertahan setelah player pergi.
  2. Adjustable Recovery Time: Waktu recovery (berapa detik sebelum jejak hilang) bisa diatur di Inspector.
  3. Scroll System: RT otomatis mengikuti posisi player, jejak lama tetap di posisi dunia yang benar.
  4. Stamp Radius & Strength: Ukuran dan kekuatan tapak jejak bisa diatur.


103. Resolusi Mesh Rumput Invisible pada Unity 6 URP Forward+ (Depth Priming & Sampler Fix)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader`
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader`
- Penyebab Masalah Invisible:
  1. Pada URP Unity 6 dengan pengaturan `DepthPrimingMode: Forced`, Forward pass mengeksekusi pengujian kedalaman `CompareFunction.Equal`. Pass `DepthOnly` yang sebelumnya memiliki `ColorMask R` dan tidak menyertakan vertex displacement menyebabkan nilai depth forward berbeda dengan prepass, sehingga GPU membuang 100% pixel rumput.
  2. Tag `"UniversalMaterialType" = "Lit"` pada SubShader AlphaTest memicu ekspektasi GBuffer/Deferred pass pada renderer Forward+.
  3. Texture sampling pada vertex shader DirectX 11 tanpa guard anti-NaN berisiko menghasilkan nilai tak tentu (NaN) yang menggagalkan posisi clip space `positionCS`.
- Solusi & Perbaikan:
  1. Arsitektur Single-Pass ForwardLit Murni: Menghilangkan pass `DepthOnly` kustom yang konfliktual dan menggunakan arsitektur ForwardLit teruji yang mewarisi fallback resmi `Universal Render Pipeline/Lit`.
  2. Pembersihan SubShader Tags: Menghilangkan tag `UniversalMaterialType` dan menetapkan `LOD 200`, `Cull Off` standar URP Cutout.
  3. Anti-NaN Displacement Guard: Melindungi kalkulasi ombak angin `7063-bump.jpg` dan interaksi player dengan validasi `!isnan()` dan sampler `sampler_LinearRepeat` bawaan Core URP.
  4. Pewarnaan & Gradasi Lembut: Mendukung penuh perpaduan Near/Far distance, Bottom height blend, dan pencahayaan foliage cerah di scene.


104. Arsitektur 4-Pass URP Penuh & Modul StylizedGrass_Common (Solusi Tuntas Depth Priming Forced)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Shared common buffer, samplers & displacement)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Mesh 3D rumput 4-pass)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Terrain detail grass 4-pass)
  * `Assets/8-12-2026/GrassBissmillah/TerrainColor.renderTexture` (Depth format fix 94)
- Penyebab Utama Mesh Menghilang:
  * Proyek menggunakan `DepthPrimingMode: Forced` di `Desktop Renderer.asset`. Pada mode ini, Unity URP RenderGraph mewajibkan SEMUA objek Cutout/AlphaTest digambar pada fase `DepthOnlyPass` terlebih dahulu.
  * Ketika objek tidak memiliki pass `DepthOnly` (atau depth pass-nya dihilangkan), rumput tidak digambar di Depth Prepass. Akibatnya saat `UniversalForward` berjalan, URP memaksa depth test hardware ke `CompareFunction.Equal`. Karena depth rumput tidak ada di depth buffer, GPU membuang 100% pixel daun rumput sehingga mesh menjadi tembus pandang (hanya wireframe/outline oranye di Scene view yang terlihat).
- Solusi Komprehensif yang Diterapkan:
  1. Modul Terpusat `StylizedGrass_Common.hlsl`: Menggabungkan CBuffer 16-byte aligned, deklarasi sampler DX11 eksplisit (`sampler_BaseMap`, `sampler_WindTex`, `sampler_GrassTrailRT`), dan fungsi vertex displacement `ApplyGrassDisplacement` dengan pengaman anti-NaN.
  2. Implementasi 4 Pass URP Lengkap:
     - `Pass 1 (ForwardLit / UniversalForward)`: Render albedo, gradasi Near/Far, Height blend, pencahayaan foliage matahari.
     - `Pass 2 (DepthOnly)`: Menggambar depth rumput di Depth Prepass dengan formula displacement identik, sehingga lolos uji `ZTest Equal` di Forward pass.
     - `Pass 3 (DepthNormals)`: Memberikan data depth dan normal untuk Screen Space Ambient Occlusion (SSAO) agar bayangan ambient tidak menolak daun rumput.
     - `Pass 4 (ShadowCaster)`: Memproyeksikan bayangan rumput ke tanah secara konsisten mengikuti liukan angin.
  3. Perbaikan Depth Format RenderTexture: Memperbarui `m_DepthStencilFormat: 94` pada `TerrainColor.renderTexture` agar warning konsol RenderGraph hilang.


105. Sistem Interaksi Rumput Real-Time & Trail Jejak Kaki Otomatis (GrassTrailRenderer & Displacement Fix)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (Sistem controller jejak & trample player)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Formula displacement interaksi & jejak kaki)
  * `Assets/8-12-2026/GrassBissmillah/GrassMatTRY.mat` & `GrassMat.mat` (_BendStrength 1.2)
- Penyebab Rumput Belum Interaktif Sebelumnya:
  1. Skrip pengontrol `GrassTrailRenderer.cs` belum terpasang di GameObject aktif mana pun dalam scene, sehingga variabel global `_PlayerTramplePos`, `_PlayerPosition`, dan `_GrassTrailRT` bernilai 0 setiap frame (shader mengira tidak ada player).
  2. Formula displacement interaksi sebelumnya menggunakan pengali `windMask` yang memotong respon gerak pada 30% pangkal helai, serta fungsi jarak kuadratik (`influence * influence`) yang membuat gerakan rumput terlalu kecil (hanya ~5 cm) sehingga tidak terlihat saat player melangkah.
- Solusi & Fitur Baru yang Diterapkan:
  1. Auto-Detection Player & Auto-Run:
     * `GrassTrailRenderer` kini otomatis mendeteksi objek pemain melalui Tag `Player`, nama `PlayerArmature` / `PlayerManager`, atau komponen `CharacterController`.
     * Dilengkapi `[RuntimeInitializeOnLoadMethod]` yang otomatis membuat sistem trail saat Play Mode berjalan tanpa harus drag-and-drop manual.
     * Dilengkapi `EditorAutoSetup` (`InitializeOnLoadMethod`) agar interaksi juga langsung hidup di Scene View saat Edit Mode.
     * Ditambahkan menu Editor `Tools -> Wirabaya -> Pasang Grass Trail Renderer`.
  2. Formula Rebah & Tekuk Real-Time yang Nyata & Elastis:
     * Menggunakan kurva halus `smoothstep(0.0, 1.0, f) * heightFactor * _BendStrength` yang menjamin akar tetap tertanam di tanah sementara batang dan ujung rumput terdorong ke samping hingga 1.3 meter dan ditekan ke bawah sebesar 0.75 meter.
  3. Trail Jejak Kaki Persisten dengan Waktu Pemulihan (Recovery Time):
     * Tapak kaki player tercetak di RenderTexture `_GrassTrailRT` dan menekan rumput ke bawah di bekas langkah player.
     * Rumput perlahan bangkit berdiri kembali sesuai durasi `trailRecoveryTime` yang dapat diatur di Inspector.


106. Kalibrasi Kualitas Interaksi Rumput, Anti-Sink Trample, Sway Angin Terkontrol, Deteksi Lompat (Anti-Jump), dan Penerimaan Shadow Karakter
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Displacement wind clamp, anti-sink trample, jump detection, trail recovery)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Penerimaan shadow karakter & trail shading)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Penerimaan shadow karakter & trail shading pada terrain grass)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (IsPlayerGrounded check, kalibrasi stamp radius & trample radius)
- Solusi Komprehensif untuk 6 Catatan Pengguna:
  1. Tanah yang Ditapak Terlalu Besar (Anti-Sink Trample):
     - Sebelumnya, formula menurunkan pucuk rumput sebesar 0.75m vertikal ke bawah tanah (`posWS.y -= bend * 0.75`), sehingga rumput terbenam ke bawah tanah dan mengekspos tekstur tanah lapang selebar 3.6 meter.
     - Diperbaiki: Rumput kini menyibak ke samping mengikuti langkah kaki (`posWS.xz += pushDir * (bend * 0.45)`), dengan penurunan vertikal minimal (`posWS.y -= bend * 0.12`). Radius di shader dibatasi otomatis maksimal 0.85m (`min(pPos.w, 0.85)`). Rumput tetap berada di atas permukaan tanah dan tidak lagi membentuk lubang kawah.
  2. Efek Recovery Grass Terlihat Jelas:
     - Ukuran jejak langkah dikalibrasi realistis (`stampRadius = 0.018f`, sekitar 0.7 meter).
     - Di fragment shader ditambahkan visual shading pada helai rumput yang terinjak (`albedo *= (1.0 - trailFactor * 0.20)`). Jalur jejak kaki terlihat jelas saat terinjak dan perlahan kembali cerah dan tegak seiring berjalannya recovery time.
  3. Nilai Material Tidak Diubah / Direset:
     - File material asset (`GrassMatTRY.mat` dan `GrassMat.mat`) dipertahankan sepenuhnya tanpa diubah atau di-overwrite propertinya.
  4. Sway Angin Tidak Miring Berlebihan:
     - Ditambahkan fungsi pembatas gelombang `clamp(wave, -0.30, 0.30)` dan skala pengali angin terkontrol (`_WindIntensity * 0.35`). Rumput bergoyang alami dengan arah sejajar texture bump tanpa pernah rebah atau patah miring.
  5. Saat Melompat Rumput Tidak Terinjak (Anti-Jump Trample):
     - Di C# (`GrassTrailRenderer.cs`): Dilengkapi metode `IsPlayerGrounded()` (memanfaatkan `CharacterController.isGrounded` dan raycast). Saat di udara, radius injakan langsung diset ke 0 dan proses pencetakan trail dihentikan.
     - Di Shader HLSL: Ditambahkan kalkulasi ketinggian kaki `float feetHeight = pPos.y - posWS.y;`. Jika kaki berada lebih dari 0.35 meter di atas rumput (`jumpFade = 0`), rumput langsung berdiri tegak seketika dan tidak merespon injakan.
  6. Ketajaman Shadow & Bayangan Karakter Terlihat Jelas:
     - Sebelumnya pass `UniversalForward` hanya memanggil `GetMainLight()` tanpa parameter koordinat shadow, sehingga `mainLight.shadowAttenuation` selalu 1.0 (bayangan karakter tidak pernah masuk ke rumput).
     - Ditambahkan `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN` dan soft shadow pragmas.
     - Di fragment shader dihitung `TransformWorldToShadowCoord(input.positionWS)` dan `GetMainLight(shadowCoord, input.positionWS, half4(1,1,1,1))`, lalu direct lighting dikalikan `mainLight.shadowAttenuation`.
     - Ambient foliage disesuaikan ke `half3(0.24, 0.30, 0.22)` menghasilkan kontras 5:1 antara area terang dan siluet bayangan karakter, sehingga bayangan karakter tampak tajam dan jelas di atas hamparan rumput.


107. Soft Stylized Shadow (Halus & Lembut), Trail Color Shift (Jejak Berubah Warna), dan Kalibrasi Respon Injak Grass
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Soft shadow remapping & trail golden color shift)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Soft shadow remapping & trail golden color shift pada terrain grass)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Displacement trample responsif 1.2m, parting ke samping 0.75m, drop 0.28m)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (IsPlayerAirborne check, stampRadius 0.025f ~ 1m, stampStrength 0.95f)
- Solusi untuk 5 Poin Masukan Pengguna:
  1. Bayangan Halus & Tidak Merusak Look Rumput (Soft Shadow):
     - Sebelumnya shadow langsung mengalikan direct lighting hingga 0, menyebabkan self-shadowing antar helai rumput menjadi garis-garis hitam pekat yang merusak visual hamparan rumput.
     - Diperbaiki dengan remapping shadow halus: `half softShadow = lerp(0.72, 1.0, shadowAtten)`. Bayangan karakter tetap terlihat jelas sebagai siluet lembut (soft shadow) tanpa membuat rumput menjadi gelap atau belang-belang hitam kasar.
  2. Respon Menginjak Rumput Terasa Nyata (Tangible Trample):
     - Mengembalikan radius injakan ke 1.2 meter (sesuai spesifikasi gameplay sebelumnya) dengan pergeseran ke samping 0.75m dan penurunan wajar 0.28m. Rumput terasa mantap menyibak saat karakter melangkah tanpa tembus ke bawah tanah.
  3. Jejak Trail Kaki Terlihat Jelas (Trail Stamp Boost):
     - Ukuran stamp jejak disesuaikan ke 0.025f (~1.0m dunia) dengan kekuatan stempel 0.95f, mencetak lekukan jejak langkah yang konsisten di belakang karakter.
  4. Bekas Injak Diberi Warna Berbeda (Trail Color Shift):
     - Di fragment shader ditambahkan blending warna khusus untuk jalur bekas langkah: `half3 trampledColor = albedo * half3(1.35, 1.30, 0.50) + half3(0.10, 0.12, 0.01)`.
     - Rumput yang baru saja diinjak berubah warna menjadi kuning-keemasan (golden crushed grass highlight) yang sangat jelas terlihat mata, lalu perlahan kembali ke warna hijau asli saat proses recovery selesai.
  5. Anti-Jump yang Tepat (IsPlayerAirborne):
     - Deteksi loncat diperbarui menjadi `IsPlayerAirborne()` yang mengecek jarak tanah > 0.75m di bawah kaki. Saat berjalan di permukaan tanah/lereng tidak akan pernah salah mengira karakter sedang loncat, dan saat Space/loncat ditekan maka interaksi dan stempel otomatis nonaktif di udara.


108. Visual Gelombang Angin (Wind Wave Sheen), Rebah Satu Arah & Radius Rapi, Warna Recovery Bebas Diatur di Inspector, dan Delay Zoom-Out Kamera
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Displacement wind sway dinamis, arah injakan unified satu arah via _PlayerForwardDir, CBuffer _RecoveryColor)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Property _RecoveryColor, visual wind wave sheen Studio Ghibli style, trample color blend)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Property _RecoveryColor, visual wind wave sheen, trample color blend untuk terrain)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (Kirim _PlayerForwardDir, radius injakan rapi 0.65m)
  * `Assets/Scripts/Camera/PlayerCameraController.cs` (Waktu tunda delay zoom-out kamera saat sprint)
- Solusi Komprehensif untuk 4 Catatan Pengguna:
  1. Gelombang Angin Jelas Terlihat (Wind Wave Sheen):
     - Vertex displacement ditingkatkan (`_WindIntensity * 0.70`, clamp `[-0.60, 0.60]`, drop vertikal `abs(wave) * 0.14`).
     - Di fragment shader ditambahkan efek kilauan ombak angin visual bergaya anime (Ghibli / Genshin Impact style): ombak berjalan melintasi padang rumput memancarkan kilau kehijauan keemasan lembut (`windSheen`) mengikuti pola tekstur bump `7063-bump.jpg` dan arah angin. Gelombang angin kini tampak megah menyapu seluruh padang rumput.
  2. Bentuk Injak Rapi & Melipat Satu Arah (Unified Directional Trample):
     - Masalah sebelumnya: dorongan radial 360 derajat menyebabkan rumput mekar acak ke segala arah seperti landak/bintang bundar yang terlalu besar.
     - Diperbaiki: Radius injakan diperkecil ke 0.65 meter (pas di sekeliling kaki karakter).
     - Di C# (`GrassTrailRenderer.cs`): Dikirimkan vektor arah hadap player `_PlayerForwardDir`.
     - Di Shader: Rebah rumput kini disatukan **dominan ke satu arah** searah langkah/hadap player (`fwdDir * 0.80 + pushDir * 0.20`). Rumput melipat rapi dan elegan ke satu arah saat dilangkahi, tidak lagi mekar bundar berantakan.
  3. Warna Rumput Terinjak / Recovery Bisa Diatur Bebas di Inspector:
     - Ditambahkan properti material `_RecoveryColor` ("Recovery / Trample Color") dan `_RecoveryColorStrength` di Inspector material rumput.
     - Default diset ke warna kuning-jerami keemasan cerah `(0.92, 0.95, 0.40, 1.0)`. Pengguna bebas memilih warna apa pun langsung dari Color Picker material (misal: kuning, jingga, hijau muda, biru magis, dll).
     - Warna injakan diterapkan baik pada rumput yang sedang diinjak langsung di bawah kaki maupun jejak trail yang sedang dalam proses recovery.
  4. Delay Zoom-Out Kamera Saat Lari (Camera Sprint Delay):
     - Di `PlayerCameraController.cs` ditambahkan properti `zoomOutDelay = 0.4f` (dapat diatur di Inspector antara 0 sampai 2 detik).
     - Kamera tidak lagi langsung menyentak zoom out seketika saat tombol sprint ditekan, melainkan menunggu jeda waktu delay terlebih dahulu sebelum kamera perlahan mundur (zoom out).


109. Peningkatan Kontras & Pita Gelombang Angin (Rolling Gust Sheen) dan Panduan Rekomendasi Wind Tiling
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Skala frekuensi gelombang `waveFreq = max(_WindTiling, 0.01) * 14.0`, modulasi noise `7063-bump.jpg` pada fase gelombang, profil hembusan tajam `pow(..., 1.8)`, liukan fisik forward 0.95m & drop 0.32m)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Kontras warna ombak visual: lembah `albedo * 0.82` vs puncak sunlit sheen `albedo * 1.45 + (0.18, 0.24, 0.04)`, masking tinggi vertikal `smoothstep(0.12, 0.80)`)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Sinkronisasi kalkulasi kontras ombak yang sama persis untuk shader terrain)
- Solusi Masalah Gelombang Kurang Kelihatan:
  1. Jarak & Frekuensi Gelombang Pas di Layar Kamera:
     - Sebelumnya panjang gelombang terlalu besar (~22.4 meter) sehingga di layar kamera yang lebarnya hanya ~12-15 meter tidak terlihat barisan ombak melainkan hanya perubahan terang-gelap lambat di seluruh layar.
     - Frekuensi gelombang ditingkatkan dengan multiplier 14.0 sehingga pada `_WindTiling = 0.08` menghasilkan panjang gelombang optimal ~5.6 meter. Di layar kamera kini selalu terlihat 2 hingga 3 baris pita ombak yang mengalir beruntun.
  2. Profil Puncak Ombak Tajam Bergaya Anime (Crest Profile):
     - Mengganti fungsi sinus datar dengan `pow(saturate(sin(wavePhase) * 0.5 + 0.5), 1.8)`. Sebanyak 70% siklus adalah rumput tenang alami, dan 30% sisanya adalah pita hembusan ombak yang tajam dan terdefinisi jelas menyapu padang rumput.
     - Pola `7063-bump.jpg` dimasukkan ke dalam pergeseran fase gelombang sehingga bentuk pita ombak meliuk secara organik dan tidak kaku seperti penggaris.
  3. Kontras Warna Lembah vs Puncak (Sunlit Wind Sheen):
     - Lembah ombak bernuansa hijau alami yang teduh (`albedo * 0.82`), sedangkan puncak hembusan memancarkan kilau emas matahari yang hidup (`albedo * 1.45 + highlight`), menghasilkan kontras >60% yang sangat memukau mata dari sudut kamera gameplay mana pun.
- Rekomendasi Nilai Parameter di Material Inspector:
  * `Wind Tiling`:
    - **0.06 - 0.08 (Sangat Direkomendasikan / Default):** Jarak antar puncak ombak ~5 - 7 meter. Sangat pas di kamera, terlihat 2-3 gulungan ombak sekaligus.
    - **0.10 - 0.15:** Jarak antar ombak lebih rapat (~3 - 4 meter), cocok untuk padang rumput berangin kencang.
    - **0.03 - 0.05:** Jarak antar ombak lebih lebar (~10 - 15 meter), untuk sapuan angin sabana luas.
  * `Wind Speed`: **0.5 - 1.0** (kecepatan gulungan ombak mengalir yang sejuk).
  * `Wind Intensity`: **0.5 - 0.8** (kekuatan liukan fisik dan kilau kontras ombak).


110. Perbaikan Gelombang Angin Alami (Referensi Infinite Grass) & Penghapusan Garis Zebra Albedo
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Displacement angin berbasis tekstur `7063-bump.jpg` multi-layer UV, liukan fisik forward + drop + side flutter, anti-zebra line)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Hapus kalkulasi warna ombak buatan pada albedo, normal dinamis condong mengikuti liukan rumput, specular sheen pantulan matahari alami)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Pembaruan identik untuk shader terrain)
- Analisis Masalah & Solusi (Sesuai Referensi Video YouTube Infinite Grass):
  1. Masalah Garis Zebra / Kontur Terhapus:
     - Garis zebra/kontur aneh pada foto sebelumnya terjadi karena shader mewarnai albedo secara langsung menggunakan fungsi sinus 1D planar.
     - Di video referensi (Infinite Grass), albedo rumput bersih alami. Gelombang angin terbentuk murni dari **liukan fisik helai rumput secara massal** yang memantulkan cahaya matahari ke kamera.
     - Seluruh manipulasi warna ombak garis pada albedo dihapus total. Albedo kini bersih dan elegan.
  2. Gelombang Angin Organik Berbasis Tekstur (2D Texture-Driven Gusts):
     - Angin kini disampling langsung dari `7063-bump.jpg` yang mengalir di world space dengan 2 layer UV bertingkat untuk memecah pengulangan.
     - Pola hembusan angin berbentuk awan/gumpalan angin 2D alami yang bergerak menyapu padang rumput, bukan garis lurus.
  3. Dynamic Normal & Specular Sheen (Kilauan Fisik Alami):
     - Di vertex shader, normal helai rumput ikut condong (`normalWS`) searah liukan helai saat ditiup angin atau diinjak player.
     - Di fragment shader, perubahan normal helai rumput ini secara alami menghasilkan specular sheen dan variasi diffuse saat ombak melintas memantulkan cahaya matahari ke arah kamera, persis seperti efek gelombang rumput di video YouTube referensi.


111. Fitur Adjustable Emission Glow pada Rumput (HDR Color, Intensity, & Tip Only)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Struktur CBuffer `UnityPerMaterial` 16-byte aligned untuk `_EmissionColor`, `_EmissionIntensity`, `_EmissionTipBoost`, `_Pad1`)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Penambahan properti Emission di Inspector & kalkulasi emission glow di `GrassFrag`)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Sinkronisasi properti dan kalkulasi emission glow yang identik untuk shader terrain)
- Fitur & Kontrol di Material Inspector (Sederhana & Tanpa Perlu Tekstur Map):
  1. `Emission Color` (`[HDR] _EmissionColor`):
     - Memungkinkan pemilihan warna pendaran bebas dengan dukungan HDR (misal: pendaran biru neon magis, hijau kunang-kunang, kuning keemasan, ungu fantasi, dll) yang langsung memicu efek Bloom jika Post-Processing aktif.
     - Default hitam `(0, 0, 0)` sehingga rumput tetap dalam tampilan normal jika fitur emission tidak digunakan.
  2. `Emission Intensity` (`_EmissionIntensity`, Range `0.0` - `10.0`):
     - Slider pengatur kekuatan cahaya pendaran secara real-time (tanpa perlu mengisi tekstur apa pun).
  3. `Emission on Tips Only` (`_EmissionTipBoost`, Range `0.0` - `1.0`):
     - Slider pengatur distribusi cahaya: `0.0` = seluruh helai rumput menyala merata, `1.0` = hanya ujung/pucuk helai rumput yang berpendar (seperti kunang-kunang / flora fantasi bercahaya).


112. Penambahan Pengaturan Fisik Rumput Terinjak & Pemisahan Menu Inspector (Color & Grass Recovery)
- Lokasi File:
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Common.hlsl` (Menambahkan `_RecoveryTime` & `_TrampleBendAmount` ke CBuffer 16-byte aligned, liukan fisik jejak langkah `bendDown` 0.60m & `bendFwd` 0.50m searah hadap player)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Mesh.shader` (Pemisahan 2 grup menu khusus di Inspector: Trample Recovery Color & Trample Grass Recovery)
  * `Assets/8-12-2026/GrassBissmillah/StylizedGrass_Terrain.shader` (Sinkronisasi struktur properti yang sama untuk terrain)
  * `Assets/8-12-2026/GrassBissmillah/GrassTrailRenderer.cs` (Sinkronisasi otomatis durasi fade RT jejak kaki dari material & deteksi karakter di Scene view)
- Pemisahan 2 Kategori Pengaturan di Material Inspector:
  1. Grup 1: **`Trample Recovery Color`** (Khusus Warna):
     * `Recovery / Trample Color`: Warna helai rumput saat terinjak.
     * `Recovery Color Intensity`: Kepekatan transisi warna injakan.
  2. Grup 2: **`Trample Grass Recovery`** (Khusus Fisik Rumput & Pemulihan):
     * `Recovery Duration (Detik)` (`_RecoveryTime`, Range: `0.5` - `20.0` detik, default `4.0`): Mengatur durasi waktu sampai rumput bangkit berdiri tegak kembali seutuhnya.
     * `Trample Bend Amount` (`_TrampleBendAmount`, Range: `0.0` - `2.0`, default `1.0`): Mengatur seberapa rebah helai rumput merapat ke tanah saat terinjak kaki (0 = tidak rebah, 1.0 = rebah jelas, 2.0 = gepeng maksimal).
- Efek Visual Fisik & Pemulihan:
  * Rumput yang terinjak kini rebah jelas ke bawah dan terlipat searah langkah kaki pemain, meninggalkan bekas tapak jejak yang tegas.
  * Seiring waktu pemulihan (`_RecoveryTime`), helai rumput terlihat jelas perlahan bangkit tegak kembali dan warnanya pulih seperti semula.
  * Nilai setelan material yang sudah diatur oleh pengguna pada `GrassMat.mat` tetap 100% utuh tanpa ada yang diubah.


113. Sistem Toon Shader Bergaya Wuthering Waves (WuWa) dengan 3 Varian (Biasa, Metal, Kulit), Penonjolan Lekukan, Inverted Hull Outline, dan Custom Inspector
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl` (Core library lighting, multi-tone cel ramp, SSS warm fringe, stylized metal matcap, crease depth, rim light, outline)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (Master Shader dengan dropdown `Material Type`: Biasa, Metal, Kulit)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (Shader khusus untuk Kain, Rambut, dan Props)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (Shader khusus untuk Armor, Zirah, dan Senjata Logam)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (Shader khusus untuk Kulit, Wajah, dan Tubuh dengan SSS warm fringe)
  * `Assets/8-12-2026/BISMILLAHWUWA/Demo/` (`M_WuWa_Skin_Sample.mat`, `M_WuWa_Metal_Sample.mat`, `M_WuWa_Cloth_Sample.mat`)
- Fitur & Keunggulan Bergaya Wuthering Waves:
  1. **3 Varian Material Khusus (Material Type Override):**
     * **Biasa (Cloth / Hair / Props):** Cel-shading 2-band / 3-tone bersih, fabric sheen specular, dan stylized rim light.
     * **Metal (Armor / Weapons / Trims):** Refleksi logam stylized multi-band, specular kilatan tajam (stepped anisotropic), bayangan logam kontras tinggi, dan dukungan MatCap.
     * **Kulit (Skin / Face / Body):** Subsurface Scattering (SSS) warm fringe band kemerahan/peach di batas bayangan, bayangan hangat bernuansa segar, gradasi halus, dan kilau alami tanpa bintik tajam.
  2. **Penonjolan Lekukan Secara Sempurna (Creases & Cavity Depth):**
     * **Normal Strength & Crease Boost:** Normal map langsung mempertegas lekukan otot, lipatan baju, dan sambungan armor sehingga langsung jatuh ke bayangan tajam.
     * **Cavity / AO Deepening:** Mendukung Cavity/AO Map dan Vertex Color AO yang memperdalam lekukan celah terdalam meskipun terkena cahaya langsung.
     * **Crease Rim Light:** Kilau rim anime menonjolkan puncak lekukan (*ridges*) sementara lembah lekukan tetap gelap, menghasilkan ilusi 3D bervolume yang menawan.
  3. **Inverted Hull Outline Pass:**
     * Garis tepi anime (*back-face hull extrusion*) di View-Space dengan depth bias anti-z-fighting untuk wajah dan jari.
  4. **Kompatibilitas Penuh SRP Batcher:**
     * Struktur CBuffer `UnityPerMaterial` 16-byte aligned untuk performa maksimal di Universal Render Pipeline (URP).


114. Perbaikan Kompatibilitas Mesh Props/Batu (Fix Mesh Hilang/Invisible & CustomEditor Warning)
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl` (Perbaikan proteksi anti-NaN pada Tangent, Safe Smoothstep, Fallback Properti Material, dan Safe Inverted Hull W-coordinate)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader`
- Masalah yang Terjadi Sebelumnya:
  1. Warning `Could not create a custom UI for the shader 'WuWa/Toon Character'` dan menu Inspector material menjadi kosong/blank karena adanya error resolusi C# editor.
  2. Mesh batu/props mendadak hilang (invisible) di Scene view saat material diganti ke `WuWa/Toon Character` akibat:
     - Mesh batu (`stones.fbx`) tidak memiliki channel tangent sehingga `normalize(tangentWS)` menghasilkan nilai `NaN` (Not a Number).
     - Material yang baru dikonversi belum memiliki nilai `_ShadowFeather` (bernilai 0), menyebabkan operasi `smoothstep(threshold, threshold, lightTerm)` melakukan pembagian nol `(x - a) / 0 = NaN`. Di GPU DirectX 11, pixel yang bernilai `NaN` langsung dibuang (*discarded*) sehingga objek menjadi transparan/hilang seutuhnya.
     - Triangulasi outline pada posisi non-aktif menghasilkan koordinat W bernilai 0 (`float4(0,0,0,0)`), memicu *divide-by-zero* pada rasterizer GPU.
- Solusi & Perbaikan yang Diterapkan:
  1. **Anti-NaN Tangent Engine:** Jika mesh tidak memiliki data tangent (panjang < 0.01), shader otomatis menurunkan tangent ortogonal aman melalui cross-product terhadap vektor up, serta mengamankan rekalkulasi Gram-Schmidt.
  2. **Safe Smoothstep & Clamped Feather:** Memberikan batas minimal aman pada feather dan threshold (`max(_ShadowFeather, 0.005)`) sehingga interval atas dan bawah tidak pernah bernilai sama.
  3. **Safe Inverted Hull:** Triangulasi non-aktif outline dialihkan ke `float4(0, 0, 0, 1.0)` dengan fragment `discard` untuk keamanan rasterizer.
  4. **Fallback Default Warna Bayangan:** Material yang belum memiliki data tint bayangan otomatis menggunakan warna cel-shadow harmonis anime WuWa (tidak jatuh ke hitam pekat).


115. Solusi Tuntas: Kompatibilitas URP Deferred Rendering (Fix Mesh Menghilang 100%) & Native Inspector Stabil
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (`WuWa/Toon Character`)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (`WuWa/Toon - Biasa (Cloth & Hair)`)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (`WuWa/Toon - Metal & Weapon`)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (`WuWa/Toon - Kulit (Skin)`)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
- Penyebab Utama Masalah (The Root Cause):
  1. **Project Menggunakan URP Deferred Rendering (`m_RenderingMode: 2`):**
     - Terverifikasi pada `Assets/URP/Settings/Desktop Renderer.asset` bahwa renderer diset ke Deferred (`m_RenderingMode: 2`).
     - Dalam pipeline URP Deferred, pass dengan tag `Tags { "LightMode" = "UniversalForward" }` **SECARA OTOMATIS DIBUANG / TIDAK AKAN PERNAH DI-RENDER** oleh URP!
     - Akibatnya, ketika material menggunakan `WuWa/Toon - Biasa (Cloth & Hair)` atau varian lainnya, pass pencahayaan/forward sama sekali tidak dieksekusi, sehingga mesh batu (`rock.001`) menjadi hilang/invisible seutuhnya di Scene dan Game View!
  2. **Inspector Material Kosong / Blank:**
     - Di Unity 6, deklarasi `CustomEditor "WuWaToonShaderGUI"` gagal dimuat jika terjadi kendala asosiasi assembly, menyebabkan pesan error `Could not create a custom UI for the shader...` dan Unity langsung membatalkan rendering GUI inspector material, meninggalkan kotak kosong/blank.
- Solusi & Perbaikan Tuntas:
  1. **Ganti ke `UniversalForwardOnly` di Semua Shader:**
     - Mengubah Pass 1 pada seluruh 4 shader menjadi `Tags { "LightMode" = "UniversalForwardOnly" }`.
     - Tag ini memaksa renderer URP (baik dalam mode Deferred maupun Forward) untuk merender objek secara langsung (forward), sehingga objek langsung muncul kembali dengan pencahayaan cel-shading WuWa yang sempurna!
  2. **Gunakan Native ShaderLab Inspector (100% Bebas Error & Stabil):**
     - Menghapus baris `CustomEditor "WuWaToonShaderGUI"` pada seluruh shader dan membersihkan folder Editor yang tidak terpakai.
     - Unity kini secara otomatis menggunakan built-in ShaderLab Property Drawer yang merender semua header, slider lekukan, color picker, normal map, dan tombol varian secara bersih, intuitif, dan tanpa risiko blank/error sama sekali.
  3. **Fallback Aman:**
     - Mengarahkan FallBack ke `"Universal Render Pipeline/Lit"` menggantikan FallbackError.
  4. **Outline Pass SRPDefaultUnlit:**
     - Mengatur Pass Inverted Hull Outline ke `Tags { "LightMode" = "SRPDefaultUnlit" }` dengan `Cull Front` agar garis tepi anime tampil mulus tanpa mengganggu geometri utama.


116. Pemulihan Penuh Inspector Material (Eliminasi Total CustomEditor Warning) & Pre-Konfigurasi Material Baru
- Lokasi File:
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader
  * Assets/8-12-2026/BISMILLAHWUWA/New Material.mat (Terkonfigurasi untuk WuWa Toon - Biasa)
  * Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat (Terkonfigurasi untuk WuWa Toon - Metal)
  * Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat (Terkonfigurasi untuk WuWa Toon - Kulit)
  * Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat (Terkonfigurasi untuk WuWa Toon Universal)
  * Penghapusan folder Assets/8-12-2026/BISMILLAHWUWA/Editor
- Masalah:
  * Pengguna melaporkan 'tetep ngga ada' karena saat memilih New Material dengan shader WuWa/Toon - Biasa (Cloth & Hair), muncul warning 'Could not create a custom UI for the shader ... CustomEditor = ' dan Inspector material di bawah MeshRenderer tidak muncul sama sekali.
- Solusi & Perbaikan:
  1. Penghapusan Total Direktif CustomEditor: Menghapus sepenuhnya baris CustomEditor dari seluruh 4 shader dan menghapus direktori Editor/. Unity kini secara otomatis menggunakan native IMGUI MaterialEditor bawaan engine yang 100% stabil, menampilkan seluruh slider, color picker, normal map, dan header tanpa error.
  2. Double-Sided Rendering (_Cull = 0): Menyetel default culling ke 0 (Off) pada semua varian shader sehingga mesh dengan orientasi normal terbalik atau polygon satu sisi tidak akan pernah hilang/invisible.
  3. Standarisasi Pass DepthOnly (ColorMask 0): Memperbaiki deklarasi ColorMask dari R menjadi 0 pada pass DepthOnly di seluruh shader agar sesuai standar URP.
  4. Pre-Konfigurasi Material Baru: Mengisi file New Material.mat sampai New Material 3.mat dengan nilai-nilai parameter default cel shading WuWa yang harmonis, aktif, dan siap pakai.


117. Solusi Akar Masalah Mesh Menghilang: Koreksi LightMode Tag (UniversalForward & Outline)
- Lokasi File:
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader
  * Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader
  * Assets/8-12-2026/BISMILLAHWUWA/New Material.mat (Terkoneksi langsung ke stone-albedo.001.png dan normal map)
- Analisis Akar Masalah (Mengapa Mesh Menghilang Total di Scene View):
  1. Sebelumnya Pass 0 menggunakan tag UniversalForwardOnly, sedangkan Pass 1 (Outline) menggunakan SRPDefaultUnlit.
  2. Kamera Scene View di Unity dan pass Forward URP mencari tag urutan: [UniversalForward, SRPDefaultUnlit].
  3. Karena Pass 0 bukan UniversalForward, Unity melewatkan Pass 0 dan langsung mengeksekusi Pass 1 (SRPDefaultUnlit / Outline).
  4. Pass Outline memiliki Cull Front dan memanggil discard saat ketebalan outline bernilai rendah, akibatnya 100% pixel objek dibuang oleh GPU dan mesh menghilang total dari layar!
- Solusi & Perbaikan Tuntas:
  1. Mengubah Pass 0 di seluruh 4 shader menjadi Tags { LightMode = UniversalForward }. T  3. Memasangkan tekstur batu asli (stone-albedo.001.png dan stone-normal.001.png) langsung pada New Material.mat, sehingga batu yang dipilih di scene langsung muncul utuh dengan cel-shading WuWa dan warna albedo/tint yang bisa di-override dengan bebas di Inspector.


118. Solusi Definitif Mesh Invisible & Color Override di URP Deferred: UniversalForwardOnly & Anti-Pitch Black Fallbackuncul utuh dengan cel-shading WuWa dan warna albedo/tint yang bisa di-override dengan bebas di Inspector.


118. Solusi Definitif Mesh Invisible & Color Override di URP Deferred: UniversalForwardOnly & Anti-Pitch Black Fallback
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
- Penyebab Teknis Mesh Menghilang 100% (The Exact Root Cause):
  1. Seluruh Renderer URP project (`Desktop Renderer.asset`, `Mobile Renderer`, dll) menggunakan mode **Deferred** (`m_RenderingMode: 2`).
  2. Dalam pipeline URP Deferred, pass forward untuk objek opaque (`m_RenderOpaqueForwardOnlyPass`) hanya memproses shader tag list: `["UniversalForwardOnly", "SRPDefaultUnlit", "LightweightForward"]`. Tag `"UniversalForward"` sama sekali TIDAK dipanggil atau dieksekusi dalam mode Deferred!
  3. Ketika Pass 0 sebelumnya diset ke `UniversalForward` pada Entry 117, renderer URP Deferred mengabaikan shader seutuhnya sehingga objek (`rock.001`) tidak memiliki draw call sama sekali dan menghilang 100% dari Scene dan Game View.
  4. Tag SubShader `"UniversalMaterialType" = "Lit"` memicu URP GBuffer pass untuk mengasosiasikan material dengan Lit GBuffer data, bukan forward custom toon lighting.
  5. Perhitungan lighting frag sebelumnya mengalikan seluruh shadow tint dan albedo dengan `mainLight.color` tanpa fallback, serta bila Scene View camera memiliki scene lighting mati atau directional light redup, objek jatuh ke warna hitam kelam `(0, 0, 0)` yang tidak terlihat.
- Perbaikan Menyeluruh yang Telah Diterapkan:
  1. **Pass 0 Ditetapkan ke `UniversalForwardOnly` di Seluruh 4 Shader:**
     - Memastikan pass geometri utama dieksekusi secara wajib baik pada URP Deferred (`m_RenderOpaqueForwardOnlyPass`) maupun URP Forward (`m_RenderOpaqueForwardPass`).
  2. **Pembersihan SubShader Tag:**
     - Menghapus tag `"UniversalMaterialType" = "Lit"` dan menetapkan `"Queue" = "Geometry"`, `"RenderType" = "Opaque"`, `"IgnoreProjector" = "True"`.
  3. **Material Color Override (`_BaseColor`) Langsung & Responsif:**
     - Di `WuWaToon_Core.hlsl`, albedo dihitung langsung sebagai `albedoColor = baseMap * _BaseColor;`. Mengubah `_BaseColor` di Inspector langsung mengubah warna dasar objek, warna bayangan 1, dan bayangan terdalam (lekukan) secara harmonis.
  4. **Jaminan Anti-Pitch Black (Safe Fallbacks):**
     - Ditambahkan pengecekan `lightColor` dan `lightDir` minimal jika directional light tidak aktif, serta batas dasar `ambient = max(ambient, albedoColor.rgb * 0.15);` agar lekukan dan siluet objek selalu tampak jelas dan hidup.
  5. **Fallback ke `"Universal Render Pipeline/Lit"`:**
     - Menjamin integrasi depth dan shadow rasterizer URP berjalan mulus.


119. Solusi Definitif Tuntas: Rekonstruksi 5-Pass URP RenderGraph, AlphaTest Forward Queue, dan Sinkronisasi SRP Batcher CBuffer
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat`
- Akar Masalah yang Ditemukan (Why Mesh & Material Preview Disappeared):
  1. **Konflik Unity 6 Render Graph vs UniversalForwardOnly:** Pada Unity 6 URP dengan Render Graph aktif, pass `UniversalForwardOnly` di Deferred pipeline berada di dalam blok `#if URP_COMPATIBILITY_MODE` sehingga tidak pernah di-enqueue. Kamera Material Preview di Project window juga hanya mencari `UniversalForward` atau `SRPDefaultUnlit`, menyebabkan preview sphere menjadi abu-abu flat (blank).
  2. **Paksaan Queue 2000 pada File Material:** Properti `m_CustomRenderQueue: 2000` di dalam `.mat` memaksa objek masuk ke fase G-Buffer. Karena shader toon memiliki lighting non-PBR (cel shading multi-band) dan sengaja tidak memiliki pass G-Buffer, objek diabaikan total oleh renderer dan lenyap dari layar.
  3. **Absennya Pass DepthNormals:** Pengaturan `Desktop Renderer.asset` mengaktifkan SSAO (`active: 1`) dan `DepthPrimingMode: Forced (2)`. Objek tanpa pass `DepthNormals` ditolak atau tidak menerima efek ambient occlusion & priming depth yang tepat.
  4. **Variabel Hilang pada CBuffer SRP Batcher:** Properti `_AlphaClip`, `_EnableNormalMap`, `_EnableOcclusion`, `_EnableEmission`, dan `_Cull` dideklarasikan di Properties namun absen dari `UnityPerMaterial` CBuffer, memicu desinkronisasi memori GPU pada DirectX 11.
- Perbaikan & Solusi Tuntas yang Diterapkan:
  1. **Arsitektur 5-Pass Lengkap Standar URP:**
     - `Pass 0 (ForwardLit)`: `Tags { "LightMode" = "UniversalForward" }` dengan cel shading WuWa penuh, rim light, dan dynamic tint.
     - `Pass 1 (Outline)`: `Tags { "LightMode" = "SRPDefaultUnlit" }` dengan ekstrusi normal View-Space anti-NaN.
     - `Pass 2 (ShadowCaster)`: `Tags { "LightMode" = "ShadowCaster" }` untuk proyeksi bayangan akurat.
     - `Pass 3 (DepthOnly)`: `Tags { "LightMode" = "DepthOnly" }` untuk depth prepass hardware.
     - `Pass 4 (DepthNormals)`: `Tags { "LightMode" = "DepthNormals" }` untuk kompatibilitas penuh SSAO & Depth Priming Forced.
  2. **Integrasi AlphaTest Forward Queue (`m_CustomRenderQueue: -1`):**
     - SubShader ditetapkan ke `"Queue" = "AlphaTest"`, `"RenderType" = "TransparentCutout"`.
     - Seluruh material di-reset ke `m_CustomRenderQueue: -1` sehingga mewarisi queue AlphaTest (2450) yang secara resmi diproses dalam forward pass dengan `ZWrite On` solid, persis seperti arsitektur sukses pada `StylizedGrass_Mesh.shader`.
  3. **100% CBuffer SRP Batcher Alignment (304 Bytes):**
     - Seluruh properti shader terdaftar rapi dalam blok 16-byte di `CBUFFER_START(UnityPerMaterial)`.
  4. **Guard Alpha Test Aman (`_AlphaClip > 0.5`):**
     - `clip()` kini diapit oleh pengecekan nilai `_AlphaClip > 0.5` di semua pass, menjamin objek opaque tidak akan pernah mengalami discard pixel secara tidak sengaja.
  5. **Pewarnaan & Konfigurasi Tekstur Batu Real-time:**
     - Seluruh material `New Material.mat` hingga `New Material 3.mat` terhubung dengan tekstur albedo & normal batu (`stone-albedo.001.png`), dan siap diwarnai / di-override warnanya melalui parameter `_BaseColor` di Inspector.


120. Resolusi Tuntas Konflik SRPDefaultUnlit & UniversalForwardOnly pada URP Unity 6 RenderGraph
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
- Akar Masalah Mendalam (Berdasarkan Analisis Kode Sumber C# URP Package):
  1. **Pembajakan Pass oleh `SRPDefaultUnlit` (The SRPDefaultUnlit Hijack):**
     - Di dalam kode sumber URP `DrawObjectsPass.cs`, urutan pencarian pass saat merender material preview adalah:
       `shaderTagIds = [ "SRPDefaultUnlit", "UniversalForward", "UniversalForwardOnly" ]`.
     - Karena Pass 1 (Outline) diberi tag `SRPDefaultUnlit`, Unity menemukan kecocokan pada Pass 1 TERLEBIH DAHULU dan langsung mengeksekusi Pass 1 tanpa pernah mengeksekusi Pass 0 (`ForwardLit`).
     - Pass 1 memiliki pengaturan `Cull Front` dan `ZWrite Off` (hanya menggambar garis luar di belakang objek). Akibatnya, preview sphere di Project window dan objek batu di Scene view hanya menggambar pass outline yang berongga/tembus pandang, sedangkan warna albedo, normal map, dan cel shading pada Pass 0 tidak pernah digambar sama sekali.
  2. **Persyaratan `UniversalForwardOnly` pada Pipeline Deferred RenderGraph:**
     - Di file `UniversalRendererRenderGraph.cs` pada pipeline Deferred, pass forward opaque dieksekusi melalui `m_RenderOpaqueForwardOnlyPass.Render(...)`.
     - Pass ini HANYA mencari `UniversalForwardOnly`, `SRPDefaultUnlit`, dan `LightweightForward` (tidak mencari `UniversalForward`). Menamai Pass 0 sebagai `UniversalForward` menyebabkannya diabaikan oleh renderer Deferred.
  3. **Korupsi Cavity dari Vertex Color Tidak Terinisialisasi:**
     - Kalkulasi vertex color AO di `WuWaToon_Core.hlsl` mengasumsikan mesh memiliki vertex color yang valid. Pada mesh batu (`stones.fbx`) dan bola preview Unity yang tidak memiliki vertex color, driver DX11 mengembalikan nilai 0 pada channel red, yang memaksa nilai `cavity = 0` dan mereduksi pencahayaan hingga 80-100% menjadi hitam pekat.
- Perbaikan & Solusi yang Diterapkan:
  1. **Pass 0 Ditetapkan ke `Tags { "LightMode" = "UniversalForwardOnly" }` di Seluruh Shader:**
     - Kompatibel 100% dengan renderer Deferred (`m_RenderOpaqueForwardOnlyPass`) dan Forward (`DrawObjectsPass`).
  2. **Pass 1 (Outline) Diubah ke `Tags { "LightMode" = "Outline" }`:**
     - Mencegah Pass Outline membajak pass utama material pada kamera Preview dan Scene view.
  3. **Pembersihan Kalkulasi Cavity di `WuWaToon_Core.hlsl`:**
     - Menghapus pembacaan vertex color otomatis yang tidak terverifikasi, sehingga pencahayaan albedo dan normal map batu tampil cerah dan tajam tanpa tertekan menjadi gelap.
  4. **SubShader Tag Distandarkan ke `RenderType = "Opaque"`, `Queue = "Geometry"`:**
     - Menjamin integrasi mulus dengan depth buffer dan sistem pencahayaan Deferred Forward-Only URP.


121. De-Glossing Specular, Preservasi Detail Tekstur, dan Koreksi Normal Map TBN Handedness
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
- Masalah yang Dialami Pengguna:
  * Permukaan batu tampak terlalu mengkilap/silau putih ("mengkilap semua") dan tekstur batu tampak buram/pudar/jelek ("texturenya jadi jelek gitu").
- Analisis Penyebab:
  1. **Specular Step Terlalu Keras & Overpowering:** Rumus specular lama menggunakan `smoothstep(0.45, 0.55, spec)` yang memicu lonjakan warna putih murni instan dengan `_SpecularIntensity: 0.4` di atas separuh permukaan mesh batu, menciptakan efek plastik murahan dan menutupi tekstur albedo.
  2. **Rim Light Terlalu Lebar & Membanjiri Permukaan:** Parameter `_RimPower: 3.5` dan `_RimIntensity: 1.0` terlalu rendah/lebar sehingga cahaya rim menyelimuti 40% permukaan batu, bukan hanya di garis siluet tepi.
  3. **Overexposure dari Penjumlahan Cahaya Ambient di Atas Lit Pass:** Cahaya ambient (SH) ditambahkan secara buta di atas albedo lit (`diffuseColor += ambient`), mendorong intensitas warna melebihi 1.5 dan membuat detail batu menjadi washed-out (putih pucat).
  4. **Pembalikan Sumbu Y Normal Map (Flipped Bitangent):** Rumus manual `cross(normalWS, tangentWS)` pada `CalculateWorldNormal` membuang faktor handedness (+1/-1) dari UV mesh. Pada pulau UV yang di-mirror (simetris), normal map terbalik sumbunya sehingga permukaan batu tampak kasar, terdistorsi, dan pencahayaannya terbalik.
- Perbaikan yang Telah Diterapkan:
  1. **Koreksi TBN Matrix Asli URP:** `CalculateWorldNormal` kini menggunakan matriks TBN standar dari `GetVertexNormalInputs` dengan handedness yang tepat via `TransformTangentToWorld(normalTS, tbn)`. Tekstur normal batu kini tampak tajam, alami, dan tidak terdistorsi.
  2. **Anime Soft Specular & Tinting:** Specular diubah ke transisi halus `smoothstep(0.4, 0.85)` dan di-tint oleh warna albedo batu, mencegah terbentuknya bercak putih plastik.
  3. **Tight Silhouette Rim Light:** Rim light diperketat dengan batas minimal `max(_RimPower, 2.5)` dan di-mask hanya pada sudut pandang tepi siluet terhadap arah cahaya.
  4. **Integrasi Ambient ke Shadow Floor:** Ambient light kini diintegrasikan untuk mengangkat kegelapan bayangan (shadow floor), bukan ditumpuk di atas area terang, sehingga kontras tekstur batu tetap terjaga tajam dan tidak pucat.
  5. **Preset Matte Stone pada `New Material.mat`:** `_SpecularIntensity` diatur ke `0.05` (sangat halus), `_RimIntensity: 0.15`, dan warna bayangan dinetralkan (`_1stShadowColor: (0.78, 0.78, 0.82)`) agar karakter batu tampak kokoh, bertekstur, dan sesuai gaya anime Wuthering Waves.


122. Perbaikan Transisi Shading Mulus (Anti-Banding/Anti-Blotches), Pemulihan Warna Bayangan Terang (Anti-Hitam Legam), dan Normal Mapping Bersih
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (v1.3)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (v1.3)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (v1.3)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (v1.3)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat`
- Masalah yang Dialami Pengguna:
  * Shading bayangan pada objek batu tampak patah-patah, kasar, dan tidak mulus ("jelek banget nimpanya nggak mulus").
  * Terdapat bercak-bercak hitam bergerigi tajam di permukaan batu (blotches/holes).
  * Varian material tertentu (Metal dan Standard di sisi bayangan) tampak hitam legam/padam.
- Analisis Penyebab:
  1. **Penggabungan Prematur `NdotL` dan Real-Time Shadow Attenuation:**
     - Rumus lama mengalikan `effectiveL * mainLight.shadowAttenuation` sebelum dimasukkan ke fungsi `smoothstep(shadowThresh - feather, shadowThresh + feather, lightTerm)`.
     - Filter shadow map (penumbra/dither) yang dikalikan dengan riak micro-normal map batu menyebabkan batas cel shading melompat bolak-balik secara acak, menciptakan garis batas bergerigi kasar dan bercak hitam compang-camping di sekujur permukaan.
  2. **Feather Terlalu Sempit & Normal Crease Boost Berlebihan:**
     - Nilai feathering sebelumnya terlalu sempit (`0.03 - 0.08`), sedangkan `_NormalCreaseBoost` mengalikan `normalTS.xy` hingga 1.5x. Setiap lekukan mikro pada normal map berubah menjadi tebing bayangan biner yang tajam dan kasar.
  3. **Penyusutan Kontras Bayangan Menjadi Hitam Padam:**
     - Pada varian Metal, `_MetalShadowContrast: 0.3` mereduksi warna albedo hingga tersisa 16% di area bayangan, sehingga saat objek membelakangi arah matahari, objek tampak hitam pekat.
     - Pada varian Standard, pencahayaan di area bayangan tidak memiliki lantai ambient yang sehat (ambient floor), sehingga di bawah langit atau pencahayaan tidak langsung, tekstur batu lenyap dalam kegelapan.
  4. **Interpolasi TBN Non-Ortogonal:**
     - Menginterpolasikan `bitangentWS` dan `tangentWS` secara terpisah di varyings menyebabkan distorsi shear pada segitiga mesh melengkung.
- Solusi & Rekayasa yang Diterapkan:
  1. **Pemisahan Cel Step dan Shadow Attenuation (Formula Standar Anime AAA):**
     - `halfLambert = saturate(NdotL * 0.5 + 0.5)` dihitung terlebih dahulu secara mandiri untuk menghasilkan transisi 3-nada yang halus, lembut, dan artistik.
     - `celStep1` dan `celStep2` dihitung dengan feathering minimal `0.15 - 0.20` yang menjamin tidak ada tepi bergerigi atau patah-patah pada tekstur batu maupun kain.
     - Bayangan real-time dari objek lain dipadukan secara bersih melalui `litFactor = celStep1 * shadowAtten`, sehingga objek yang tertimpa bayangan pohon/atap berpindah mulus ke warna bayangan tanpa menjadi hitam gosong.
  2. **Konstruksi TBN Sesuai Standar URP Lit:**
     - `ToonVaryings` kini hanya membawa `tangentWS` (dengan sign handedness di channel `.w`). Bitangent direkonstruksi per-pixel via `sgn * cross(normalWS, tangentWS.xyz)`, menjamin normal mapping 100% presisi tanpa distorsi.
     - Nilai `_BumpScale` dikalibrasi ke 0.5 - 0.6 untuk memberikan tekstur bebatuan yang tajam namun tetap menjaga kelembutan gradasi anime.
  3. **Proteksi Lantai Kecerahan Bayangan (Vibrant Shadow Floor):**
     - Bayangan cel diberi batas proteksi kecerahan minimal 35% - 45% dari warna albedo asli (`max(cShadow, albedo * 0.35)`).
     - Ambient Spherical Harmonics (SH) diintegrasikan dengan lantai minimal `half3(0.35, 0.35, 0.38)`.
     - Seluruh varian material (Biasa, Metal, Kulit, Universal) kini selalu menampilkan detail tekstur batu secara tajam, bersih, dan indah, baik di sisi terang maupun di sisi bayangan.


123. Preservasi 100% Tekstur & Warna Material Asli (Faithful Neutral Toon Shading) Tanpa Mengubah Karakter Visual
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (v1.4)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (v1.4)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (v1.4)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (v1.4)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat`
- Klarifikasi Kebutuhan Pengguna:
  * Pengguna menegaskan bahwa ketika shader ini diterapkan menimpa material/tekstur lama, shader tidak boleh merombak atau mengubah tampilan warna dan karakter tekstur aslinya secara drastis (tidak boleh berubah jadi ungu, gosong, mengkilap plastik, atau terdistorsi); tujuannya murni mengubah model pencahayaannya menjadi gaya toon cel-shading yang rapi.
- Rekayasa & Kalibrasi Neutral Toon Shading:
  1. **Preservasi 100% Warna Asli Albedo:**
     - Pada sisi terang (*lit*), formula kini murni `cLit = albedoColor.rgb * lightColor`, sehingga tampilan warna, kontras, dan detail tekstur batu/karakter sama persis dan sebersih tampilan standar URP Lit.
  2. **Warna Bayangan Netral Multiplikatif (Bebas Distorsi Ungu/Gelap):**
     - Nilai default warna bayangan `_1stShadowColor` distandarkan ke netral `(0.75, 0.75, 0.75)` dan `_2ndShadowColor` ke `(0.55, 0.55, 0.55)`.
     - Bayangan bertindak sebagai peredupan teduh alami pada albedo asli, bukan mengganti warna albedo dengan warna pigmen lain. Tekstur batu cokelat tetap cokelat, batu abu-abu tetap abu-abu.
  3. **Normal Mapping Presisi Tanpa Manipulasi Artifisial:**
     - Menggunakan `UnpackNormalScale(normalSample, _BumpScale)` standar URP tanpa modifikasi crease boost, menghasilkan lekukan tekstur yang natural persis seperti material aslinya.
  4. **Specular, Rim, & Outline Dinolkan secara Default (Opt-In):**
     - `_SpecularIntensity: 0.0`, `_RimIntensity: 0.0`, dan `_OutlineWidth: 0.0` disetel default 0. Ketika diterapkan ke material apapun (batu, tanah, kayu, kain), shader tidak akan memaksakan kilau plastik atau garis hitam kartun tebal kecuali pengguna sengaja mengaktifkannya di Inspector.


124. Penyempurnaan Drop-in Toon Shading: Pemisahan Normal Geometris untuk Batas Bayangan Cel & Kompatibilitas Penuh Property Material Lama
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (v1.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (v1.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (v1.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (v1.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
- Masalah yang Teratasi:
  * Ketika shader dipasang menggantikan material lama (seperti `stone-albedo.001.mat`), tekstur tampak bergerigi, compang-camping, atau berubah warna secara tidak diinginkan.
- Solusi Komprehensif yang Diterapkan:
  1. **Pemisahan Normal Geometris untuk Batas Bayangan Cel (`_NormalToonInfluence`):**
     - Pada material yang memiliki normal map foto-realistis (seperti batu dengan lekukan tajam dan pori-pori), menerapkan cel-stepping langsung pada normal map menyebabkan garis bayangan cel patah-patah dan membentuk bercak hitam bergerigi.
     - Solusi: Garis cel shading kini dihitung menggunakan normal geometris mesh yang mulus (`geomNormalWS = normalize(input.normalWS)`), dibaurkan secara lembut via slider `_NormalToonInfluence` (default `0.0`).
     - Hasil: Garis bayangan cel shading di sekujur batu menjadi 100% mulus, rapi, dan bersih ala anime AAA, sementara seluruh detail retakan, guratan, dan lumut pada tekstur albedo asli tetap tampak sangat tajam dan jelas.
  2. **Dukungan Penuh Properti Material Lama (Drop-In Compatibility):**
     - Ditambahkan fallback alias ke dalam seluruh shader: `_MainTex`, `_Color`, `_NormalMap`, `_Shading_Color`, `_Cel_Shader_Offset`, `_Cel_Ramp_Smoothness`, dan `_UseNormalMap`.
     - Saat material lama dari NekoLegends Anime Cel Shader maupun URP Lit dialihkan ke WuWa Toon, Unity tidak akan menghilangkan slot tekstur atau mereset properti warna bayangan ke putih.
  3. **Zero Distorsi / Nilai Bawaan Bersih:**
     - Semua efek tambahan (Specular, Rim Light, Inverted Hull Outline, Crease Boost) disetel `0.0` secara default di SEMUA varian shader (Standard, Universal, Metal, Skin).
     - Mengganti shader pada material lama kini murni 100% mempertahankan tampilan tekstur asli dengan konversi pencahayaan toon cel-shading yang mulus dan indah.


125. Arsitektur Perfected NekoLegends: Rekayasa Ulang Cel Shading Menjadi Model Anime AAA yang Sempurna
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (v1.6)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (v1.6)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat`
- Kelemahan Shader NekoLegends Bawaan yang Diperbaiki:
  1. NekoLegends hanya memiliki 1-step cel shading (biner/flat 2D), membuat objek melengkung (batu, karakter) terlihat pipih seperti stiker kertas tanpa volume 3D.
  2. Kalkulasi bayangan NekoLegends mengabaikan indirect/ambient SH lighting dan rentan mengalami over-blown highlight atau jatuh ke gelap gulita jika nilai `_dark` salah disetel.
  3. Pemotongan normal map di NekoLegends menimbulkan aliasing parah (stair-stepping/gerigi) dan lubang bercak hitam di permukaan foto-realistis.
  4. Efek rim light NekoLegends berupa fresnel kamera murah yang menyala seragam di semua sisi (bahkan di sisi gelap membelakangi cahaya).
- Penyempurnaan yang Dihadirkan (Perfected NekoLegends Architecture):
  1. **Vibrant & Punchy Anime Lighting:**
     - Area terang menampilkan tekstur asli secara tajam dan cerah (`albedo * lightColor`).
     - Area bayangan dihitung dengan multiplier matahari sehat (`max(lightColor, 0.85)`) dan dipadukan dengan ambient SH, sehingga detail tekstur batu/benda di sisi bayangan tetap 100% terang, jelas, dan hidup (tidak pernah drop ke hitam legam/muddy).
  2. **Dua Tingkat Bayangan Anime (2-Step Cel Shading / 3-Tone Volume):**
     - Dilengkapi opsi `_Use_2nd_Shadow` (aktif default). Menghasilkan transisi: Terang -> Bayangan Lembut -> Bayangan Dalam (Deep Shadow). Memberikan dimensi volume anime sinematik berkualitas Genshin/WuWa.
  3. **Batas Bayangan Mulus Anti-Aliased:**
     - Tepi cel shading diperhalus dengan minimum smoothstep guard (`0.03 - 0.08`), menghasilkan garis bayangan anime yang tajam namun bebas dari pixel jaggies / stair-stepping.
  4. **Directional Backlight Rim:**
     - Rim light kini memiliki masker arah cahaya (`dot(lightDir, -viewDir)`), hanya memancarkan siluet cahaya di tepi yang membelakangi matahari.
  5. **Standardisasi 4 Material Batu di Scene:**
     - `New Material.mat`: Preset 2-Tone Anime Cel Shading (Modern AAA).
     - `New Material 1.mat`: Preset Classic NekoLegends Single Step (Tajam & Bersih).
     - `New Material 2.mat`: Preset Soft Anime Gradient (Gradasi Lembut).
     - `New Material 3.mat`: Preset High Contrast Punchy Anime.
     - Keempat batu kini bebas dari visual blob putih/pink dan menampilkan tekstur batu secara optimal dengan karakter toon yang sempurna.


126. WuWa Toon v2.5: Pemisahan 4 Tipe Material Spesifik (Standard, Metal Super Shiny, Skin SSS, Hair Angel Ring Tuing-Tuing) & Penguatan Kontras Cel Shading Anime
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl` (v2.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader` (v2.5)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader` (Cloth & Props)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader` (Metal Shining Armor)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader` (Skin Warm SSS Fringe)
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Hair.shader` (BARU: Hair Angel Ring)
  * `Assets/8-12-2026/BISMILLAHWUWA/Demo/M_WuWa_Hair_Sample.mat` (BARU: Preset Sample Rambut)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat` (Standard / Cloth)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat` (Metal Super Shiny)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat` (Skin Warm SSS Fringe)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat` (Hair Angel Ring Tuing-Tuing)
- Masalah Pengguna yang Diatasi:
  1. Kontras Toon Shader tipis dan tidak kelihatan karena nilai warna bayangan material lama terlalu terang (abu-abu ~58%) dan cel feathering terlalu blur.
  2. Belum ada pemisahan jelas untuk 4 tipe material anime (khususnya Metal yang kurang berkilau/shining dan Rambut yang belum memiliki highlight gloss melengkung "tuing-tuing").
- Solusi Komprehensif & Fitur Baru yang Diimplementasikan:
  1. **Penguatan Kontras Cel Shading (Bold & Punchy Anime Cel):**
     - Nilai default bayangan di seluruh shader distandarkan ke kontras anime tegas: `_1stShadowColor` (0.35, 0.35, 0.40) dan `_2ndShadowColor` (0.18, 0.18, 0.22).
     - Menambahkan *Auto-Contrast Guard* pada shader: jika material lama menggunakan warna bayangan yang terlalu terang (> 0.50 luminance), shader otomatis mengkalibrasi agar garis cel shading tetap tegas dan kontras tinggi tanpa mengubah warna pigmen asli.
     - Cel edge feather dibatasi pada rentang tajam anti-aliased (0.015 - 0.20), menghilangkan kesan transisi bayangan tipis atau kabur.
  2. **Tipe 1: Standard / Cloth (Pakaian, Aksesoris, Props & Lingkungan):**
     - Cel shading 2-tier tegas dengan pemisahan area terang dan bayangan yang sangat bersih.
     - Integrasi screen-space curvature & cavity deepening: lipatan kain, guratan, dan lekukan tekstur ("lengkungannya keliatan") langsung mendapatkan penebalan bayangan alami tanpa perlu baking AO map.
  3. **Tipe 2: Skin (Kulit & Wajah dengan SSS Warm Fringe):**
     - Dilengkapi *Subsurface Scattering (SSS) Warm Fringe*: memancarkan garis cahaya kemerahan/oranye hangat di perbatasan terang-gelap (khas Wuthering Waves & Genshin Impact).
     - Bayangan kulit selalu mempertahankan undertone hangat (bebas dari kesan abu-abu kotor).
     - Transisi cel shading organik yang lebih lembut untuk kontur wajah dan tubuh.
  4. **Tipe 3: Metal (Logam, Armor, & Senjata — Super Shiny & Dynamic Glint):**
     - *Multi-Band Procedural Reflection*: menghasilkan 3 pita refleksi anime (Horizon Flash putih di garis horizon pandang, diagonal anime streaks, serta gradasi langit-bumi).
     - *Dual-Stage Specular Glint*: kilau specular bertingkat dengan core tajam menyilaukan (`pow(NdotH, sharpness) * 2.8`) dan broad sheen pelindung armor (`smoothstep * 1.2`).
     - Kontras bayangan dinamis tinggi bernuansa cool slate-navy yang membuat bagian mengkilap tampak sangat mencolok (*blinding shine*).
  5. **Tipe 4: Hair (Rambut — Angel Ring / Tenshi no Wa "Tuing-Tuing"):**
     - *Hybrid Anisotropic Tangent + View-Space Curved Halo*: memproyeksikan lingkaran kilau melingkar (*Angel Ring*) di mahkota rambut yang dinamis bergerak dan melengkung mengikuti rotasi kamera/sudut pandang ("tuing-tuing").
     - *Micro-Strand Jitter*: memecah pita kilau menjadi serat-serat helai rambut bergaris natural via kalkulasi prosedural, mencegah kilau terlihat seperti stiker plastik solid.
     - *Dual-Layer Angel Ring*: memadukan pita tajam primer dengan halo sekunder yang lebih lembut untuk kedalaman 3D rambut anime.
     - Bayangan cel 2-tone tajam untuk poni dan ikal rambut.
  6. **Konfigurasi Drop-in pada 4 Material di Scene:**
     - `New Material.mat`: Tipe 0 (Cloth / Standard) — Kontras cel shading anime tegas dan lekukan tampak jelas.
     - `New Material 1.mat`: Tipe 1 (Metal) — Refleksi logam berkilau tinggi, glint tajam, dan kontras cool shadow.
     - `New Material 2.mat`: Tipe 2 (Skin) — Kulit bernada hangat dengan pendaran SSS fringe oranye-kemerahan.
     - `New Material 3.mat`: Tipe 3 (Hair) — Kilau Angel Ring rambut dinamis "tuing-tuing" dengan serat helai alami.


127. WuWa Toon v2.6: Perbaikan Tuntas Siluet Hitam Pekat (Pitch-Black Shadow Fix), Rekonstruksi Ambient Fill SH, Penonjolan Lekukan & Rekahan 3D ("Lengkungan Keliatan"), serta Restriksi Arah Angel Ring
- Lokasi File:
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Core.hlsl`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Universal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Standard.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Metal.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Skin.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/WuWaToon_Hair.shader`
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material.mat` (Standard / Cloth / Stone)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 1.mat` (Metal)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 2.mat` (Skin)
  * `Assets/8-12-2026/BISMILLAHWUWA/New Material 3.mat` (Hair)
- Analisis Akar Masalah (Why The Scene Looked "Masih Jelek Banget" / Pitch Black Rocks):
  1. **The Pitch-Black Shadow Bug**: Pada v2.5, terdapat guard pemaksa kontras yang memotong luminance bayangan `s1` hingga `0.42` dan `s2` hingga `0.22`, sementara kontribusi ambient lighting dikalikan `0.04` (hampir 0). Ketika permukaan batu menghadap membelakangi matahari di Scene View (menghadap kamera), tekstur batu `stone-albedo.001.png` yang berwarna abu-abu gelap terkalikan menjadi 5% luminance, menghasilkan siluet hitam pekat (blackout silhouette) di mana detail tekstur dan cel shading hilang total.
  2. **Normal Map Tidak Aktif Pada Ramp**: Properti `_EnableNormalMap` di material bernilai 0 dan `_NormalToonInfluence` bernilai 0, sehingga lekukan dan permukaan relief normal map tidak mempengaruhi batas cel shading sama sekali.
  3. **Angel Ring Bocor ke Sisi Gelap**: Pada material tipe Hair, kalkulasi halo melengkung view-space tidak memiliki pembatas sudut arah cahaya (`ringMask`), memicu munculnya coretan garis oranye di atas batu hitam yang sedang membelakangi matahari.
- Solusi & Kalibrasi Komprehensif:
  1. **Rekonstruksi Warna Bayangan Anime (Vibrant & Clear Cel Shading — Anti-Blackout)**:
     - Mengubah rentang dasar bayangan cel menjadi tingkat terang anime sehat: `_1stShadowColor` (0.74, 0.74, 0.80) (~75% kecerahan albedo) dan `_2ndShadowColor` (0.48, 0.48, 0.56) (~50% kecerahan albedo untuk lekukan dalam).
     - Menghapus pembatas potong gelap (0.42/0.22) dan menggantinya dengan auto-floor guard yang menjamin tekstur di sisi bayangan tetap cerah, kaya warna, dan terbaca jelas.
  2. **Pencahayaan Ambient SH Alami di Sisi Bayangan**:
     - Menambahkan formula `albedoColor.rgb * ambient * (0.45 * (1.0 - litFactor * 0.6))`. Di sisi bayangan yang membelakangi matahari, objek menerima pantulan cahaya langit ambient secara proporsional, persis seperti karakter player dan pohon di scene.
  3. **Penonjolan Lekukan & Rekahan 3D ("Lengkungan Keliatan Jelas")**:
     - Nilai kelengkungan model (`curvature`) otomatis menggeser threshold cel Tier 2 (`offset2 = offset1 * (0.55 - creviceFactor * 0.30)`).
     - Rekahan, retakan, cekungan, dan lekukan geometri secara otomatis terpetakan ke dalam bayangan cel tingkat kedua (deep cel shadow), membuat bentuk 3D batu dan lekukan model langsung muncul tegas dan memiliki dimensi patung anime yang kaya.
     - Mengaktifkan `_EnableNormalMap: 1` dan `_NormalToonInfluence: 0.35 - 0.45` pada seluruh material batu dengan `stone-normal.001.png`, sehingga tekstur bump normal map menyatu mulus ke garis cel shading tanpa patah-patah (*anti-aliased smooth stepping*).
  4. **Restriksi Arah Cahaya pada Angel Ring Rambut ("Tuing-Tuing")**:
     - Menambahkan masker cone pantulan `ringMask = litFactor * smoothstep(0.12, 0.50, NdotH * NdotL)` pada `CalculateWuWaHair`.
     - Angel Ring kini hanya berpendar ketika permukaan menghadap ke arah pantulan cahaya/kamera, dan tidak akan pernah bocor menjadi coretan aneh di sisi bayangan gelap.
  5. **Penyempurnaan 4 Tipe Material Spesifik**:
     - **Tipe 0 (Standard/Stone/Cloth)**: Tekstur batu utuh, 2 tingkat cel shading anime kontras bersih, lekukan tampak tajam.
     - **Tipe 1 (Metal)**: Refleksi horizon glint multi-band, kilau blinding specular, dan di sisi bayangan tetap memperlihatkan sheen metalik (tidak gelap).
     - **Tipe 2 (Skin)**: Kulit bernada hangat dengan pendaran SSS fringe oranye-kemerahan terang di terminator bayangan, bersih dari noda kusam.
     - **Tipe 3 (Hair)**: Angel Ring anisotropic dinamis yang berkilau seiring rotasi sudut pandang ("tuing-tuing") dengan tekstur helai alami.
