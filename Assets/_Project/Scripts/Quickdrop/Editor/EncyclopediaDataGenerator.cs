#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EncyclopediaDataGenerator
{
    const string Folder = "Assets/_Project/ScriptableObjects/Encyclopedia";

    [MenuItem("Chemistry/Generate Encyclopedia Entries")]
    public static void Generate()
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
        };

        AssetDatabase.SaveAssets();

        var go = Selection.activeGameObject;
        var popup = go != null ? go.GetComponent<EncyclopediaPopup>() : null;
        if (popup != null)
        {
            popup.entries = list;
            EditorUtility.SetDirty(popup);
            PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
            Debug.Log("EncyclopediaPopup terisi dengan " + list.Count + " entri.");
        }
        else
        {
            Debug.Log("Entri dibuat. Pilih objek EncyclopediaPopup di Hierarchy lalu jalankan menu ini lagi untuk mengisi daftarnya.");
        }
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
}
#endif