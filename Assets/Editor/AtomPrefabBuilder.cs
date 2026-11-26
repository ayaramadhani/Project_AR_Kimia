using System;
using System.Collections.Generic;
using System.Text;
using ARKimia.Data;
using UnityEditor;
using UnityEngine;

namespace ARKimia.EditorTools
{
    public static class AtomPrefabBuilder
    {
        private const string ModelsFolder = "Assets/Models/Atoms";
        private const string PrefabsFolder = "Assets/Prefabs/AR";
        private const string ElementsFolder = "Assets/Resources/ChemistryData/Elements";
        private const float ImportGlobalScale = 0.055f;

        private static readonly string[] CanonicalSymbols =
        {
            "H", "O", "C", "N", "Na", "Cl", "S", "Mg", "Ca", "Fe"
        };

        [MenuItem("AR Kimia/Build Atom 3D Prefabs")]
        public static string BuildAllAtomPrefabs()
        {
            EnsureFolderHierarchy(PrefabsFolder);

            var report = new StringBuilder();
            int builtCount = 0;

            for (int i = 0; i < CanonicalSymbols.Length; i++)
            {
                string sym = CanonicalSymbols[i];
                string fbxPath = $"{ModelsFolder}/Atom_{sym}.fbx";
                ConfigureModelImporter(fbxPath);

                GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxAsset == null)
                    throw new InvalidOperationException($"[AtomPrefabBuilder] Failed to load FBX asset at {fbxPath}");

                GameObject temp = PrefabUtility.InstantiatePrefab(fbxAsset) as GameObject;
                if (temp == null)
                    throw new InvalidOperationException($"[AtomPrefabBuilder] Failed to instantiate FBX prefab {fbxPath}");

                PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                GameObject root = new GameObject($"Prefab_Atom_{sym}");
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;

                GameObject orbitContainer = new GameObject("Wadah_Semua_Orbit");
                orbitContainer.transform.SetParent(root.transform, false);
                orbitContainer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                orbitContainer.transform.localScale = Vector3.one;
                var containerAnimator = orbitContainer.AddComponent<AtomAnimator>();
                containerAnimator.kecepatanPutar = new Vector3(15f, 35f, 10f);
                containerAnimator.gunakanSumbuLokal = false;

                Transform nucleus = FindNucleus(temp.transform);
                if (nucleus == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(temp);
                    throw new InvalidOperationException($"[AtomPrefabBuilder] Nucleus (*_Inti_Atom) missing in {fbxPath}");
                }

                nucleus.SetParent(root.transform, false);
                var nucleusAnimator = nucleus.gameObject.AddComponent<AtomAnimator>();
                nucleusAnimator.kecepatanPutar = new Vector3(10f, 20f, 5f);
                nucleusAnimator.gunakanSumbuLokal = true;

                List<Transform> orbits = FindSortedOrbits(temp.transform);
                if (orbits.Count == 0)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(temp);
                    throw new InvalidOperationException($"[AtomPrefabBuilder] No orbit rings (*_Orbit_*) found in {fbxPath}");
                }

                var orbitDetails = new List<string>(orbits.Count);
                var worldNormals = new List<Vector3>(orbits.Count);

                for (int idx = 0; idx < orbits.Count; idx++)
                {
                    Transform orbit = orbits[idx];
                    orbit.SetParent(orbitContainer.transform, false);

                    MeshFilter mf = orbit.GetComponent<MeshFilter>();
                    Vector3 normalAxis = DetectRingNormalAxis(mf);
                    float speed = (idx % 2 == 0) ? 140f : -110f;

                    var orbitAnimator = orbit.gameObject.AddComponent<AtomAnimator>();
                    orbitAnimator.kecepatanPutar = normalAxis * speed;
                    orbitAnimator.gunakanSumbuLokal = true;

                    Vector3 worldNormal = orbit.TransformDirection(normalAxis).normalized;
                    worldNormals.Add(worldNormal);
                    orbitDetails.Add($"{orbit.name}:axis={AxisName(normalAxis)}({speed:+#;-#;0})");
                }

                StripColliders(root);
                DisableShadows(root);

                var dragCollider = root.AddComponent<SphereCollider>();
                dragCollider.radius = 0.04f;
                dragCollider.center = Vector3.zero;
                root.AddComponent<ARDraggable3D>();

                float maxWorldRadius = ComputeMaxWorldRadius(root);
                string shaderSummary = CollectShaderSummary(root);
                float minShellAngle = ComputeMinInterShellAngle(worldNormals);

                string prefabPath = $"{PrefabsFolder}/Prefab_Atom_{sym}.prefab";
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(temp);

                if (savedPrefab == null)
                    throw new InvalidOperationException($"[AtomPrefabBuilder] Failed to save prefab at {prefabPath}");

                AssignPrefabToElement(sym, savedPrefab);
                builtCount++;

                report.AppendLine(
                    $"{sym}: orbits={orbits.Count} maxR={maxWorldRadius:F4}m minShellAngle={minShellAngle:F1}deg shaders=[{shaderSummary}] | {string.Join(", ", orbitDetails)}");
            }

            AssetDatabase.SaveAssets();
            string summary = $"[AtomPrefabBuilder] Built {builtCount}/10 atom prefabs.\n{report}";
            Debug.Log(summary);
            return summary;
        }

        public static Vector3 DetectRingNormalAxis(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null)
                return Vector3.forward;

            Vector3 size = mf.sharedMesh.bounds.size;
            if (size.x <= size.y && size.x <= size.z)
                return Vector3.right;
            if (size.y <= size.x && size.y <= size.z)
                return Vector3.up;
            return Vector3.forward;
        }

        private static void ConfigureModelImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"[AtomPrefabBuilder] ModelImporter missing for {assetPath}");

            bool dirty = false;

            if (!Mathf.Approximately(importer.globalScale, ImportGlobalScale))
            {
                importer.globalScale = ImportGlobalScale;
                dirty = true;
            }
            if (importer.importCameras)
            {
                importer.importCameras = false;
                dirty = true;
            }
            if (importer.importLights)
            {
                importer.importLights = false;
                dirty = true;
            }
            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                dirty = true;
            }
            if (importer.addCollider)
            {
                importer.addCollider = false;
                dirty = true;
            }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                dirty = true;
            }
            if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
            {
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        private static Transform FindNucleus(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != root && all[i].name.EndsWith("_Inti_Atom", StringComparison.OrdinalIgnoreCase))
                    return all[i];
            }
            return null;
        }

        private static List<Transform> FindSortedOrbits(Transform root)
        {
            var list = new List<Transform>();
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != root && all[i].name.IndexOf("_Orbit_", StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add(all[i]);
            }

            list.Sort((a, b) => ExtractTrailingInt(a.name).CompareTo(ExtractTrailingInt(b.name)));
            return list;
        }

        private static int ExtractTrailingInt(string name)
        {
            int end = name.Length - 1;
            while (end >= 0 && !char.IsDigit(name[end]))
                end--;
            if (end < 0)
                return int.MaxValue;

            int start = end;
            while (start > 0 && char.IsDigit(name[start - 1]))
                start--;

            return int.TryParse(name.Substring(start, end - start + 1), out int n) ? n : int.MaxValue;
        }

        private static void DisableShadows(GameObject root)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private static void StripColliders(GameObject root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    UnityEngine.Object.DestroyImmediate(cols[i]);
            }
        }

        private static float ComputeMaxWorldRadius(GameObject root)
        {
            float maxDist = 0f;
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf == null || mf.sharedMesh == null)
                    continue;

                Vector3[] verts = mf.sharedMesh.vertices;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 worldPt = mf.transform.TransformPoint(verts[v]);
                    float d = worldPt.magnitude;
                    if (d > maxDist)
                        maxDist = d;
                }
            }
            return maxDist;
        }

        private static float ComputeMinInterShellAngle(List<Vector3> worldNormals)
        {
            if (worldNormals.Count <= 1)
                return -1f;

            float minAngle = 180f;
            for (int i = 0; i < worldNormals.Count; i++)
            {
                for (int j = i + 1; j < worldNormals.Count; j++)
                {
                    float a = Vector3.Angle(worldNormals[i], worldNormals[j]);
                    if (a > 90f)
                        a = 180f - a;
                    if (a < minAngle)
                        minAngle = a;
                }
            }
            return minAngle;
        }

        private static string CollectShaderSummary(GameObject root)
        {
            var items = new HashSet<string>();
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = renderers[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null)
                    {
                        items.Add("null");
                        continue;
                    }

                    string shader = mat.shader != null ? mat.shader.name : "null-shader";
                    bool emits = mat.IsKeywordEnabled("_EMISSION");
                    Color baseCol = mat.HasProperty("_BaseColor")
                        ? mat.GetColor("_BaseColor")
                        : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.black);
                    Color emisCol = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                    items.Add($"{mat.name}:{shader}(emis={emits},base=({baseCol.r:F2},{baseCol.g:F2},{baseCol.b:F2}),ec=({emisCol.r:F2},{emisCol.g:F2},{emisCol.b:F2}))");
                }
            }
            return string.Join("; ", items);
        }

        private static string AxisName(Vector3 axis)
        {
            if (axis == Vector3.right) return "X";
            if (axis == Vector3.up) return "Y";
            return "Z";
        }

        private static void AssignPrefabToElement(string symbol, GameObject prefab)
        {
            string assetPath = $"{ElementsFolder}/{symbol}.asset";
            ElementSO elementSo = AssetDatabase.LoadAssetAtPath<ElementSO>(assetPath);
            if (elementSo == null)
                throw new InvalidOperationException($"[AtomPrefabBuilder] ElementSO not found at {assetPath}");

            var so = new SerializedObject(elementSo);
            SerializedProperty prop = so.FindProperty("bohrModelPrefab");
            if (prop == null)
                throw new InvalidOperationException($"[AtomPrefabBuilder] Property 'bohrModelPrefab' missing on {assetPath}");

            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(elementSo);
        }

        private static void EnsureFolderHierarchy(string targetFolder)
        {
            if (AssetDatabase.IsValidFolder(targetFolder))
                return;

            string[] parts = targetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
