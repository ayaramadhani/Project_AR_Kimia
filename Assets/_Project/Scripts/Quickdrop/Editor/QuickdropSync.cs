
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ARKimia.Data;

public static class QuickdropSync
{
    [MenuItem("Chemistry/Sinkronkan Data Quickdrop")]
    public static void SyncAll()
    {
        // Mengambil ChemistryDatabase dari Project Window.
        ChemistryDatabase db = Selection.activeObject as ChemistryDatabase;

        // Jika belum dipilih, cari asset ChemistryDatabase.
        if (db == null)
        {
            string[] dbGuids = AssetDatabase.FindAssets("t:ChemistryDatabase");

            if (dbGuids.Length == 1)
            {
                string path = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
                db = AssetDatabase.LoadAssetAtPath<ChemistryDatabase>(path);
            }
            else if (dbGuids.Length > 1)
            {
                Debug.LogWarning(
                    "Ditemukan beberapa ChemistryDatabase. " +
                    "Pilih asset yang ingin disinkronkan di Project Window."
                );
                return;
            }
        }

        // Mencari EncyclopediaPopup di scene.
        EncyclopediaPopup popup =
            UnityEngine.Object.FindFirstObjectByType<EncyclopediaPopup>(
                FindObjectsInactive.Include
            );

        if (db == null)
        {
            Debug.LogWarning(
                "ChemistryDatabase tidak ditemukan. " +
                "Pilih asset ChemistryDatabase di Project Window."
            );
        }

        if (popup == null)
        {
            Debug.LogWarning(
                "EncyclopediaPopup tidak ditemukan di scene."
            );
        }

        int addEl = 0;
        int addCo = 0;
        int addInfo = 0;

        // ==========================================
        // 1. SINKRONISASI UNSUR DAN SENYAWA
        // ==========================================

        if (db != null)
        {
            SerializedObject dbSO = new SerializedObject(db);

            SerializedProperty elements =
                dbSO.FindProperty("elements");

            SerializedProperty compounds =
                dbSO.FindProperty("compounds");

            if (elements == null || compounds == null)
            {
                Debug.LogError(
                    "Property elements atau compounds tidak ditemukan " +
                    "di ChemistryDatabase."
                );
                return;
            }

            // Memasukkan unsur ke ChemistryDatabase.
            foreach (ElementSO e in LoadAll<ElementSO>())
            {
                if (e == null)
                    continue;

                bool exists = false;

                for (int i = 0; i < elements.arraySize; i++)
                {
                    ElementSO existing =
                        elements.GetArrayElementAtIndex(i)
                        .objectReferenceValue as ElementSO;

                    if (existing != null &&
                        string.Equals(
                            existing.Symbol,
                            e.Symbol,
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    int index = elements.arraySize;
                    elements.InsertArrayElementAtIndex(index);

                    elements.GetArrayElementAtIndex(index)
                        .objectReferenceValue = e;

                    addEl++;
                }
            }

            // Memasukkan senyawa ke ChemistryDatabase.
            foreach (CompoundSO c in LoadAll<CompoundSO>())
            {
                if (c == null)
                    continue;

                bool exists = false;

                for (int i = 0; i < compounds.arraySize; i++)
                {
                    CompoundSO existing =
                        compounds.GetArrayElementAtIndex(i)
                        .objectReferenceValue as CompoundSO;

                    if (existing != null &&
                        string.Equals(
                            existing.ChemicalFormula,
                            c.ChemicalFormula,
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    int index = compounds.arraySize;
                    compounds.InsertArrayElementAtIndex(index);

                    compounds.GetArrayElementAtIndex(index)
                        .objectReferenceValue = c;

                    addCo++;
                }
            }

            dbSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // ==========================================
        // 2. SINKRONISASI ENSIKLOPEDIA
        // ==========================================

        if (popup != null)
        {
            // Menghapus referensi null.
            popup.entries.RemoveAll(x => x == null);

            foreach (EncyclopediaEntry info
                     in LoadAll<EncyclopediaEntry>())
            {
                if (info == null)
                    continue;

                bool exists = popup.entries.Exists(
                    x => x != null &&
                    string.Equals(
                        x.key,
                        info.key,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                if (!exists)
                {
                    popup.entries.Add(info);
                    addInfo++;
                }
            }

            MarkDirty(popup);
        }

        // ==========================================
        // 3. PEMERIKSAAN DATA ENSIKLOPEDIA
        // ==========================================

        if (db != null && popup != null)
        {
            foreach (ElementSO e in db.Elements)
            {
                if (e == null)
                    continue;

                bool hasEntry = popup.entries.Exists(
                    x => x != null &&
                    string.Equals(
                        x.key,
                        e.Symbol,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                if (!hasEntry)
                {
                    Debug.LogWarning(
                        "Belum ada entri ensiklopedia untuk unsur: " +
                        e.ElementName + " (" + e.Symbol + ")"
                    );
                }
            }

            foreach (CompoundSO c in db.Compounds)
            {
                if (c == null)
                    continue;

                bool hasEntry = popup.entries.Exists(
                    x => x != null &&
                    string.Equals(
                        x.key,
                        c.ChemicalFormula,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                if (!hasEntry)
                {
                    Debug.LogWarning(
                        "Belum ada entri ensiklopedia untuk senyawa: " +
                        c.CompoundName + " (" +
                        c.ChemicalFormula + ")"
                    );
                }
            }
        }

        // ==========================================
        // 4. LAPORAN SINKRONISASI
        // ==========================================

        int totalElements = db != null ? db.Elements.Count : 0;
        int totalCompounds = db != null ? db.Compounds.Count : 0;
        int totalEntries = popup != null ? popup.entries.Count : 0;

        Debug.Log(
            "Sinkronisasi Quickdrop selesai.\n" +
            "Unsur: " + totalElements + " (+" + addEl + ")\n" +
            "Senyawa: " + totalCompounds + " (+" + addCo + ")\n" +
            "Entri ensiklopedia: " + totalEntries +
            " (+" + addInfo + ")"
        );
    }

    // ==========================================
    // MEMUAT SEMUA ASSET BERDASARKAN TIPE
    // ==========================================

    static List<T> LoadAll<T>() where T : UnityEngine.Object
    {
        var list = new List<T>();

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:" + typeof(T).Name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
                list.Add(asset);
        }

        list.Sort(
            (a, b) => string.CompareOrdinal(a.name, b.name)
        );

        return list;
    }

    // ==========================================
    // MENANDAI PERUBAHAN SCENE
    // ==========================================

    static void MarkDirty(Component component)
    {
        if (component == null)
            return;

        EditorUtility.SetDirty(component);

        PrefabUtility.RecordPrefabInstancePropertyModifications(
            component
        );

        Scene scene = component.gameObject.scene;

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}

#endif
