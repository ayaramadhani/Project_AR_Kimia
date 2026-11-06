using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// Untuk AR Foundation 6 (Unity 6). Untuk AR Foundation 5, lihat README.
[RequireComponent(typeof(ARTrackedImageManager))]
public class CardTrackingManager : MonoBehaviour
{
    public ChemistryDatabase db;
    public ScanHintUI scanHint;   // opsional

    ARTrackedImageManager manager;
    readonly Dictionary<TrackableId, GameObject> spawned = new();

    void Awake() => manager = GetComponent<ARTrackedImageManager>();
    void OnEnable()  => manager.trackablesChanged.AddListener(OnChanged);
    void OnDisable() => manager.trackablesChanged.RemoveListener(OnChanged);

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var img in args.added)   Spawn(img);
        foreach (var img in args.updated) UpdateMarker(img);
        foreach (var kv in args.removed)  Remove(kv.Key);
    }

    void Spawn(ARTrackedImage img)
    {
        var element = db.FindByCardName(img.referenceImage.name);
        if (element == null || element.markerPrefab == null) return;

        var go = Instantiate(element.markerPrefab, img.transform);
        var view = go.GetComponent<MarkerView>();
        if (view != null) view.Init(element);
        spawned[img.trackableId] = go;
        if (scanHint != null) scanHint.OnMarkerShown();
    }

    void UpdateMarker(ARTrackedImage img)
    {
        if (!spawned.TryGetValue(img.trackableId, out var go)) return;
        bool visible = img.trackingState == TrackingState.Tracking;
        if (go.activeSelf != visible)
        {
            go.SetActive(visible);
            if (scanHint != null)
            {
                if (visible) scanHint.OnMarkerShown(); else scanHint.OnMarkerHidden();
            }
        }
    }

    void Remove(TrackableId id)
    {
        if (spawned.TryGetValue(id, out var go))
        {
            if (go.activeSelf && scanHint != null) scanHint.OnMarkerHidden();
            Destroy(go);
        }
        spawned.Remove(id);
    }
}
