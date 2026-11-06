using UnityEngine;

/// Pasang di root prefab marker (bersama Collider).
public class MarkerView : MonoBehaviour
{
    public ElementData Element { get; private set; }
    public static System.Action<ElementData> OnMarkerTapped;

    public void Init(ElementData e) => Element = e;
    public void Tap() => OnMarkerTapped?.Invoke(Element);
}
