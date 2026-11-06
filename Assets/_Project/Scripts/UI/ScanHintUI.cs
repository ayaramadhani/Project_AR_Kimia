using UnityEngine;

public class ScanHintUI : MonoBehaviour
{
    public GameObject hintRoot;
    int activeMarkers;

    public void OnMarkerShown()  { activeMarkers++; Refresh(); }
    public void OnMarkerHidden() { activeMarkers = Mathf.Max(0, activeMarkers - 1); Refresh(); }
    void Refresh() => hintRoot.SetActive(activeMarkers == 0);
}
