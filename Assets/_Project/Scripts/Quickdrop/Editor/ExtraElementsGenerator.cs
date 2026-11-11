#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExtraElementsGenerator
{
    const string ElemFolder = "Assets/_Project/ScriptableObjects/Elements";
    const string InfoFolder = "Assets/_Project/ScriptableObjects/Encyclopedia";

    class Def
    {
        public string symbol, name, subtitle, description, funFact;
        public int number;
        public Def(string symbol, string name, int number, string subtitle, string description, string funFact)
        {
            this.symbol = symbol; this.name = name; this.number = number;
            this.subtitle = subtitle; this.description = description; this.funFact = funFact;
        }
    }

    // Teks memakai ASCII saja karena font Joti One dibuat dengan Character Set ASCII
    static readonly Def[] Defs =
    {
        new Def("S", "Sulfur", 16,
            "Nomor atom 16 | Massa atom 32,06 | Padat (kuning)",
            "Sulfur atau belerang adalah nonmetal berwarna kuning cerah yang berupa padatan rapuh pada suhu ruang. Unsur ini banyak ditemukan di sekitar gunung berapi dan mata air panas, serta menjadi bahan baku pembuatan asam sulfat, korek api, dan karet.",
            "Bau telur busuk berasal dari gas hidrogen sulfida, senyawa antara hidrogen dan belerang."),
        new Def("Mg", "Magnesium", 12,
            "Nomor atom 12 | Massa atom 24,305 | Padat (logam)",
            "Magnesium adalah logam ringan berwarna putih keperakan yang kuat dan mudah terbakar. Saat dibakar, magnesium menyala putih sangat terang. Logam ini dipakai untuk membuat paduan yang ringan, misalnya pada rangka kendaraan dan laptop.",
            "Atom magnesium berada di pusat molekul klorofil, zat hijau daun yang dipakai tumbuhan untuk fotosintesis."),
        new Def("Ca", "Calcium", 20,
            "Nomor atom 20 | Massa atom 40,078 | Padat (logam)",
            "Kalsium adalah logam lunak berwarna perak keabuan yang cukup reaktif, sehingga di alam selalu ditemukan dalam senyawa, misalnya batu kapur, marmer, dan kulit telur. Kalsium menyusun tulang dan gigi serta dibutuhkan oleh otot dan saraf.",
            "Tubuh orang dewasa mengandung sekitar 1 kilogram kalsium, hampir seluruhnya di tulang dan gigi."),
        new Def("Fe", "Iron", 26,
            "Nomor atom 26 | Massa atom 55,845 | Padat (logam)",
            "Besi adalah logam keras berwarna abu-abu yang bersifat magnetik dan menjadi bahan dasar baja. Besi mudah berkarat bila bersentuhan dengan oksigen dan air. Unsur ini juga penyusun utama inti Bumi.",
            "Hemoglobin di dalam darah mengandung besi, yang membuat darah berwarna merah dan mengangkut oksigen."),
    };

    // ---------- Menu 1: unsur + ChemistryDatabase ----------
    [MenuItem("Chemistry/Extra Elements/1 Unsur S-Mg-Ca-Fe")]
    public static void CreateElements()
    {
        EnsureFolder(ElemFolder);
        var list = new List<ElementData>();

        foreach (var d in Defs)
        {
            var e = FindElementBySymbol(d.symbol);
            if (e == null)
            {
                string path = ElemFolder + "/" + d.name + ".asset";
                e = ScriptableObject.CreateInstance<ElementData>();
                e.symbol = d.symbol;
                e.displayName = d.name;
                e.atomicNumber = d.number;
                e.cardImageName = "Card_" + d.name;
                AssetDatabase.CreateAsset(e, path);
                Debug.Log("Dibuat: " + path);
            }
            list.Add(e);
        }
        AssetDatabase.SaveAssets();

        var db = GetSelected<ChemistryDatabase>();
        if (db == null)
        {
            Debug.Log("Aset siap. Pilih objek ChemistryDatabase di Hierarchy lalu jalankan menu ini lagi untuk memasukkannya ke daftar.");
            return;
        }

        int added = 0;
        foreach (var e in list)
            if (!db.elements.Contains(e)) { db.elements.Add(e); added++; }
        MarkDirty(db);
        Debug.Log("ChemistryDatabase: " + added + " unsur ditambahkan (total " + db.elements.Count + ").");
    }

    // ---------- Menu 2: ensiklopedia + EncyclopediaPopup ----------
    [MenuItem("Chemistry/Extra Elements/2 Ensiklopedia S-Mg-Ca-Fe")]
    public static void CreateInfo()
    {
        EnsureFolder(InfoFolder);
        var list = new List<EncyclopediaEntry>();

        foreach (var d in Defs)
        {
            string path = InfoFolder + "/Info_" + d.symbol + ".asset";
            var info = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(path);
            if (info == null)
            {
                info = ScriptableObject.CreateInstance<EncyclopediaEntry>();
                info.key = d.symbol;
                info.category = "UNSUR";
                info.subtitle = d.subtitle;
                info.description = d.description;
                info.funFact = d.funFact;
                AssetDatabase.CreateAsset(info, path);
                Debug.Log("Dibuat: " + path);
            }
            list.Add(info);
        }
        AssetDatabase.SaveAssets();

        var popup = GetSelected<EncyclopediaPopup>();
        if (popup == null)
        {
            Debug.Log("Entri siap. Pilih objek EncyclopediaPopup di Hierarchy lalu jalankan menu ini lagi untuk memasukkannya ke daftar.");
            return;
        }

        int added = 0;
        foreach (var info in list)
            if (!popup.entries.Exists(x => x != null && x.key == info.key)) { popup.entries.Add(info); added++; }
        MarkDirty(popup);
        Debug.Log("EncyclopediaPopup: " + added + " entri ditambahkan (total " + popup.entries.Count + ").");
    }

    // ---------- Pembantu ----------
    static ElementData FindElementBySymbol(string symbol)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:ElementData"))
        {
            var e = AssetDatabase.LoadAssetAtPath<ElementData>(AssetDatabase.GUIDToAssetPath(guid));
            if (e != null && e.symbol == symbol) return e;
        }
        return null;
    }

    static T GetSelected<T>() where T : Component
    {
        var go = Selection.activeGameObject;
        return go != null ? go.GetComponent<T>() : null;
    }

    static void MarkDirty(Component c)
    {
        EditorUtility.SetDirty(c);
        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        if (c.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif