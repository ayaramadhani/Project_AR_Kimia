using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Pasang di ElementSidebar. Tombol toggle adalah objek SEJAJAR (bukan anak) sidebar,
/// sehingga tetap terlihat di tepi layar saat sidebar tertutup.
public class SidebarCollapse : MonoBehaviour
{
    public Button toggleButton;
    public RectTransform arrowIcon;
    public float duration = 0.25f;
    public bool startCollapsed;

    [Header("Posisi tombol (px)")]
    public float openInset = 16f;     // jarak tombol dari tepi kiri sidebar saat terbuka
    public float closedInset = 16f;   // jarak tombol dari tepi kanan layar saat tertutup
    public float topInset = 16f;      // jarak dari atas

    RectTransform rt, btnRt;
    bool collapsed;
    Coroutine anim;

    public bool IsCollapsed => collapsed;

    void Awake()
    {
        rt = (RectTransform)transform;
        btnRt = (RectTransform)toggleButton.transform;

        // Paksa anchor dan pivot yang dibutuhkan skrip
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot = new Vector2(1f, 1f);

        toggleButton.onClick.AddListener(Toggle);
    }

    void Start() => Set(startCollapsed, true);

    public void Toggle() => Set(!collapsed, false);

    void Pose(bool isCollapsed, out float sideX, out Vector2 btnPos)
    {
        float w = rt.rect.width;
        float bw = btnRt.rect.width;
        sideX = isCollapsed ? w : 0f;
        btnPos = isCollapsed
            ? new Vector2(-closedInset, -topInset)
            : new Vector2(-w + openInset + bw, -topInset);
    }

    void Set(bool value, bool instant)
    {
        collapsed = value;
        Pose(collapsed, out float sx, out Vector2 bp);
        float rot = collapsed ? 180f : 0f;

        if (anim != null) StopCoroutine(anim);
        if (instant) Apply(sx, bp, rot);
        else anim = StartCoroutine(Slide(sx, bp, rot));
    }

    void Apply(float sx, Vector2 bp, float rot)
    {
        rt.anchoredPosition = new Vector2(sx, rt.anchoredPosition.y);
        btnRt.anchoredPosition = bp;
        if (arrowIcon) arrowIcon.localRotation = Quaternion.Euler(0, 0, rot);
    }

    IEnumerator Slide(float sx, Vector2 bp, float rot)
    {
        float sx0 = rt.anchoredPosition.x;
        Vector2 bp0 = btnRt.anchoredPosition;
        float r0 = arrowIcon ? arrowIcon.localEulerAngles.z : 0f;
        if (r0 > 180f) r0 -= 360f;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / duration);
            Apply(Mathf.Lerp(sx0, sx, k), Vector2.Lerp(bp0, bp, k), Mathf.Lerp(r0, rot, k));
            yield return null;
        }
        Apply(sx, bp, rot);
    }
}