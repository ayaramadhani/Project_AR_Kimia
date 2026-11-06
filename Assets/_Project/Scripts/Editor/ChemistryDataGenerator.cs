#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Menu: Chemistry > Generate Sample Data
/// Membuat asset unsur & senyawa awal. Jika objek ChemistryDatabase sedang
/// dipilih di Hierarchy, listnya ikut terisi otomatis.
public static class ChemistryDataGenerator
{
    const string ElemPath = "Assets/_Project/ScriptableObjects/Elements";
    const string CompPath = "Assets/_Project/ScriptableObjects/Compounds";

    [MenuItem("Chemistry/Generate Sample Data")]
    public static void Generate()
    {
        Directory.CreateDirectory(ElemPath);
        Directory.CreateDirectory(CompPath);

        var el = new Dictionary<string, ElementData>();
        MakeElement(el, "H",  "Hydrogen", 1);
        MakeElement(el, "C",  "Carbon",   6);
        MakeElement(el, "N",  "Nitrogen", 7);
        MakeElement(el, "O",  "Oxygen",   8);
        MakeElement(el, "Na", "Sodium",   11);
        MakeElement(el, "Cl", "Chlorine", 17);

        var comps = new List<CompoundData>
        {
            MakeCompound("H2O",  "Air", "Senyawa penyusun air, vital bagi kehidupan.", (el["H"],2),(el["O"],1)),
            MakeCompound("CO2",  "Karbon Dioksida", "Gas hasil respirasi dan pembakaran.", (el["C"],1),(el["O"],2)),
            MakeCompound("CH4",  "Metana", "Komponen utama gas alam.", (el["C"],1),(el["H"],4)),
            MakeCompound("NH3",  "Amonia", "Gas berbau tajam, bahan pupuk.", (el["N"],1),(el["H"],3)),
            MakeCompound("NaCl", "Garam Dapur", "Natrium klorida, bumbu dapur.", (el["Na"],1),(el["Cl"],1)),
        };

        AssetDatabase.SaveAssets();

        var db = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<ChemistryDatabase>() : null;
        if (db != null)
        {
            db.elements = new List<ElementData>(el.Values);
            db.compounds = comps;
            EditorUtility.SetDirty(db);
            Debug.Log("ChemistryDatabase terisi.");
        }
        else Debug.Log("Asset dibuat. Pilih objek ChemistryDatabase di Hierarchy lalu jalankan menu ini lagi untuk mengisi list-nya.");
    }

    static void MakeElement(Dictionary<string, ElementData> map, string sym, string name, int no)
    {
        string path = $"{ElemPath}/{name}.asset";
        var e = AssetDatabase.LoadAssetAtPath<ElementData>(path);
        if (e == null) { e = ScriptableObject.CreateInstance<ElementData>(); AssetDatabase.CreateAsset(e, path); }
        e.symbol = sym; e.displayName = name; e.atomicNumber = no;
        e.cardImageName = "Card_" + name;   // samakan nama gambar kartu dengan ini
        EditorUtility.SetDirty(e);
        map[sym] = e;
    }

    static CompoundData MakeCompound(string formula, string name, string desc, params (ElementData, int)[] r)
    {
        string path = $"{CompPath}/{formula}.asset";
        var c = AssetDatabase.LoadAssetAtPath<CompoundData>(path);
        if (c == null) { c = ScriptableObject.CreateInstance<CompoundData>(); AssetDatabase.CreateAsset(c, path); }
        c.formula = formula; c.displayName = name; c.description = desc;
        c.recipe = new List<RecipeItem>();
        foreach (var (e, n) in r) c.recipe.Add(new RecipeItem { element = e, amount = n });
        EditorUtility.SetDirty(c);
        return c;
    }
}
#endif
