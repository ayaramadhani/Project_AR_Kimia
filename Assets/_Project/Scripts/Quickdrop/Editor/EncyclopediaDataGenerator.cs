#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EncyclopediaDataGenerator
{
    public const string Folder = "Assets/_Project/ScriptableObjects/Encyclopedia";
    public const string GlossaryAssetPath = Folder + "/GlossaryData.asset";

    [MenuItem("Chemistry/Generate Encyclopedia Entries")]
    public static List<EncyclopediaEntry> Generate()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        var list = new List<EncyclopediaEntry>
        {
            Make("H", "UNSUR", "Nomor atom 1 | Massa atom 1,008 | Gas",
                "Hidrogen adalah unsur paling ringan dan paling melimpah di alam semesta. Pada suhu ruang ia berupa gas tidak berwarna dan tidak berbau. Hidrogen menjadi penyusun air dan hampir semua senyawa organik.",
                "Matahari menghasilkan energinya dengan menggabungkan inti hidrogen menjadi helium."),
            Make("C", "UNSUR", "Nomor atom 6 | Massa atom 12,011 | Padat",
                "Karbon dapat berikatan kuat dengan banyak atom, termasuk dengan sesama karbon, sehingga menjadi kerangka semua senyawa organik dan makhluk hidup. Karbon ditemukan sebagai grafit, intan, dan arang.",
                "Intan dan grafit pensil sama-sama tersusun dari karbon murni; bedanya hanya pada susunan atomnya."),
            Make("N", "UNSUR", "Nomor atom 7 | Massa atom 14,007 | Gas",
                "Nitrogen menyusun sekitar 78% udara yang kita hirup. Gas N<sub>2</sub> sangat stabil dan sulit bereaksi, tetapi nitrogen juga menjadi penyusun penting protein, DNA, dan pupuk.",
                "Nitrogen cair bersuhu sekitar minus 196 derajat Celsius dan dipakai untuk membekukan makanan serta menyimpan sampel biologis."),
            Make("O", "UNSUR", "Nomor atom 8 | Massa atom 15,999 | Gas",
                "Oksigen adalah gas yang dibutuhkan makhluk hidup untuk bernapas dan menyusun sekitar 21% udara. Oksigen juga unsur paling melimpah di kerak Bumi, terutama sebagai bagian dari batuan dan air.",
                "Lapisan ozon (O<sub>3</sub>) di atmosfer melindungi kita dari sinar ultraviolet Matahari."),
            Make("Na", "UNSUR", "Nomor atom 11 | Massa atom 22,990 | Padat (logam)",
                "Natrium adalah logam lunak keperakan yang sangat reaktif, sehingga harus disimpan di dalam minyak. Ia bereaksi hebat dengan air dan tidak pernah ditemukan sendirian di alam, melainkan dalam senyawa seperti garam dapur.",
                "Natrium yang dibakar menghasilkan nyala kuning terang, warna yang sama dengan lampu uap natrium."),
            Make("Cl", "UNSUR", "Nomor atom 17 | Massa atom 35,45 | Gas (kuning kehijauan)",
                "Klorin berupa gas kuning kehijauan berbau menyengat dan beracun dalam bentuk murni. Klorin sangat reaktif dan mudah berikatan dengan unsur lain, misalnya dengan natrium membentuk garam dapur.",
                "Klorin dalam kadar sangat kecil dipakai untuk membunuh kuman pada air minum dan kolam renang."),
            Make("S", "UNSUR", "Nomor atom 16 | Massa atom 32,06 | Padat (nonlogam)",
                "Belerang atau sulfur adalah unsur nonlogam berwarna kuning cerah yang banyak ditemukan di sekitar kawah gunung berapi. Unsur ini digunakan dalam pembuatan pupuk, korek api, dan asam sulfat.",
                "Bau menyengat di sekitar kawah gunung berapi berasal dari senyawa gas hidrogen sulfida dan sulfur dioksida."),
            Make("Mg", "UNSUR", "Nomor atom 12 | Massa atom 24,305 | Padat (logam alkali tanah)",
                "Magnesium adalah logam ringan berwarna perak keputihan yang terbakar dengan nyala putih sangat terang. Di alam, magnesium menjadi atom pusat molekul klorofil pada daun tumbuhan hijau.",
                "Tanpa atom magnesium di dalam klorofil, tumbuhan tidak akan bisa menangkap cahaya matahari untuk fotosintesis."),
            Make("Ca", "UNSUR", "Nomor atom 20 | Massa atom 40,078 | Padat (logam alkali tanah)",
                "Kalsium adalah logam reaktif yang menyusun tulang, gigi, cangkang kerang, dan batu kapur. Kalsium merupakan mineral paling melimpah di dalam tubuh manusia.",
                "Sekitar 99% kalsium dalam tubuh manusia tersimpan di dalam struktur tulang dan gigi."),
            Make("Fe", "UNSUR", "Nomor atom 26 | Massa atom 55,845 | Padat (logam transisi)",
                "Besi atau ferrum adalah logam paling banyak digunakan oleh manusia sebagai bahan baku baja dan konstruksi. Di dalam tubuh, zat besi berperan mengikat oksigen di dalam hemoglobin darah.",
                "Inti bagian dalam planet Bumi sebagian besar tersusun dari logam besi dan nikel panas."),

            Make("H2O", "SENYAWA", "Rumus H2O | Bengkok, sudut ikatan sekitar 104,5 derajat",
                "Air tersusun dari dua atom hidrogen dan satu atom oksigen yang terikat secara kovalen. Molekulnya bersifat polar sehingga air dapat melarutkan banyak zat dan menjadi pelarut penting bagi kehidupan.",
                "Air termasuk sedikit zat yang lebih ringan saat membeku; itulah sebabnya es mengapung di atas air."),
            Make("CO2", "SENYAWA", "Rumus CO2 | Bentuk lurus, dua ikatan rangkap dua",
                "Karbon dioksida tersusun dari satu atom karbon dan dua atom oksigen. Gas ini dihasilkan saat kita bernapas dan saat bahan bakar dibakar, serta dipakai tumbuhan dalam fotosintesis.",
                "Es kering adalah karbon dioksida padat; ia tidak mencair, tetapi langsung menguap menjadi gas."),
            Make("CH4", "SENYAWA", "Rumus CH4 | Bentuk tetrahedral",
                "Metana tersusun dari satu atom karbon dan empat atom hidrogen. Metana adalah komponen utama gas alam dan mudah terbakar. Gas ini juga dihasilkan oleh pembusukan bahan organik di rawa dan oleh hewan pemamah biak.",
                "Metana adalah gas rumah kaca; per molekul, ia memerangkap panas jauh lebih banyak daripada karbon dioksida."),
            Make("NH3", "SENYAWA", "Rumus NH3 | Bentuk piramida segitiga",
                "Amonia tersusun dari satu atom nitrogen dan tiga atom hidrogen. Gas ini tidak berwarna dengan bau yang sangat menyengat, mudah larut dalam air, dan menjadi bahan utama pembuatan pupuk.",
                "Sebagian besar amonia dunia dibuat dengan proses Haber-Bosch, yang menggabungkan nitrogen dari udara dengan hidrogen."),
            Make("NaCl", "SENYAWA", "Rumus NaCl | Senyawa ion, kisi kristal kubik",
                "Garam dapur terbentuk dari ion natrium (positif) dan ion klorida (negatif) yang saling tarik-menarik dan tersusun rapi dalam kisi kristal. Zat ini larut dalam air dan penting bagi tubuh untuk menjaga keseimbangan cairan dan fungsi saraf.",
                "Kedua unsur penyusunnya berbahaya saat berdiri sendiri, tetapi bersatu menjadi bumbu yang aman dimakan."),
            Make("H2SO4", "SENYAWA", "Rumus H2SO4 | Bentuk tetrahedral pada atom S pusat",
                "Asam sulfat tersusun dari dua atom hidrogen, satu atom belerang, dan empat atom oksigen. Senyawa ini merupakan asam mineral kuat yang sangat korosif dan banyak digunakan dalam aki kendaraan serta industri pupuk.",
                "Saat mengencerkan asam sulfat, asam harus selalu dituangkan perlahan ke dalam air, bukan sebaliknya, karena reaksinya melepaskan panas tinggi."),
            Make("Fe2O3", "SENYAWA", "Rumus Fe2O3 | Besi(III) oksida (Karat Besi)",
                "Karat besi atau besi(III) oksida terbentuk ketika logam besi bereaksi dengan oksigen di udara dan kelembapan air. Senyawa padat berwarna cokelat kemerahan ini juga ditemukan di alam sebagai mineral hematit.",
                "Permukaan planet Mars berwarna merah karena tanahnya kaya akan debu besi(III) oksida (karat besi)."),
        };

        AssetDatabase.SaveAssets();

        var go = Selection.activeGameObject;
        var popup = go != null ? go.GetComponent<EncyclopediaPopup>() : null;
        if (popup != null)
        {
            popup.entries = list;
            EditorUtility.SetDirty(popup);
            PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
        }

        return list;
    }

    static EncyclopediaEntry Make(string key, string category, string subtitle, string description, string funFact)
    {
        string path = Folder + "/Info_" + key + ".asset";
        var e = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(path);
        if (e == null)
        {
            e = ScriptableObject.CreateInstance<EncyclopediaEntry>();
            e.key = key;
            e.category = category;
            e.subtitle = subtitle;
            e.description = description;
            e.funFact = funFact;
            AssetDatabase.CreateAsset(e, path);
        }
        return e;
    }

    [MenuItem("Chemistry/Generate Glossary Data")]
    public static EncyclopediaGlossaryData GenerateGlossary()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        var existing = AssetDatabase.LoadAssetAtPath<EncyclopediaGlossaryData>(GlossaryAssetPath);
        if (existing != null)
            return existing;

        var asset = ScriptableObject.CreateInstance<EncyclopediaGlossaryData>();
        asset.terms = new List<GlossaryTerm>
        {
            new GlossaryTerm
            {
                term = "Asam",
                definition = "Zat kimia yang melepaskan ion hidrogen (H+) saat dilarutkan dalam air dan biasanya memiliki pH kurang dari 7, seperti asam sulfat (H2SO4)."
            },
            new GlossaryTerm
            {
                term = "Atom",
                definition = "Satuan terkecil dari suatu unsur kimia yang masih mempertahankan sifat unsur tersebut, tersusun dari inti atom (proton dan neutron) serta elektron di sekitarnya."
            },
            new GlossaryTerm
            {
                term = "Elektron",
                definition = "Partikel subatom bermuatan negatif yang bergerak mengelilingi inti atom dalam kulit atau orbital elektron dan berperan dalam pembentukan ikatan kimia."
            },
            new GlossaryTerm
            {
                term = "Garam",
                definition = "Senyawa ion yang terbentuk dari tarik-menarik ion positif (kation logam) dan ion negatif (anion), contohnya natrium klorida (NaCl)."
            },
            new GlossaryTerm
            {
                term = "Ikatan Ion",
                definition = "Ikatan kimia yang terbentuk karena serah terima elektron antara atom logam yang melepas elektron dan atom nonlogam yang menerima elektron."
            },
            new GlossaryTerm
            {
                term = "Ikatan Kovalen",
                definition = "Ikatan kimia yang terjadi ketika dua atom nonlogam saling berbagi pasangan elektron agar mencapai konfigurasi stabil, seperti pada molekul air (H2O)."
            },
            new GlossaryTerm
            {
                term = "Massa Atom",
                definition = "Massa rata-rata sebuah atom suatu unsur yang dinyatakan dalam satuan massa atom (sma), dipengaruhi oleh jumlah proton dan neutron dalam intinya."
            },
            new GlossaryTerm
            {
                term = "Molekul",
                definition = "Gabungan dua atau lebih atom yang terikat kuat secara kimia sebagai satu kesatuan netral, misalnya molekul air (H2O) dan karbon dioksida (CO2)."
            },
            new GlossaryTerm
            {
                term = "Neutron",
                definition = "Partikel subatom tidak bermuatan (netral) yang berada di dalam inti atom bersama proton."
            },
            new GlossaryTerm
            {
                term = "Nomor Atom",
                definition = "Angka yang menunjukkan jumlah proton dalam inti suatu atom dan menjadi identitas unik setiap unsur kimia dalam tabel periodik."
            },
            new GlossaryTerm
            {
                term = "Oksida",
                definition = "Senyawa kimia yang terbentuk dari gabungan unsur oksigen dengan satu unsur lain, misalnya besi(III) oksida (Fe2O3) atau karat besi."
            },
            new GlossaryTerm
            {
                term = "Proton",
                definition = "Partikel subatom bermuatan positif (+) yang terdapat di dalam inti atom."
            },
            new GlossaryTerm
            {
                term = "Reaksi Kimia",
                definition = "Proses ketika satu atau lebih zat pereaksi berubah menjadi zat baru (senyawa) dengan susunan atom dan sifat yang berbeda."
            },
            new GlossaryTerm
            {
                term = "Rumus Kimia",
                definition = "Notasi singkat menggunakan lambang unsur dan angka indeks bawah (subskrip) untuk menunjukkan jenis serta jumlah atom penyusun senyawa."
            },
            new GlossaryTerm
            {
                term = "Senyawa",
                definition = "Zat murni yang tersusun dari dua atau lebih unsur kimia berbeda yang bergabung secara kimia dengan perbandingan massa dan atom yang tetap."
            },
            new GlossaryTerm
            {
                term = "Unsur",
                definition = "Zat murni paling sederhana yang tidak dapat diuraikan lagi menjadi zat lain melalui reaksi kimia biasa, seperti Hidrogen (H) dan Oksigen (O)."
            }
        };

        asset.terms.Sort((a, b) => string.Compare(a.term, b.term, StringComparison.OrdinalIgnoreCase));
        AssetDatabase.CreateAsset(asset, GlossaryAssetPath);
        AssetDatabase.SaveAssets();
        return asset;
    }
}
#endif
