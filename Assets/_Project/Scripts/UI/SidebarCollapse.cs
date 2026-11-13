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
    public float closedInset = 64f;
    public float closedTopInset = 24f;
    public float topInset = 12f;
    public bool respectScreenSafeArea = true;

    RectTransform rt, btnRt;
    bool collapsed;
    bool initialized;
    Coroutine anim;

    public bool IsCollapsed => collapsed;

    void Awake()
    {
        EnsureRefs();
        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);
    }

    void Start()
    {
        initialized = true;
        Set(startCollapsed, true);
    }

    void OnRectTransformDimensionsChange()
    {
        if (initialized && anim == null)
            Set(collapsed, true);
    }

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
        if (isCollapsed)
        {
            float rightMargin = closedInset;
            float topMargin = closedTopInset;
            if (respectScreenSafeArea)
            {
                ComputeSafeAreaRightTop(out float safeRight, out float safeTop);
                if (safeRight > 0f)
                    rightMargin = Mathf.Max(rightMargin, safeRight + 24f);
                if (safeTop > 0f)
                    topMargin = Mathf.Max(topMargin, safeTop + 16f);
            }
            btnPos = new Vector2(-rightMargin, -topMargin);
        }
        else
        {
            btnPos = new Vector2(-w + openInset + bw, -topInset);
        }
    }

    void ComputeSafeAreaRightTop(out float safeRight, out float safeTop)
    {
        safeRight = 0f;
        safeTop = 0f;
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safe = Screen.safeArea;
        float pxRight = Mathf.Max(0f, Screen.width - safe.xMax);
        float pxTop = Mathf.Max(0f, Screen.height - safe.yMax);

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        float scale = rootCanvas != null && rootCanvas.scaleFactor > 0.01f ? rootCanvas.scaleFactor : 1f;
        safeRight = pxRight / scale;
        safeTop = pxTop / scale;
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
