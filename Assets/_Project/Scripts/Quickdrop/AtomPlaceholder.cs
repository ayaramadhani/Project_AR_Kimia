using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ARKimia.Data;
using ARKimia.Tracking;

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
        { "S",  new Color(0.99f, 0.85f, 0.21f) },
        { "Mg", new Color(0.98f, 0.55f, 0.00f) },
        { "Ca", new Color(0.56f, 0.64f, 0.68f) },
        { "Fe", new Color(0.55f, 0.43f, 0.39f) },
    };

    void Update()
    {
        for (int i = 0; i < orbits.Count; i++)
            if (orbits[i])
                orbits[i].Rotate(0f, spinSpeed * (i % 2 == 0 ? 1f : -1.3f) * Time.deltaTime, 0f, Space.Self);
    }

    public static GameObject Create(ElementSO e, float size, Transform parent)
    {
        var root = new GameObject("Atom_" + e.Symbol);
        root.transform.SetParent(parent, false);
        var atom = root.AddComponent<AtomPlaceholder>();

        Color col = Colors.TryGetValue(e.Symbol, out var c) ? c : e.ThemeColor;
        Color ringCol = Color.Lerp(col, Color.white, 0.5f);
        Material baseMat = Resources.Load<Material>("ChemistryData/PlaceholderAtomMaterial");

        var nucleus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ElementTargetHandler.StripCollider(nucleus);
        nucleus.transform.SetParent(root.transform, false);
        nucleus.transform.localScale = Vector3.one * size;
        ElementTargetHandler.ApplyColor(nucleus, baseMat, col);

        var hit = root.AddComponent<SphereCollider>();
        hit.radius = size;

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
                lr.sharedMaterial = new Material(shader);
                lr.startColor = lr.endColor = ringCol;
                for (int i = 0; i < 48; i++)
                {
                    float a = 2f * Mathf.PI * i / 48f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }
            }

            var electron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ElementTargetHandler.StripCollider(electron);
            electron.transform.SetParent(pivot, false);
            electron.transform.localPosition = new Vector3(radius, 0f, 0f);
            electron.transform.localScale = Vector3.one * size * 0.22f;
            ElementTargetHandler.ApplyColor(electron, baseMat, ringCol);

            atom.orbits.Add(pivot);
        }

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = new Vector3(0f, size * 1.9f, 0f);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(0.18f, 0.06f);
        tmp.text = e.Symbol;
        tmp.fontSize = size * 4.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.25f;

        return root;
    }
}
