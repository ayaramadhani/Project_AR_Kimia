using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AtomPlaceholder : MonoBehaviour
{
    public List<Transform> orbits = new();
    public float spinSpeed = 90f;

    static readonly Dictionary<string, Color> Colors = new()
    {
        { "H",  new Color(0.95f, 0.95f, 0.95f) },
        { "C",  new Color(0.25f, 0.25f, 0.25f) },
        { "N",  new Color(0.20f, 0.35f, 0.95f) },
        { "O",  new Color(0.91f, 0.15f, 0.17f) },
        { "Na", new Color(0.61f, 0.36f, 0.96f) },
        { "Cl", new Color(0.18f, 0.80f, 0.25f) },
        { "S",  new Color(0.95f, 0.85f, 0.20f) },
        { "Mg", new Color(0.30f, 0.75f, 0.35f) },
        { "Ca", new Color(0.30f, 0.80f, 0.70f) },
        { "Fe", new Color(0.85f, 0.45f, 0.25f) },
    };

    void Update()
    {
        for (int i = 0; i < orbits.Count; i++)
            if (orbits[i])
                orbits[i].Rotate(0f, spinSpeed * (i % 2 == 0 ? 1f : -1.3f) * Time.deltaTime, 0f, Space.Self);
    }

    public static GameObject Create(ElementData e, float size, Transform parent)
    {
        var root = new GameObject("Atom_" + e.symbol);
        root.transform.SetParent(parent, false);
        var atom = root.AddComponent<AtomPlaceholder>();

        Color col = Colors.TryGetValue(e.symbol, out var c) ? c : Color.gray;
        Color ringCol = Color.Lerp(col, Color.white, 0.5f);

        // inti
        var nucleus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(nucleus.GetComponent<Collider>());
        nucleus.transform.SetParent(root.transform, false);
        nucleus.transform.localScale = Vector3.one * size;
        nucleus.GetComponent<Renderer>().material.color = col;

        // area ketuk
        var hit = root.AddComponent<SphereCollider>();
        hit.radius = size;

        // cincin orbit + elektron
        var shader = Shader.Find("Sprites/Default");
        for (int k = 0; k < 2; k++)
        {
            var pivot = new GameObject("Orbit" + k).transform;
            pivot.SetParent(root.transform, false);
            pivot.localRotation = Quaternion.Euler(k == 0 ? 70f : -25f, 0f, k == 0 ? 20f : 60f);
            float radius = size * (1.2f + 0.6f * k);

            if (shader != null)
            {
                var lr = pivot.gameObject.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = 48;
                lr.widthMultiplier = size * 0.05f;
                lr.material = new Material(shader);
                lr.startColor = lr.endColor = ringCol;
                for (int i = 0; i < 48; i++)
                {
                    float a = 2f * Mathf.PI * i / 48f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }
            }

            var electron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(electron.GetComponent<Collider>());
            electron.transform.SetParent(pivot, false);
            electron.transform.localPosition = new Vector3(radius, 0f, 0f);
            electron.transform.localScale = Vector3.one * size * 0.22f;
            electron.GetComponent<Renderer>().material.color = ringCol;

            atom.orbits.Add(pivot);
        }

        // label simbol
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = new Vector3(0f, size * 2.3f, 0f);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.text = e.symbol;
        tmp.fontSize = size * 8f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.25f;

        return root;
    }
}