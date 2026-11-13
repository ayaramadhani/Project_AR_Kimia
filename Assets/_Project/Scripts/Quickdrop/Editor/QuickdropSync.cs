#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuickdropSync
{
    [MenuItem("Chemistry/Sinkronkan Data Quickdrop")]
    public static void SyncAll()
    {
        var db = UnityEngine.Object.FindFirstObjectByType<ChemistryDatabase>(FindObjectsInactive.Include);
        var popup = UnityEngine.Object.FindFirstObjectByType<EncyclopediaPopup>(FindObjectsInactive.Include);
        if (db == null) Debug.LogWarning("Sinkron: objek ChemistryDatabase tidak ada di scene ini.");
        if (popup == null) Debug.LogWarning("Sinkron: objek EncyclopediaPopup tidak ada di scene ini.");

        int addEl = 0, addCo = 0, addInfo = 0;

        if (db != null)
        {
            db.elements.RemoveAll(x => x == null);
            db.compounds.RemoveAll(x => x == null);
            foreach (var e in LoadAll<ElementData>())
                if (!db.elements.Exists(x => x.symbol == e.symbol)) { db.elements.Add(e); addEl++; }
            foreach (var c in LoadAll<CompoundData>())
                if (!db.compounds.Exists(x => x.formula == c.formula)) { db.compounds.Add(c); addCo++; }
            MarkDirty(db);
        }

        if (popup != null)
        {
            popup.entries.RemoveAll(x => x == null);
            foreach (var info in LoadAll<EncyclopediaEntry>())
                if (!popup.entries.Exists(x => x.key == info.key)) { popup.entries.Add(info); addInfo++; }
            MarkDirty(popup);
        }

        if (db != null && popup != null)
        {
            foreach (var e in db.elements)
                if (!popup.entries.Exists(x => x.key == e.symbol))
                    Debug.LogWarning("Belum ada entri ensiklopedia untuk unsur: " + e.displayName + " (" + e.symbol + ")");
            foreach (var c in db.compounds)
                if (!popup.entries.Exists(x => x.key == c.formula))
                    Debug.LogWarning("Belum ada entri ensiklopedia untuk senyawa: " + c.displayName + " (" + c.formula + ")");
            Debug.Log("Sinkron selesai. Unsur " + db.elements.Count + " (+" + addEl + "), senyawa " + db.compounds.Count +
                      " (+" + addCo + "), entri ensiklopedia " + popup.entries.Count + " (+" + addInfo + "). Tekan Ctrl+S.");
        }
    }

    static List<T> LoadAll<T>() where T : UnityEngine.Object
    {
        var list = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null) list.Add(a);
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    static void MarkDirty(Component c)
    {
        EditorUtility.SetDirty(c);
        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        if (c.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
    }
}
#endif