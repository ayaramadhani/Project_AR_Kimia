using System;
using System.Collections.Generic;
using UnityEngine;
using ARKimia.Data;
using ARKimia.Tracking;

namespace ARKimia.Gameplay
{
    public class ElementCombinationManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private ChemistryDatabase database;

        private readonly HashSet<ElementTargetHandler> trackedTargets = new HashSet<ElementTargetHandler>();
        private readonly Dictionary<ElementSO, int> physicalCounts = new Dictionary<ElementSO, int>();
        private readonly Dictionary<ElementSO, int> manualOffsets = new Dictionary<ElementSO, int>();
        private readonly Dictionary<ElementSO, int> activeElements = new Dictionary<ElementSO, int>();

        public static ElementCombinationManager Instance { get; private set; }

        public event Action<ElementSO, int> OnElementQuantityChanged;
        public event Action<ElementTargetHandler, bool> OnTargetTrackingChanged;
        public event Action<CompoundSO> OnCombinationSuccess;
        public event Action OnCombinationFailed;

        public ChemistryDatabase Database => database;
        public IReadOnlyDictionary<ElementSO, int> ActiveElements => activeElements;
        public IReadOnlyCollection<ElementTargetHandler> TrackedTargets => trackedTargets;
        public int TrackedMarkerCount => trackedTargets.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (database == null)
            {
                database = Resources.Load<ChemistryDatabase>("ChemistryData/ChemistryDatabase");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            ElementTargetHandler.OnTargetDetected += HandleTargetDetected;
            ElementTargetHandler.OnTargetLost += HandleTargetLost;

            SyncExistingTargets();
        }

        private void OnDisable()
        {
            ElementTargetHandler.OnTargetDetected -= HandleTargetDetected;
            ElementTargetHandler.OnTargetLost -= HandleTargetLost;
        }

        private void SyncExistingTargets()
        {
            ElementTargetHandler[] handlers = FindObjectsByType<ElementTargetHandler>();
            for (int i = 0; i < handlers.Length; i++)
            {
                ElementTargetHandler handler = handlers[i];
                if (handler != null && handler.isActiveAndEnabled && handler.IsTracked)
                {
                    HandleTargetDetected(handler);
                }
            }
        }

        private void HandleTargetDetected(ElementTargetHandler target)
        {
            if (target == null || target.ElementData == null)
                return;

            if (!trackedTargets.Add(target))
                return;

            ElementSO element = target.ElementData;
            int currentPhysical = physicalCounts.TryGetValue(element, out int count) ? count : 0;
            physicalCounts[element] = currentPhysical + 1;

            RecalculateElementQuantity(element);
            OnTargetTrackingChanged?.Invoke(target, true);
        }

        private void HandleTargetLost(ElementTargetHandler target)
        {
            if (target == null)
                return;

            if (!trackedTargets.Remove(target))
                return;

            ElementSO element = target.ElementData;
            if (element != null && physicalCounts.TryGetValue(element, out int count))
            {
                int nextPhysical = count - 1;
                if (nextPhysical <= 0)
                {
                    physicalCounts.Remove(element);
                    manualOffsets.Remove(element);
                }
                else
                {
                    physicalCounts[element] = nextPhysical;
                }

                RecalculateElementQuantity(element);
            }

            OnTargetTrackingChanged?.Invoke(target, false);
        }

        public int GetPhysicalMarkerCount(ElementSO element)
        {
            if (element == null)
                return 0;

            return physicalCounts.TryGetValue(element, out int count) ? count : 0;
        }

        public void SetQuantity(ElementSO element, int quantity)
        {
            if (element == null)
                return;

            int clamped = Mathf.Max(0, quantity);
            int physical = GetPhysicalMarkerCount(element);
            int offset = clamped - physical;

            if (offset == 0)
            {
                manualOffsets.Remove(element);
            }
            else
            {
                manualOffsets[element] = offset;
            }

            RecalculateElementQuantity(element);
        }

        public void AddQuantity(ElementSO element, int delta = 1)
        {
            if (element == null)
                return;

            int current = activeElements.TryGetValue(element, out int count) ? count : 0;
            int minAllowed = GetPhysicalMarkerCount(element) > 0 ? 1 : 0;
            SetQuantity(element, Mathf.Max(minAllowed, current + delta));
        }

        public void ClearAllManualQuantities()
        {
            if (manualOffsets.Count == 0)
                return;

            var affected = new List<ElementSO>(manualOffsets.Keys);
            manualOffsets.Clear();

            for (int i = 0; i < affected.Count; i++)
            {
                RecalculateElementQuantity(affected[i]);
            }
        }

        public bool TryGetTrackedCentroid(out Vector3 centroid)
        {
            centroid = Vector3.zero;
            int validCount = 0;

            foreach (ElementTargetHandler target in trackedTargets)
            {
                if (target == null)
                    continue;

                centroid += target.transform.position;
                validCount++;
            }

            if (validCount == 0)
                return false;

            centroid /= validCount;
            return true;
        }

        public CompoundSO EvaluateCurrentCombination()
        {
            if (database == null)
                return null;

            return database.EvaluateCombination(activeElements);
        }

        public bool TryCombine(out CompoundSO discoveredCompound)
        {
            discoveredCompound = EvaluateCurrentCombination();
            if (discoveredCompound != null)
            {
                OnCombinationSuccess?.Invoke(discoveredCompound);
                return true;
            }

            OnCombinationFailed?.Invoke();
            return false;
        }

        private void RecalculateElementQuantity(ElementSO element)
        {
            int physical = GetPhysicalMarkerCount(element);
            int offset = manualOffsets.TryGetValue(element, out int rawOffset) ? rawOffset : 0;
            int nextTotal = Mathf.Max(0, physical + offset);
            int prevTotal = activeElements.TryGetValue(element, out int existing) ? existing : 0;

            if (nextTotal <= 0)
            {
                if (activeElements.Remove(element))
                {
                    OnElementQuantityChanged?.Invoke(element, 0);
                }
                return;
            }

            activeElements[element] = nextTotal;
            if (nextTotal != prevTotal)
            {
                OnElementQuantityChanged?.Invoke(element, nextTotal);
            }
        }
    }
}
