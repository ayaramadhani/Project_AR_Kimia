using TMPro;
using UnityEngine;

/// Pasang di prefab kosong (bersama MarkerView + SphereCollider): membuat bola + teks
/// simbol sebagai placeholder sampai model Blender siap.
[RequireComponent(typeof(MarkerView))]
public class PlaceholderMarkerBuilder : MonoBehaviour
{
    public string symbol = "C";
    public Color color = Color.gray;
    public float size = 0.04f;

    void Start()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = new Vector3(0, size, 0);
        sphere.transform.localScale = Vector3.one * size * 2f;
        sphere.GetComponent<Renderer>().material.color = color;

        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0, size, 0);
            col.radius = size * 1.2f;
        }

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0, size * 2.8f, 0);
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = symbol;
        tmp.fontSize = 0.25f;
        tmp.alignment = TextAlignmentOptions.Center;
        textGo.AddComponent<BillboardFaceCamera>();
    }
}
