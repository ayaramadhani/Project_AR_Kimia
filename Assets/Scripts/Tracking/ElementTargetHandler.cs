using System;
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
        [Tooltip("Keep false for tabletop card scanning so removing a card from camera view immediately marks it lost.")]
        [SerializeField] private bool allowExtendedTracking = false;

        [Header("Visual Spawning")]
        [SerializeField] private bool spawnPlaceholderIfNoPrefab = true;
        [SerializeField] private Vector3 modelLocalOffset = new Vector3(0f, 0.045f, 0f);
        [SerializeField] private Material placeholderBaseMaterial;

        private ObserverBehaviour observer;
        private GameObject visualRoot;

        public static event Action<ElementTargetHandler> OnTargetDetected;
        public static event Action<ElementTargetHandler> OnTargetLost;

        public ElementSO ElementData => elementData;
        public ObserverBehaviour Observer => observer;
        public bool IsTracked { get; private set; }
        public Status CurrentStatus { get; private set; } = Status.NO_POSE;
        public StatusInfo CurrentStatusInfo { get; private set; } = StatusInfo.NORMAL;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            OnTargetDetected = null;
            OnTargetLost = null;
        }

        private void Awake()
        {
            observer = GetComponent<ObserverBehaviour>();
            BuildVisualIfNeeded();
            SetVisualActive(false);
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

            if (IsTracked)
            {
                IsTracked = false;
                CurrentStatus = Status.NO_POSE;
                SetVisualActive(false);
                OnTargetLost?.Invoke(this);
            }
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

            bool shouldBeTracked = elementData != null && IsStatusConsideredTracked(targetStatus.Status);
            if (shouldBeTracked == IsTracked)
                return;

            IsTracked = shouldBeTracked;
            SetVisualActive(IsTracked);

            if (IsTracked)
            {
                OnTargetDetected?.Invoke(this);
            }
            else
            {
                OnTargetLost?.Invoke(this);
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

        private void SetVisualActive(bool active)
        {
            if (visualRoot != null && visualRoot.activeSelf != active)
            {
                visualRoot.SetActive(active);
            }
        }

        private void BuildVisualIfNeeded()
        {
            if (visualRoot != null || elementData == null)
                return;

            if (elementData.BohrModelPrefab != null)
            {
                visualRoot = Instantiate(elementData.BohrModelPrefab, transform);
                visualRoot.name = $"BohrModel_{elementData.Symbol}";
                visualRoot.transform.localPosition = modelLocalOffset;
                visualRoot.transform.localRotation = Quaternion.identity;
                return;
            }

            if (!spawnPlaceholderIfNoPrefab)
                return;

            visualRoot = BuildPlaceholderBohrModel();
        }

        private GameObject BuildPlaceholderBohrModel()
        {
            var root = new GameObject($"PlaceholderBohr_{elementData.Symbol}");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = modelLocalOffset;
            root.transform.localRotation = Quaternion.identity;

            Material baseMat = placeholderBaseMaterial != null
                ? placeholderBaseMaterial
                : Resources.Load<Material>("ChemistryData/PlaceholderAtomMaterial");

            var nucleus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nucleus.name = "Nucleus";
            nucleus.transform.SetParent(root.transform, false);
            nucleus.transform.localScale = Vector3.one * 0.032f;
            StripCollider(nucleus);
            ApplyColor(nucleus, baseMat, elementData.ThemeColor);

            int electronCount = Mathf.Clamp(elementData.AtomicNumber, 1, 6);
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
            labelObj.transform.localPosition = new Vector3(0f, 0.042f, 0f);
            labelObj.transform.localScale = Vector3.one * 0.0045f;

            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = $"{elementData.Symbol} ({elementData.AtomicNumber})";
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            return root;
        }

        private static void StripCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        private static void ApplyColor(GameObject obj, Material baseMat, Color color)
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
