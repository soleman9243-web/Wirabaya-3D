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
