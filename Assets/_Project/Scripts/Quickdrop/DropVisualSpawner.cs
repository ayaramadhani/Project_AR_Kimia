using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ARKimia.Data;
using ARKimia.Gameplay;

public class DropVisualSpawner : MonoBehaviour
{
    public Camera cam;
    public float distance = 0.42f;
    public float atomSize = 0.036f;
    public int maxVisuals = 10;

    readonly List<GameObject> spawned = new();
    readonly Dictionary<ElementSO, List<GameObject>> spawnedByElement = new();

    public IReadOnlyList<GameObject> SpawnedVisuals => spawned;

    void OnEnable()
    {
        QuickdropEvents.ElementDropped += OnElement;
        QuickdropEvents.CompoundDropped += OnCompound;
    }

    void OnDisable()
    {
        QuickdropEvents.ElementDropped -= OnElement;
        QuickdropEvents.CompoundDropped -= OnCompound;
    }

    Camera Cam
    {
        get
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            return cam;
        }
    }

    void OnElement(ElementSO e, Vector2 pos)
    {
        if (e == null) return;
        if (ElementCombinationManager.Instance != null && !ElementCombinationManager.Instance.CanIncrement(e))
            return;

        GameObject go = e.BohrModelPrefab != null
            ? Instantiate(e.BohrModelPrefab)
            : AtomPlaceholder.Create(e, atomSize, null);
        Place(go, pos);

        if (!spawnedByElement.TryGetValue(e, out var list))
        {
            list = new List<GameObject>();
            spawnedByElement[e] = list;
        }
        list.Add(go);
    }

    void OnCompound(CompoundSO c, Vector2 pos)
    {
        if (c == null) return;
        GameObject go;
        if (c.MoleculeModelPrefab != null)
        {
            go = Instantiate(c.MoleculeModelPrefab);
            if (go.GetComponent<ProceduralRotator>() == null)
            {
                var rot = go.AddComponent<ProceduralRotator>();
                rot.Configure(Vector3.up, 42f, true);
            }
        }
        else if (ElementCombinationManager.Instance != null)
        {
            go = ElementCombinationManager.Instance.CreateStandaloneCompoundVisual(c);
        }
        else
        {
            go = MakeLabel(c.ChemicalFormula);
        }
        Place(go, pos);
    }

    void Place(GameObject go, Vector2 screenPos)
    {
        var c = Cam;
        if (c == null)
        {
            Destroy(go);
            return;
        }

        go.transform.SetParent(c.transform, true);
        go.transform.position = c.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distance));
        go.transform.rotation = c.transform.rotation;

        spawned.Add(go);
        while (spawned.Count > maxVisuals)
        {
            if (spawned[0]) Destroy(spawned[0]);
            spawned.RemoveAt(0);
        }
    }

    public bool HasVisualForElement(ElementSO e)
    {
        if (e == null || !spawnedByElement.TryGetValue(e, out var list))
            return false;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null) list.RemoveAt(i);
        }
        return list.Count > 0;
    }

    public void SyncElementVisualCount(ElementSO e, int targetCount)
    {
        if (e == null || !spawnedByElement.TryGetValue(e, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null) list.RemoveAt(i);
        }

        while (list.Count > Mathf.Max(0, targetCount))
        {
            int lastIdx = list.Count - 1;
            GameObject go = list[lastIdx];
            list.RemoveAt(lastIdx);
            spawned.Remove(go);
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
    }

    public void ClearAll()
    {
        foreach (var go in spawned)
        {
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
        spawned.Clear();
        spawnedByElement.Clear();
    }

    static GameObject MakeLabel(string text)
    {
        var go = new GameObject("Label_" + text);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 0.7f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.25f;
        return go;
    }
}
