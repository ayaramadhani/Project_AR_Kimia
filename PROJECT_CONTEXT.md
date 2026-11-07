# Dokumentasi & Konteks Proyek: AR Kimia Mobile

Dokumentasi komprehensif arsitektur, spesifikasi sistem, pembagian tugas, dan panduan teknis implementasi proyek mata kuliah **Realitas Tertambah dan Realitas Maya**.

---

## 1. Ringkasan Eksekutif & Visi Proyek

Aplikasi **AR Kimia** adalah media pembelajaran interaktif berbasis Augmented Reality (AR) untuk perangkat mobile. Konsep inti mengadopsi mekanika penggabungan elemen ala *Little Alchemy 2* yang dipadukan dengan kartu fisik bergaya *Trading Card Game* (TCG).

Pemain memindai kartu unsur kimia di dunia nyata menggunakan kamera AR, melihat representasi 3D model atom secara *real-time*, memanipulasi rasio jumlah atom melalui antarmuka layar (UI), dan menggabungkannya untuk menemukan (*discover*) senyawa kimia baru.

### Core Loop
1. **Scan Marker**: Kamera mendeteksi 1 atau lebih kartu fisik unsur kimia (Image Target).
2. **Visualisasi 3D**: Model Atom Bohr muncul di atas marker dengan rotasi prosedural cincin elektron.
3. **Penyesuaian Kuantitas (UI Counter)**: Pengguna menambah/mengurangi jumlah atom tanpa perlu mencetak banyak kartu identik (misal: 1 kartu O disetel menjadi 2 untuk membentuk $CO_2$).
4. **Validasi & Penggabungan**: Tombol *Gabungkan* divalidasi berdasarkan resep senyawa kimia. Jika valid, model atom berganti menjadi Struktur Molekul (*Ball-and-Stick*).
5. **Discovery & Persistence**: Muncul Pop-up Discovery berisi informasi senyawa baru, data tersimpan permanen ke inventori lokal (*Quick Drop*) dan Ensiklopedia.

---

## 2. Struktur Tim & Pembagian Peran

| Anggota Tim | Peran Utama | Tanggung Jawab Spesifik |
| :--- | :--- | :--- |
| **Teo** *(Programmer Utama)* | **Core AR Gameplay Logic** | Integrasi Vuforia, deteksi multi-marker, arsitektur *ScriptableObject* resep kimia, kalkulator rasio kuantitas, validasi kombinasi, animasi rotasi prosedural C#, bantuan pemodelan 3D. |
| **Iqbal** | UI/UX & 3D Modeling | Desain antarmuka di Figma (*simple stylized*), pemodelan 3D di Blender (Bohr atom & molekul), *copywriting* deskripsi unsur/senyawa, laporan mingguan. |
| **Ima** | UI Implementation & 3D | Pemodelan 3D di Blender, implementasi layout UI & sistem navigasi transisi layar di Unity UGUI. |
| **Daya** | Audio, Assist & Docs | Pencarian dan integrasi SFX/BGM bebas royalti, integrasi data ensiklopedia/pop-up, penyusunan jurnal progres di Google Classroom. |

---

## 3. Lingkungan Teknis & Status Setup Saat Ini

* **Engine**: Unity 6 (`6000.5.10f1`)
* **Render Pipeline**: Universal Render Pipeline (URP)
* **Root Workspace**: `D:\Kuliah\Realitas Tertambah dan Realitas Maya`
* **Path Proyek Unity**: `D:\Kuliah\Realitas Tertambah dan Realitas Maya\AR-Kimia\AR-Kimia`
* **AR SDK**: Vuforia Engine SDK v11.4.4 (`add-vuforia-package-11-4-4.unitypackage` siap diimport)
* **AI Dev Bridge (MCP)**:
  * Package: `com.coplaydev.unity-mcp` (v10.3.0) terpasang di `Packages/manifest.json`.
  * MCP Server: `uvx --from mcpforunityserver mcp-for-unity --transport stdio`.
  * Dependency Terpasang: Python 3.12, Astral `uv` 0.13.7 di `C:\Users\Pongo\.local\bin`.
  * Konfigurasi Global: `C:\Users\Pongo\.gemini\config\mcp_config.json`.
* **Metode Debugging**: XR Device Simulator / USB Debugging Android.

---

## 4. Spesifikasi Konten & Data Kimia

### A. Batasan Scope
* **Fokus Reaksi**: Murni penggabungan unsur menjadi senyawa (reaksi sintesis dasar). Tidak mencakup reaksi lanjutan antar-senyawa menjadi larutan kompleks.
* **Aturan Aset**: Minimal 80% aset 3D dan UI merupakan karya orisinal tim.

### B. Daftar Unsur (Minimal 10 Unsur TCG)
Setiap unsur memiliki identitas warna unik dan divisualisasikan dalam bentuk **Model Atom Bohr** (inti proton/neutron dengan cincin orbit elektron yang berputar):

1. **Hidrogen (H)**
2. **Oksigen (O)**
3. **Karbon (C)**
4. **Nitrogen (N)**
5. **Natrium (Na)**
6. **Klorin (Cl)**
7. **Belerang / Sulfur (S)**
8. **Magnesium (Mg)**
9. **Kalsium (Ca)**
10. **Besi / Ferrum (Fe)**

### C. Daftar Senyawa (Minimal 5 Senyawa)
Divisualisasikan dalam bentuk **Model Struktur Molekul (Ball-and-Stick)** menggunakan warna identitas unsur pembentuknya:
* **Air ($H_2O$)**: 2 Hidrogen + 1 Oksigen
* **Karbon Dioksida ($CO_2$)**: 1 Karbon + 2 Oksigen
* **Garam Dapur ($NaCl$)**: 1 Natrium + 1 Klorin
* **Asam Sulfat ($H_2SO_4$)**: 2 Hidrogen + 1 Belerang + 4 Oksigen *(Senyawa 3 Unsur)*
* **Karat Besi / Besi(III) Oksida ($Fe_2O_3$)** atau senyawa opsional lainnya ($CaO$, $MgO$, $CH_4$, dll.).

---

## 5. Arsitektur Antarmuka & UX (Figma Alignment)

Berdasarkan kesepakatan desain (Frame 11 & Frame 12 Figma):

1. **Tampilan Kamera Utama (Frame 12)**:
   * Viewport AR bersih untuk pemindaian marker.
   * Kiri atas: 3 Tombol aksi cepat (Quit, Settings, Ensiklopedia).
   * Kiri bawah: Tombol minim (keadaan sebelum kartu terdeteksi).
   * Kanan: Tombol ekspansi panel Quick Drop.

2. **Panel Deteksi & Pengatur Kuantitas (Frame 11 - Kiri Bawah)**:
   * Mengembang otomatis saat kartu unsur terdeteksi di kamera.
   * Menampilkan slot kartu aktif beserta pengatur jumlah atom (`+` / `-`).
   * Tombol **"Gabungkan"** aktif saat kombinasi rasio cocok dengan salah satu resep yang ada di database.

3. **Panel Quick Drop / Inventori (Kanan Layar)**:
   * Menyimpan daftar unsur dan senyawa yang sudah berhasil dibuka (*unlocked*).
   * Dilengkapi kolom pencarian (*search bar*) dan dukungan *scrolling*.
   * Dapat disembunyikan (*collapsible*) agar kamera AR tetap terlihat penuh.
   * **Fitur Drag & Drop**: Pengguna dapat menarik elemen langsung dari sidebar ke viewport kamera tanpa memindai ulang kartu fisik.

4. **Pop-up Discovery (Little Alchemy 2 Style)**:
   * Muncul di tengah layar dengan efek *background blur* saat menemukan elemen/senyawa baru.
   * Menampilkan nama, model 3D yang berputar, deskripsi fakta sains, tombol *Lanjutkan*, dan *Lihat Selengkapnya*.
   * **Status AR di latar belakang tetap persistent di memori**.

5. **Menu Ensiklopedia**:
   * Memuat katalog seluruh unsur dan senyawa.
   * Pada tab **Makes**, senyawa yang belum ditemukan ditampilkan dalam bentuk **siluet hitam berikon tanda tanya (?)** sebagai petunjuk kombinasi.

6. **Gaya Visual (Art Style)**:
   * Menggunakan pendekatan **Simple Stylized / Flat Stylized** (menghindari tekstur berat kayu/sci-fi berlebihan demi efisiensi render mobile dan kemudahan implementasi front-end di Unity).

---

## 6. Desain Arsitektur Kode (Tugas Teo)

Untuk menjaga modularitas, sistem dipisahkan menjadi beberapa layer independen:

### A. Data Layer (`ScriptableObject`)
* `ElementSO`: Menyimpan ID, simbol kimia, nomor atom, nama, warna tema, prefab 3D Model Atom Bohr, dan deskripsi teks.
* `CompoundSO`: Menyimpan ID senyawa, rumus molekul ($CO_2$, $H_2SO_4$), nama senyawa, resep komposisi (`ElementSO` + kuantitas int), prefab 3D Model Molekul, dan status *unlocked*.
* `ChemistryDatabase`: Koleksi seluruh `ElementSO` dan `CompoundSO` untuk lookup cepat O(1) berbasis hash resep.

### B. Tracking Layer (Vuforia Interface)
* `VuforiaConfiguration`: Konfigurasi wajib **`Max Simultaneous Tracked Images: 4`**.
* `ElementTargetHandler`: Komponen yang menempel pada setiap `ImageTargetBehaviour`. Memicu *event* `OnTargetDetected(ElementTargetHandler)` dan `OnTargetLost(ElementTargetHandler)` tanpa mengetahui logika UI atau reaksi kimia, sekaligus me-spawn placeholder Model Atom Bohr 3D jika prefab belum tersedia.

### C. Gameplay & Chemistry Core
* `ElementCombinationManager`:
  * Menerima daftar unsur aktif (baik dari tracking Vuforia maupun drag-and-drop Quick Drop).
  * Menghitung kuantitas total per elemen.
  * Mengevaluasi apakah kuantitas saat ini cocok dengan salah satu `CompoundSO` di database.
  * Memicu *spawn* model molekul senyawa dan transisi visual objek 3D.
* `ProceduralRotator`: Script sederhana untuk memutar model atom, ring elektron, atau model molekul secara halus (`transform.Rotate(Vector3.up * speed * Time.deltaTime)`).

### D. Inventory & State Management
* `PlayerProgressManager`: Menyimpan data elemen dan senyawa yang sudah di-unlock menggunakan serialisasi JSON ke `Application.persistentDataPath` (aman terhadap restart aplikasi).

---

## 7. Roadmap Implementasi Teknis

```
[Step 0: Selesai]
- Pembuatan Unity Project 6 URP
- Instalasi package CoplayDev/unity-mcp
- Setup environment Python uv & MCP config stdio
-------------------------------------------------
[Step 1: Pondasi AR Vuforia]
- Import add-vuforia-package-11-4-4.unitypackage
- Konfigurasi ARCamera & License Key
- Ubah Max Simultaneous Tracked Images = 4
- Buat ImageTarget dummy untuk 2-3 unsur awal (H, O, C)
-------------------------------------------------
[Step 2: ScriptableObject & Formula Validation]
- Struktur ElementSO dan CompoundSO
- Script validasi resep kimia
- Unit test logika kombinasi di Unity
-------------------------------------------------
[Step 3: Visual Spawner & Procedural Animation]
- Script rotasi prosedural orbit elektron
- Script transisi dari Model Bohr menjadi Model Molekul
-------------------------------------------------
[Step 4: UI Integration (Kerjasama Ima & Iqbal)]
- Binding Element Counter (+ / -) ke active target
- Tombol "Gabungkan" interaktif
- Pop-up Discovery dengan background blur
-------------------------------------------------
[Step 5: Quick Drop & Persistence]
- Fitur drag & drop elemen tanpa scan ulang
- Sistem save/load progress ke storage lokal
```

---

## 8. Checkpoint Progress Terakhir (2026-11-06)

### Sudah Selesai & Tersimpan (0 Compile Error / 0 Warning)
1. **Vuforia Engine 11.4.4 Terpasang & Tervalidasi**:
   - Package `com.ptc.vuforia.engine` terpasang di `Packages/manifest.json`.
   - `Assets/Resources/VuforiaConfiguration.asset`: License Key aktif, `MaxSimultaneousImageTargets = 4`.
   - `Assets/Scenes/SampleScene.unity`: `ARCamera` dan `ChemistryManager` sudah dibuat & disimpan.
2. **Database Kimia (`ScriptableObject`) Lengkap**:
   - 10 unsur di `Assets/Resources/ChemistryData/Elements/` (`H`, `O`, `C`, `N`, `Na`, `Cl`, `S`, `Mg`, `Ca`, `Fe`).
   - 5 senyawa di `Assets/Resources/ChemistryData/Compounds/` (`H2O`, `CO2`, `NaCl`, `H2SO4`, `Fe2O3`).
   - `Assets/Resources/ChemistryData/ChemistryDatabase.asset` terhubung penuh.
3. **Audit & Refactor C# Scripts Selesai**:
   - `Assets/Scripts/Data/CompoundSO.cs`: Fix `Equals`/`GetHashCode`, guard `ingredients.Count == 0`, validasi duplikat unsur di `OnValidate`.
   - `Assets/Scripts/Data/ChemistryDatabase.cs`: Optimasi pencarian zero-allocation dan null-safe `string.Equals`.
   - `Assets/Scripts/Gameplay/ProceduralRotator.cs`: Method `Configure()` untuk setup orbit elektron runtime.
   - `Assets/Scripts/Tracking/ElementTargetHandler.cs`: Tracking berbasis instance (`OnTargetDetected` / `OnTargetLost`), filter `allowExtendedTracking = false` (mencegah ghost marker saat kartu diangkat), reset static event untuk Unity 6, dan auto-generator placeholder 3D Bohr Atom.
   - `Assets/Scripts/Gameplay/ElementCombinationManager.cs`: Pelacakan instance fisik (`HashSet<ElementTargetHandler>`) terpisah dari offset manual UI (`+/-`), kalkulasi titik tengah 3D (`TryGetTrackedCentroid`), auto-fallback load `ChemistryDatabase`.
   - `Assets/Scripts/Tracking/MultiMarkerDebugOverlay.cs`: Panel diagnostik ringkas (~90 baris, DPI-scaled + safe-area mobile) murni untuk verifikasi status multi-marker.
4. **Scene & Aset Multi-Marker Siap Tes**:
   - Material URP `Assets/Resources/ChemistryData/PlaceholderAtomMaterial.mat` sudah dibuat.
   - 4 Vuforia Instant `ImageTarget` (`ImageTarget_H`, `ImageTarget_O`, `ImageTarget_Na`, `ImageTarget_Cl`) terpasang di `Assets/Scenes/SampleScene.unity` lengkap dengan `ElementTargetHandler`.
   - `ChemistryManager` sudah dipasangi `ElementCombinationManager` + `MultiMarkerDebugOverlay`.
   - Lembar tes 4 marker siap pindai di `MULTI_MARKER_TEST_SHEET.png` (root workspace).

### Opsi Pengujian Kamera Multi-Marker
1. **Langsung di Unity Play Mode (Webcam Laptop)**: `Chicony USB2.0 Camera` sudah dipilih di `VuforiaConfiguration`. Buka `MULTI_MARKER_TEST_SHEET.png` di layar HP, tekan **Play** di Unity, lalu arahkan layar HP ke webcam laptop.
2. **Kamera HP via USB Tanpa Build APK**: Gunakan aplikasi USB Webcam (DroidCam / Iriun), ubah webcam Vuforia ke kamera tersebut, lalu arahkan HP ke `MULTI_MARKER_TEST_SHEET.png` di monitor laptop.
3. **Build APK via USB Debugging**: Install modul **Android Build Support** (+ OpenJDK & SDK/NDK Tools) di **Unity Hub > Installs > 6000.5.10f1 > Add Modules** terlebih dahulu karena `AndroidPlayer` belum terpasang.


