using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SidebarCollapse : MonoBehaviour
{
    public Button toggleButton;
    public RectTransform arrowIcon;
    public float duration = 0.25f;
    public bool startCollapsed;

    [Header("Posisi tombol (px)")]
    public float openInset = 12f;
    public float closedInset = 16f;
    public float topInset = 12f;

    RectTransform rt, btnRt;
    bool collapsed;
    Coroutine anim;

    public bool IsCollapsed => collapsed;

    void Awake()
    {
        EnsureRefs();
        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);
    }

    void Start() => Set(startCollapsed, true);

    void EnsureRefs()
    {
        if (rt == null) rt = (RectTransform)transform;
        if (btnRt == null && toggleButton != null)
        {
            btnRt = (RectTransform)toggleButton.transform;
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
        }
    }

    public void Toggle() => Set(!collapsed, false);

    public void SetCollapsed(bool value, bool instant = false) => Set(value, instant);

    void Pose(bool isCollapsed, out float sideX, out Vector2 btnPos)
    {
        EnsureRefs();
        float w = rt != null && rt.rect.width > 0f ? rt.rect.width : (rt != null && rt.sizeDelta.x > 0f ? rt.sizeDelta.x : 312f);
        float bw = btnRt != null && btnRt.rect.width > 0f ? btnRt.rect.width : (btnRt != null && btnRt.sizeDelta.x > 0f ? btnRt.sizeDelta.x : 68f);
        sideX = isCollapsed ? w : 0f;
        btnPos = isCollapsed
            ? new Vector2(-closedInset, -topInset)
            : new Vector2(-w + openInset + bw, -topInset);
    }

    void Set(bool value, bool instant)
    {
        EnsureRefs();
        collapsed = value;
        Pose(collapsed, out float sx, out Vector2 bp);
        float rot = collapsed ? 180f : 0f;

        if (anim != null) StopCoroutine(anim);
        if (instant || !Application.isPlaying || !isActiveAndEnabled) Apply(sx, bp, rot);
        else anim = StartCoroutine(Slide(sx, bp, rot));
    }

    void Apply(float sx, Vector2 bp, float rot)
    {
        if (rt != null) rt.anchoredPosition = new Vector2(sx, rt.anchoredPosition.y);
        if (btnRt != null) btnRt.anchoredPosition = bp;
        if (arrowIcon != null) arrowIcon.localRotation = Quaternion.Euler(0, 0, rot);
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
