using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropVisualSpawner : MonoBehaviour
{
    public Camera cam;
    public float distance = 0.5f;   // meter dari kamera
    public float atomSize = 0.04f;  // diameter inti placeholder (meter)
    public int maxVisuals = 6;      // batas visual di layar

    readonly List<GameObject> spawned = new();

    void OnEnable()
    {
        QuickdropEvents.ElementDropped += OnElement;
        QuickdropEvents.CompoundDropped += OnCompound;
        Debug.Log("DropVisualSpawner aktif dan menunggu drop");
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
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            return cam;
        }
    }

    void OnElement(ElementData e, Vector2 pos)
    {
        Debug.Log("DropVisualSpawner menerima unsur: " + e.displayName);
        GameObject go = e.markerPrefab != null
            ? Instantiate(e.markerPrefab)
            : AtomPlaceholder.Create(e, atomSize, null);
        Place(go, pos);
    }

    void OnCompound(CompoundData c, Vector2 pos)
    {
        Debug.Log("DropVisualSpawner menerima senyawa: " + c.displayName);
        GameObject go;
        if (c.modelPrefab != null)
        {
            go = Instantiate(c.modelPrefab);
            if (go.GetComponent<ModelRotator>() == null) go.AddComponent<ModelRotator>();
        }
        else go = MakeLabel(c.formula);   // cadangan bila model Blender belum dipasang
        Place(go, pos);
    }

    void Place(GameObject go, Vector2 screenPos)
    {
        var c = Cam;
        if (c == null)
        {
            Debug.LogError("DropVisualSpawner: kamera tidak ditemukan. Isi kolom Cam atau beri tag MainCamera.");
            Destroy(go);
            return;
        }

        go.transform.SetParent(c.transform, true);
        go.transform.position = c.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distance));
        go.transform.rotation = c.transform.rotation;
        Debug.Log("DropVisualSpawner memunculkan " + go.name + " di " + go.transform.position);

        spawned.Add(go);
        while (spawned.Count > maxVisuals)
        {
            if (spawned[0]) Destroy(spawned[0]);
            spawned.RemoveAt(0);
        }
    }

    // Dipanggil Si A saat counter direset atau digabung
    public void ClearAll()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
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