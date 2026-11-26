using System;
using System.Collections;
using UnityEngine;
using Vuforia;
using ARKimia.Data;
using ARKimia.Gameplay;

namespace ARKimia.Tracking
{
    [RequireComponent(typeof(ObserverBehaviour))]
    public class ElementTargetHandler : MonoBehaviour
    {
        [Header("Element Data")]
        [SerializeField] private ElementSO elementData;

        [Header("Tracking Filtering")]
        [Tooltip("Keep false for tabletop card scanning so removing a card from camera view marks it lost.")]
        [SerializeField] private bool allowExtendedTracking = false;
        [SerializeField] private float lostDebounceSeconds = 0.5f;
        [Tooltip("Grace period before hiding visual on brief tracking dropouts to prevent multi-marker flickering.")]
        [SerializeField] private float visualLostGraceSeconds = 0.35f;

        [Header("Visual Spawning")]
        [SerializeField] private bool spawnPlaceholderIfNoPrefab = true;
        [SerializeField] private Vector3 modelLocalOffset = new Vector3(0f, 0.045f, 0f);
        [SerializeField] private Material placeholderBaseMaterial;

        private ObserverBehaviour observer;
        private GameObject visualRoot;
        private Coroutine lostDebounceCoroutine;
        private Coroutine visualLostCoroutine;

        public static event Action<ElementTargetHandler> OnTargetDetected;
        public static event Action<ElementTargetHandler> OnTargetLost;

        public ElementSO ElementData => elementData;
        public ObserverBehaviour Observer => observer;
        public bool IsTracked { get; private set; }
        public bool IsVisualSuppressed { get; private set; }
        public Status CurrentStatus { get; private set; } = Status.NO_POSE;
        public StatusInfo CurrentStatusInfo { get; private set; } = StatusInfo.NORMAL;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            OnTargetDetected = null;
            OnTargetLost = null;
        }

        public float VisualLostGraceSeconds => visualLostGraceSeconds;

        private void Awake()
        {
            observer = GetComponent<ObserverBehaviour>();
            BuildVisualIfNeeded();
            RefreshVisualVisibility();
        }

        private void OnEnable()
        {
            if (observer != null)
            {
                observer.OnTargetStatusChanged += HandleTargetStatusChanged;
                HandleTargetStatusChanged(observer, observer.TargetStatus);
            }
        }

        private void OnDisable()
        {
            if (observer != null)
            {
                observer.OnTargetStatusChanged -= HandleTargetStatusChanged;
            }

            CancelLostDebounce();
            CancelVisualLostGrace();

            if (IsTracked)
            {
                IsTracked = false;
                CurrentStatus = Status.NO_POSE;
                RefreshVisualVisibility();
                OnTargetLost?.Invoke(this);
            }
            else
            {
                RefreshVisualVisibility();
            }
        }

        public void SetVisualSuppressed(bool suppressed)
        {
            if (IsVisualSuppressed == suppressed)
                return;

            IsVisualSuppressed = suppressed;
            RefreshVisualVisibility();
        }

        public void SetAllowExtendedTracking(bool allow)
        {
            if (allowExtendedTracking == allow)
                return;

            allowExtendedTracking = allow;
            if (observer != null && isActiveAndEnabled)
            {
                HandleTargetStatusChanged(observer, observer.TargetStatus);
            }
        }

        private void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
        {
            CurrentStatus = targetStatus.Status;
            CurrentStatusInfo = targetStatus.StatusInfo;

            bool rawTracked = elementData != null && IsStatusConsideredTracked(targetStatus.Status);

            if (rawTracked)
            {
                CancelLostDebounce();
                CancelVisualLostGrace();
                if (!IsTracked)
                {
                    IsTracked = true;
                    ResetVisualDragPosition();
                    RefreshVisualVisibility();
                    OnTargetDetected?.Invoke(this);
                }
                else
                {
                    RefreshVisualVisibility();
                }
                return;
            }

            if (!IsTracked)
                return;

            // Wait a brief grace period before hiding visual so single-frame tracking jitter does not flicker the atom.
            float grace = lostDebounceSeconds > 0f
                ? Mathf.Min(visualLostGraceSeconds, lostDebounceSeconds)
                : visualLostGraceSeconds;

            if (grace <= 0f || !isActiveAndEnabled)
            {
                CancelVisualLostGrace();
                RefreshVisualVisibility();
            }
            else if (visualLostCoroutine == null)
            {
                visualLostCoroutine = StartCoroutine(VisualLostGraceRoutine(grace));
            }

            if (lostDebounceSeconds <= 0f || !isActiveAndEnabled)
            {
                CompleteTargetLost();
            }
            else if (lostDebounceCoroutine == null)
            {
                lostDebounceCoroutine = StartCoroutine(LostDebounceRoutine());
            }
        }

        private IEnumerator VisualLostGraceRoutine(float graceSeconds)
        {
            float elapsed = 0f;
            while (elapsed < graceSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            visualLostCoroutine = null;
            RefreshVisualVisibility();
        }

        private IEnumerator LostDebounceRoutine()
        {
            float elapsed = 0f;
            while (elapsed < lostDebounceSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            lostDebounceCoroutine = null;
            CompleteTargetLost();
        }

        private void CompleteTargetLost()
        {
            CancelVisualLostGrace();
            if (!IsTracked)
                return;

            IsTracked = false;
            RefreshVisualVisibility();
            OnTargetLost?.Invoke(this);
        }

        private void CancelLostDebounce()
        {
            if (lostDebounceCoroutine != null)
            {
                StopCoroutine(lostDebounceCoroutine);
                lostDebounceCoroutine = null;
            }
        }

        private void CancelVisualLostGrace()
        {
            if (visualLostCoroutine != null)
            {
                StopCoroutine(visualLostCoroutine);
                visualLostCoroutine = null;
            }
        }

        private void ResetVisualDragPosition()
        {
            if (visualRoot == null)
                return;

            var draggables = visualRoot.GetComponentsInChildren<ARDraggable3D>(true);
            for (int i = 0; i < draggables.Length; i++)
            {
                draggables[i].ResetToInitialLocalPosition();
            }
        }

        private bool IsStatusConsideredTracked(Status status)
        {
            if (status == Status.TRACKED)
                return true;

            if (allowExtendedTracking && status == Status.EXTENDED_TRACKED)
                return true;

            return false;
        }

        private void RefreshVisualVisibility()
        {
            bool trackedOrInGrace = observer == null
                || IsStatusConsideredTracked(CurrentStatus)
                || visualLostCoroutine != null;
            bool shouldShow = IsTracked && !IsVisualSuppressed && trackedOrInGrace;
            if (visualRoot != null && visualRoot.activeSelf != shouldShow)
            {
                visualRoot.SetActive(shouldShow);
            }
        }

        private void BuildVisualIfNeeded()
        {
            if (visualRoot != null || elementData == null)
                return;

            visualRoot = BuildBohrVisual(elementData, transform, modelLocalOffset, placeholderBaseMaterial, spawnPlaceholderIfNoPrefab);
        }

        public static GameObject BuildBohrVisual(
            ElementSO data,
            Transform parent,
            Vector3 localOffset,
            Material baseMaterialOverride = null,
            bool allowPlaceholder = true)
        {
            if (data == null)
                return null;

            if (data.BohrModelPrefab != null)
            {
                GameObject instance = Instantiate(data.BohrModelPrefab, parent);
                instance.name = $"BohrModel_{data.Symbol}";
                instance.transform.localPosition = localOffset;
                instance.transform.localRotation = Quaternion.identity;
                DisableShadows(instance);
                return instance;
            }

            if (!allowPlaceholder)
                return null;

            var root = new GameObject($"PlaceholderBohr_{data.Symbol}");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }
            root.transform.localPosition = localOffset;
            root.transform.localRotation = Quaternion.identity;

            ChemistryDatabase sharedDb = ElementCombinationManager.Instance != null && ElementCombinationManager.Instance.Database != null
                ? ElementCombinationManager.Instance.Database
                : Resources.Load<ChemistryDatabase>("ChemistryData/ChemistryDatabase");

            Material baseMat = baseMaterialOverride != null
                ? baseMaterialOverride
                : (sharedDb != null && sharedDb.PlaceholderAtomMaterial != null
                    ? sharedDb.PlaceholderAtomMaterial
                    : Resources.Load<Material>("ChemistryData/PlaceholderAtomMaterial"));

            var nucleus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nucleus.name = "Nucleus";
            nucleus.transform.SetParent(root.transform, false);
            nucleus.transform.localScale = Vector3.one * 0.032f;
            StripCollider(nucleus);
            ApplyColor(nucleus, baseMat, data.ThemeColor);

            int electronCount = Mathf.Clamp(data.AtomicNumber, 1, 6);
            for (int i = 0; i < 2; i++)
            {
                var orbitPivot = new GameObject($"Orbit_{i}");
                orbitPivot.transform.SetParent(root.transform, false);
                orbitPivot.transform.localRotation = Quaternion.Euler(i == 0 ? 28f : -35f, i * 60f, i == 0 ? 15f : -20f);

                var rotator = orbitPivot.AddComponent<ProceduralRotator>();
                rotator.Configure(Vector3.up, i == 0 ? 95f : -125f, true);

                int perOrbit = i == 0 ? Mathf.Min(2, electronCount) : Mathf.Max(1, electronCount - 2);
                float radius = i == 0 ? 0.032f : 0.044f;

                for (int e = 0; e < perOrbit; e++)
                {
                    float angleRad = (e / (float)perOrbit) * Mathf.PI * 2f;
                    var electron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    electron.name = $"Electron_{i}_{e}";
                    electron.transform.SetParent(orbitPivot.transform, false);
                    electron.transform.localPosition = new Vector3(Mathf.Cos(angleRad) * radius, 0f, Mathf.Sin(angleRad) * radius);
                    electron.transform.localScale = Vector3.one * 0.009f;
                    StripCollider(electron);
                    ApplyColor(electron, baseMat, new Color(0.25f, 0.92f, 1f, 1f));
                }
            }

            var labelObj = new GameObject("SymbolLabel");
            labelObj.transform.SetParent(root.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.036f, 0f);
            labelObj.transform.localScale = Vector3.one * 0.0022f;

            var textMesh = labelObj.AddComponent<TextMesh>();
            Font symbolFont = sharedDb != null ? sharedDb.LogoUnsurFont : null;
            if (symbolFont != null)
            {
                textMesh.font = symbolFont;
                MeshRenderer labelRenderer = labelObj.GetComponent<MeshRenderer>();
                if (labelRenderer != null && symbolFont.material != null)
                {
                    labelRenderer.sharedMaterial = symbolFont.material;
                }
            }
            textMesh.text = data.Symbol;
            textMesh.fontSize = 40;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            DisableShadows(root);
            return root;
        }

        private static void DisableShadows(GameObject root)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        public static void StripCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }

        public static void ApplyColor(GameObject obj, Material baseMat, Color color)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null)
                return;

            if (baseMat != null)
            {
                var matInstance = new Material(baseMat);
                matInstance.color = color;
                if (matInstance.HasProperty("_BaseColor"))
                    matInstance.SetColor("_BaseColor", color);
                rend.sharedMaterial = matInstance;
            }
            else
            {
                rend.material.color = color;
            }
        }
    }
}
