using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARKimia.Data;
using ARKimia.Tracking;

namespace ARKimia.Gameplay
{
    public class ElementCombinationManager : MonoBehaviour
    {
        public const int MaxQuantityPerElement = 10;

        [Header("Database")]
        [SerializeField] private ChemistryDatabase database;

        [Header("Reaction Settings")]
        [SerializeField] private float autoUnlockTimeoutSeconds = 8f;
        [SerializeField] private Vector3 moleculeCardOffset = new Vector3(0f, 0.055f, 0f);
        [SerializeField] private float fallbackCameraDistance = 0.42f;

        private readonly HashSet<ElementTargetHandler> trackedTargets = new HashSet<ElementTargetHandler>();
        private readonly List<ElementTargetHandler> trackedTargetOrder = new List<ElementTargetHandler>();
        private readonly Dictionary<ElementSO, int> physicalCounts = new Dictionary<ElementSO, int>();
        private readonly Dictionary<ElementSO, int> manualExtras = new Dictionary<ElementSO, int>();
        private readonly HashSet<ElementSO> suppressedPhysicalElements = new HashSet<ElementSO>();
        private readonly Dictionary<ElementSO, GameObject> virtualAtomVisuals = new Dictionary<ElementSO, GameObject>();
        private readonly Dictionary<ElementSO, int> activeElements = new Dictionary<ElementSO, int>();
        private readonly List<ElementSO> activeOrder = new List<ElementSO>();
        private readonly HashSet<ElementSO> discoveredElements = new HashSet<ElementSO>();
        private readonly List<ElementSO> discoveredElementOrder = new List<ElementSO>();
        private readonly HashSet<CompoundSO> discoveredCompounds = new HashSet<CompoundSO>();
        private readonly List<CompoundSO> discoveredCompoundOrder = new List<CompoundSO>();
        private readonly HashSet<ElementSO> unreadElements = new HashSet<ElementSO>();
        private readonly HashSet<CompoundSO> unreadCompounds = new HashSet<CompoundSO>();

        private GameObject activeCompoundInstance;
        private readonly List<ElementTargetHandler> compoundAnchorTargets = new List<ElementTargetHandler>();
        private Vector3 compoundMixedLocalOffset;
        private Coroutine reactionUnlockCoroutine;
        private Mesh cachedSphereMesh;

        public static ElementCombinationManager Instance { get; private set; }

        public event Action<ElementSO, int> OnElementQuantityChanged;
        public event Action<ElementTargetHandler, bool> OnTargetTrackingChanged;
        public event Action<ElementSO> OnElementDiscovered;
        public event Action<CompoundSO> OnCompoundDiscovered;
        public event Action OnUnreadStateChanged;
        public event Action<CompoundSO> OnCombinationSuccess;
        public event Action OnCombinationFailed;
        public event Action OnCartReset;
        public event Action<bool> OnReactionLockChanged;

        public ChemistryDatabase Database => database;
        public IReadOnlyDictionary<ElementSO, int> ActiveElements => activeElements;
        public IReadOnlyList<ElementSO> ActiveElementOrder => activeOrder;
        public IReadOnlyCollection<ElementTargetHandler> TrackedTargets => trackedTargets;
        public IReadOnlyCollection<ElementSO> DiscoveredElements => discoveredElements;
        public IReadOnlyList<ElementSO> DiscoveredElementOrder => discoveredElementOrder;
        public IReadOnlyCollection<CompoundSO> DiscoveredCompounds => discoveredCompounds;
        public IReadOnlyList<CompoundSO> DiscoveredCompoundOrder => discoveredCompoundOrder;
        public bool HasAnyUnread => unreadElements.Count > 0 || unreadCompounds.Count > 0;
        public int TrackedMarkerCount => trackedTargets.Count;
        public int ActiveElementTypeCount => activeOrder.Count;
        public bool IsReactionLocked { get; private set; }
        public GameObject ActiveCompoundVisual => activeCompoundInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

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
            QuickdropEvents.ElementDropped += HandleQuickdropElementDropped;
            QuickdropEvents.ElementInfoRequested += HandleElementInfoRequested;
            QuickdropEvents.CompoundInfoRequested += HandleCompoundInfoRequested;

            SyncExistingTargets();
        }

        private void OnDisable()
        {
            ElementTargetHandler.OnTargetDetected -= HandleTargetDetected;
            ElementTargetHandler.OnTargetLost -= HandleTargetLost;
            QuickdropEvents.ElementDropped -= HandleQuickdropElementDropped;
            QuickdropEvents.ElementInfoRequested -= HandleElementInfoRequested;
            QuickdropEvents.CompoundInfoRequested -= HandleCompoundInfoRequested;
        }

        private void HandleElementInfoRequested(ElementSO element)
        {
            MarkElementRead(element);
        }

        private void HandleCompoundInfoRequested(CompoundSO compound)
        {
            MarkCompoundRead(compound);
        }

        public bool IsElementUnread(ElementSO element)
        {
            return element != null && unreadElements.Contains(element);
        }

        public bool IsCompoundUnread(CompoundSO compound)
        {
            return compound != null && unreadCompounds.Contains(compound);
        }

        public void MarkElementRead(ElementSO element)
        {
            if (element != null && unreadElements.Remove(element))
            {
                OnUnreadStateChanged?.Invoke();
            }
        }

        public void MarkCompoundRead(CompoundSO compound)
        {
            if (compound != null && unreadCompounds.Remove(compound))
            {
                OnUnreadStateChanged?.Invoke();
            }
        }

        private void HandleQuickdropElementDropped(ElementSO element, Vector2 screenPos)
        {
            if (element == null || IsReactionLocked)
                return;

            Vector3? worldPos = null;
            Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, fallbackCameraDistance));
            }

            TryAddFromQuickDrop(element, worldPos);
        }

        private void LateUpdate()
        {
            if (activeCompoundInstance != null && compoundAnchorTargets.Count > 0)
            {
                UpdateCompoundAnchorPose();
            }
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

            trackedTargetOrder.Add(target);

            ElementSO element = target.ElementData;
            int currentPhysical = physicalCounts.TryGetValue(element, out int count) ? count : 0;
            physicalCounts[element] = currentPhysical + 1;

            RegisterElementDiscovery(element);

            if (IsReactionLocked)
            {
                suppressedPhysicalElements.Add(element);
                target.SetVisualSuppressed(true);
                OnTargetTrackingChanged?.Invoke(target, true);
                return;
            }

            ClearActiveCompoundVisual();
            suppressedPhysicalElements.Remove(element);
            ApplySuppressionForElement(element, false);
            DestroyVirtualVisual(element);

            RecalculateElementQuantity(element, forceNotify: true);
            OnTargetTrackingChanged?.Invoke(target, true);
        }

        private void HandleTargetLost(ElementTargetHandler target)
        {
            if (target == null)
                return;

            if (!trackedTargets.Remove(target))
                return;

            trackedTargetOrder.Remove(target);
            target.SetVisualSuppressed(false);

            if (compoundAnchorTargets.Contains(target))
            {
                Vector3 prevWorldPos = activeCompoundInstance != null ? activeCompoundInstance.transform.position : Vector3.zero;
                compoundAnchorTargets.Remove(target);

                if (compoundAnchorTargets.Count == 0)
                {
                    ClearActiveCompoundVisual();
                }
                else if (activeCompoundInstance != null)
                {
                    Vector3 nextPhysCenter = ComputePhysicalAnchorsCentroid(out Quaternion nextPhysRot);
                    compoundMixedLocalOffset = Quaternion.Inverse(nextPhysRot) * (prevWorldPos - nextPhysCenter);
                }
            }

            ElementSO element = target.ElementData;
            if (element != null && physicalCounts.TryGetValue(element, out int count))
            {
                int nextPhysical = count - 1;
                if (nextPhysical <= 0)
                {
                    physicalCounts.Remove(element);
                    suppressedPhysicalElements.Remove(element);
                }
                else
                {
                    physicalCounts[element] = nextPhysical;
                }

                if (!IsReactionLocked)
                {
                    if (GetEffectivePhysicalMarkerCount(element) == 0 && GetManualExtra(element) > 0)
                    {
                        EnsureVirtualVisual(element, null);
                    }
                    RecalculateElementQuantity(element, forceNotify: true);
                }
            }

            OnTargetTrackingChanged?.Invoke(target, false);
        }

        public int GetQuantity(ElementSO element)
        {
            if (element == null)
                return 0;

            return activeElements.TryGetValue(element, out int count) ? count : 0;
        }

        public int GetPhysicalMarkerCount(ElementSO element)
        {
            if (element == null)
                return 0;

            return physicalCounts.TryGetValue(element, out int count) ? count : 0;
        }

        public int GetEffectivePhysicalMarkerCount(ElementSO element)
        {
            if (element == null || suppressedPhysicalElements.Contains(element))
                return 0;

            return Mathf.Min(MaxQuantityPerElement, GetPhysicalMarkerCount(element));
        }

        public int GetMinAllowedQuantity(ElementSO element)
        {
            return 0;
        }

        public bool HasZeroedPhysicalMarker(ElementSO element)
        {
            return GetEffectivePhysicalMarkerCount(element) > 0 && GetQuantity(element) <= 0;
        }

        public bool CanIncrement(ElementSO element)
        {
            return !IsReactionLocked && element != null && GetQuantity(element) < MaxQuantityPerElement;
        }

        public bool CanDecrement(ElementSO element)
        {
            return !IsReactionLocked && element != null && GetQuantity(element) > GetMinAllowedQuantity(element);
        }

        public void SetQuantity(ElementSO element, int quantity)
        {
            if (element == null || IsReactionLocked)
                return;

            if (quantity > 0 && suppressedPhysicalElements.Remove(element))
            {
                ApplySuppressionForElement(element, false);
            }

            int effectivePhysical = GetEffectivePhysicalMarkerCount(element);
            int clampedTotal = Mathf.Clamp(quantity, 0, MaxQuantityPerElement);
            int nextExtra = clampedTotal - effectivePhysical;

            if (nextExtra == 0)
            {
                manualExtras.Remove(element);
            }
            else
            {
                manualExtras[element] = nextExtra;
            }

            if (clampedTotal <= 0)
            {
                DestroyVirtualVisual(element);
            }
            else
            {
                ClearActiveCompoundVisual();
                RegisterElementDiscovery(element);

                if (effectivePhysical == 0)
                {
                    EnsureVirtualVisual(element, null);
                }
                else
                {
                    DestroyVirtualVisual(element);
                }
            }

            DropVisualSpawner dropSpawner = FindAnyObjectByType<DropVisualSpawner>();
            if (dropSpawner != null)
            {
                dropSpawner.SyncElementVisualCount(element, Mathf.Max(0, clampedTotal - effectivePhysical));
            }

            RecalculateElementQuantity(element, forceNotify: false);
        }

        public void AddQuantity(ElementSO element, int delta = 1)
        {
            if (element == null || IsReactionLocked || delta == 0)
                return;

            if (delta > 0 && suppressedPhysicalElements.Remove(element))
            {
                ApplySuppressionForElement(element, false);
                ClearActiveCompoundVisual();
                DestroyVirtualVisual(element);
                RecalculateElementQuantity(element, forceNotify: true);
                return;
            }

            SetQuantity(element, GetQuantity(element) + delta);
        }

        public bool TryAddFromQuickDrop(ElementSO element, Vector3? worldSpawnPosition = null)
        {
            if (element == null || IsReactionLocked)
                return false;

            if (suppressedPhysicalElements.Remove(element))
            {
                ApplySuppressionForElement(element, false);
                ClearActiveCompoundVisual();
                DestroyVirtualVisual(element);
                RecalculateElementQuantity(element, forceNotify: true);
                return true;
            }

            int current = GetQuantity(element);
            if (current >= MaxQuantityPerElement)
                return false;

            ClearActiveCompoundVisual();
            RegisterElementDiscovery(element);

            int effectivePhysical = GetEffectivePhysicalMarkerCount(element);
            int nextExtra = Mathf.Clamp((current + 1) - effectivePhysical, -effectivePhysical, MaxQuantityPerElement - effectivePhysical);
            if (nextExtra == 0)
            {
                manualExtras.Remove(element);
            }
            else
            {
                manualExtras[element] = nextExtra;
            }

            if (effectivePhysical == 0)
            {
                EnsureVirtualVisual(element, worldSpawnPosition);
            }

            RecalculateElementQuantity(element, forceNotify: false);
            return true;
        }

        public void ClearAllManualQuantities()
        {
            if (manualExtras.Count == 0)
                return;

            var affected = new List<ElementSO>(manualExtras.Keys);
            manualExtras.Clear();
            DestroyAllVirtualVisuals();

            for (int i = 0; i < affected.Count; i++)
            {
                RecalculateElementQuantity(affected[i], forceNotify: true);
            }
        }

        public void ResetCart()
        {
            ClearActiveCompoundVisual();
            UnlockReactionCamera();

            manualExtras.Clear();
            DestroyAllVirtualVisuals();

            DropVisualSpawner dropSpawner = FindAnyObjectByType<DropVisualSpawner>();
            if (dropSpawner != null)
            {
                dropSpawner.ClearAll();
            }

            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler target = trackedTargetOrder[i];
                if (target != null && target.ElementData != null)
                {
                    suppressedPhysicalElements.Add(target.ElementData);
                    target.SetVisualSuppressed(true);
                }
            }

            if (activeOrder.Count > 0)
            {
                var snapshot = new List<ElementSO>(activeOrder);
                activeElements.Clear();
                activeOrder.Clear();

                for (int i = 0; i < snapshot.Count; i++)
                {
                    OnElementQuantityChanged?.Invoke(snapshot[i], 0);
                }
            }

            OnCartReset?.Invoke();
        }

        public void ZeroAllElementsAndClearModels()
        {
            ClearActiveCompoundVisual();
            UnlockReactionCamera();

            DestroyAllVirtualVisuals();

            DropVisualSpawner dropSpawner = FindAnyObjectByType<DropVisualSpawner>();
            if (dropSpawner != null)
            {
                dropSpawner.ClearAll();
            }

            var affected = new HashSet<ElementSO>(activeOrder);
            foreach (ElementSO key in manualExtras.Keys)
            {
                affected.Add(key);
            }
            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler t = trackedTargetOrder[i];
                if (t != null && t.ElementData != null)
                {
                    affected.Add(t.ElementData);
                }
            }

            manualExtras.Clear();
            suppressedPhysicalElements.Clear();

            foreach (ElementSO elem in affected)
            {
                if (elem == null)
                    continue;

                int phys = GetEffectivePhysicalMarkerCount(elem);
                if (phys > 0)
                {
                    manualExtras[elem] = -phys;
                }
                RecalculateElementQuantity(elem, forceNotify: true);
            }
        }

        public void UnlockReactionCamera()
        {
            if (reactionUnlockCoroutine != null)
            {
                StopCoroutine(reactionUnlockCoroutine);
                reactionUnlockCoroutine = null;
            }

            if (!IsReactionLocked)
                return;

            IsReactionLocked = false;
            OnReactionLockChanged?.Invoke(false);
        }

        public bool TryGetTrackedCentroid(out Vector3 centroid)
        {
            centroid = Vector3.zero;
            int validCount = 0;

            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler target = trackedTargetOrder[i];
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
            if (database == null || activeElements.Count == 0)
                return null;

            return database.EvaluateCombination(activeElements);
        }

        public bool HasRelatedRecipeForActiveElements()
        {
            if (database == null || activeElements.Count < 2)
                return false;

            return database.HasRelatedCompound(activeElements);
        }

        public bool TryCombine(out CompoundSO discoveredCompound)
        {
            discoveredCompound = null;
            if (IsReactionLocked)
                return false;

            discoveredCompound = EvaluateCurrentCombination();
            if (discoveredCompound == null)
            {
                OnCombinationFailed?.Invoke();
                return false;
            }

            ExecuteCombinationSequence(discoveredCompound);
            return true;
        }

        private void ExecuteCombinationSequence(CompoundSO compound)
        {
            SetReactionLockedInternal(true);

            var participatingCards = new List<ElementTargetHandler>();
            var allObjectPositions = new List<Vector3>();

            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler target = trackedTargetOrder[i];
                if (target != null && target.ElementData != null && activeElements.ContainsKey(target.ElementData))
                {
                    participatingCards.Add(target);
                    Vector3 atomPos = target.transform.TransformPoint(moleculeCardOffset);
                    allObjectPositions.Add(atomPos);
                    SpawnSmokePuff(atomPos);
                }
            }

            foreach (KeyValuePair<ElementSO, GameObject> kvp in virtualAtomVisuals)
            {
                if (kvp.Value != null && activeElements.ContainsKey(kvp.Key))
                {
                    Vector3 virtPos = kvp.Value.transform.position;
                    allObjectPositions.Add(virtPos);
                    SpawnSmokePuff(virtPos);
                }
            }

            DropVisualSpawner dropSpawner = FindAnyObjectByType<DropVisualSpawner>();
            if (dropSpawner != null)
            {
                for (int i = 0; i < dropSpawner.SpawnedVisuals.Count; i++)
                {
                    GameObject dropGo = dropSpawner.SpawnedVisuals[i];
                    if (dropGo != null)
                    {
                        Vector3 dropPos = dropGo.transform.position;
                        allObjectPositions.Add(dropPos);
                        SpawnSmokePuff(dropPos);
                    }
                }
                dropSpawner.ClearAll();
            }

            manualExtras.Clear();
            DestroyAllVirtualVisuals();

            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler target = trackedTargetOrder[i];
                if (target != null && target.ElementData != null)
                {
                    suppressedPhysicalElements.Add(target.ElementData);
                    target.SetVisualSuppressed(true);
                }
            }

            SpawnCompoundMoleculeAtMidpoint(compound, participatingCards, allObjectPositions);

            var consumed = new List<ElementSO>(activeOrder);
            activeElements.Clear();
            activeOrder.Clear();

            for (int i = 0; i < consumed.Count; i++)
            {
                OnElementQuantityChanged?.Invoke(consumed[i], 0);
            }

            OnCartReset?.Invoke();
            RegisterCompoundDiscovery(compound);
            OnCombinationSuccess?.Invoke(compound);
        }

        private void SpawnCompoundMoleculeAtMidpoint(
            CompoundSO compound,
            List<ElementTargetHandler> participatingCards,
            List<Vector3> allObjectPositions)
        {
            ClearActiveCompoundVisual();

            Vector3 combinedCenter = Vector3.zero;
            if (allObjectPositions.Count > 0)
            {
                for (int i = 0; i < allObjectPositions.Count; i++)
                {
                    combinedCenter += allObjectPositions[i];
                }
                combinedCenter /= allObjectPositions.Count;
            }

            Transform parent = null;
            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;

            if (participatingCards.Count > 0)
            {
                compoundAnchorTargets.AddRange(participatingCards);

                Vector3 physCenter = ComputePhysicalAnchorsCentroid(out Quaternion physRot);
                if (allObjectPositions.Count > participatingCards.Count)
                {
                    compoundMixedLocalOffset = Quaternion.Inverse(physRot) * (combinedCenter - physCenter);
                }
                else
                {
                    compoundMixedLocalOffset = Vector3.zero;
                }

                spawnPos = physCenter + (physRot * compoundMixedLocalOffset);
                spawnRot = physRot;
            }
            else
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    parent = cam.transform;
                    Vector3 worldTarget = allObjectPositions.Count > 0
                        ? combinedCenter
                        : cam.transform.position + cam.transform.forward * fallbackCameraDistance + cam.transform.up * -0.02f;
                    spawnPos = cam.transform.InverseTransformPoint(worldTarget);
                    spawnRot = Quaternion.identity;
                }
                else
                {
                    spawnPos = allObjectPositions.Count > 0 ? combinedCenter : new Vector3(0f, 0f, fallbackCameraDistance);
                    spawnRot = Quaternion.identity;
                }
            }

            if (compound.MoleculeModelPrefab != null)
            {
                activeCompoundInstance = Instantiate(compound.MoleculeModelPrefab, parent);
                activeCompoundInstance.name = $"Molecule_{compound.CompoundId}";
                if (parent != null)
                {
                    activeCompoundInstance.transform.localPosition = spawnPos;
                    activeCompoundInstance.transform.localRotation = spawnRot;
                }
                else
                {
                    activeCompoundInstance.transform.SetPositionAndRotation(spawnPos, spawnRot);
                }
            }
            else
            {
                activeCompoundInstance = BuildPlaceholderMolecule(compound, parent, spawnPos, spawnRot);
            }

            Vector3 puffWorldPos = parent != null ? parent.TransformPoint(spawnPos) : spawnPos;
            SpawnSmokePuff(puffWorldPos);
        }

        private void UpdateCompoundAnchorPose()
        {
            Vector3 physCenter = ComputePhysicalAnchorsCentroid(out Quaternion physRot);
            Vector3 targetPos = physCenter + (physRot * compoundMixedLocalOffset);
            activeCompoundInstance.transform.SetPositionAndRotation(targetPos, physRot);
        }

        private Vector3 ComputePhysicalAnchorsCentroid(out Quaternion averageRotation)
        {
            Vector3 sumPos = Vector3.zero;
            Vector3 sumUp = Vector3.zero;
            Vector3 sumForward = Vector3.zero;
            int valid = 0;

            for (int i = 0; i < compoundAnchorTargets.Count; i++)
            {
                ElementTargetHandler t = compoundAnchorTargets[i];
                if (t == null)
                    continue;

                sumPos += t.transform.TransformPoint(moleculeCardOffset);
                sumUp += t.transform.up;
                sumForward += t.transform.forward;
                valid++;
            }

            if (valid == 0)
            {
                averageRotation = Quaternion.identity;
                return Vector3.zero;
            }

            Vector3 avgUp = (sumUp / valid).normalized;
            if (avgUp.sqrMagnitude < 0.001f)
            {
                avgUp = Vector3.up;
            }

            Vector3 avgForward = Vector3.ProjectOnPlane(sumForward / valid, avgUp).normalized;
            if (avgForward.sqrMagnitude < 0.001f)
            {
                avgForward = Vector3.ProjectOnPlane(Vector3.forward, avgUp).normalized;
                if (avgForward.sqrMagnitude < 0.001f)
                {
                    avgForward = Vector3.right;
                }
            }

            averageRotation = Quaternion.LookRotation(avgForward, avgUp);
            return sumPos / valid;
        }

        private GameObject BuildPlaceholderMolecule(CompoundSO compound, Transform parent, Vector3 pos, Quaternion rot)
        {
            var root = new GameObject($"PlaceholderMolecule_{compound.CompoundId}");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = pos;
                root.transform.localRotation = rot;
            }
            else
            {
                root.transform.SetPositionAndRotation(pos, rot);
            }

            var spinRoot = new GameObject("SpinPivot");
            spinRoot.transform.SetParent(root.transform, false);
            var rotator = spinRoot.AddComponent<ProceduralRotator>();
            rotator.Configure(Vector3.up, 42f, true);

            Material baseMat = database != null && database.PlaceholderAtomMaterial != null
                ? database.PlaceholderAtomMaterial
                : Resources.Load<Material>("ChemistryData/PlaceholderAtomMaterial");

            var atomList = new List<ElementSO>();
            for (int i = 0; i < compound.Ingredients.Count; i++)
            {
                ElementIngredient ing = compound.Ingredients[i];
                if (ing.element == null)
                    continue;

                int clampedCount = Mathf.Min(ing.quantity, 6);
                for (int c = 0; c < clampedCount; c++)
                {
                    atomList.Add(ing.element);
                }
            }

            int centerIndex = 0;
            for (int i = 0; i < compound.Ingredients.Count; i++)
            {
                if (compound.Ingredients[i].quantity == 1 && compound.Ingredients[i].element != null)
                {
                    centerIndex = atomList.IndexOf(compound.Ingredients[i].element);
                    if (centerIndex < 0) centerIndex = 0;
                    break;
                }
            }

            if (atomList.Count > 0)
            {
                ElementSO centerElem = atomList[centerIndex];
                atomList.RemoveAt(centerIndex);

                var centerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                centerSphere.name = $"Atom_{centerElem.Symbol}_Center";
                centerSphere.transform.SetParent(spinRoot.transform, false);
                centerSphere.transform.localScale = Vector3.one * 0.034f;
                ElementTargetHandler.StripCollider(centerSphere);
                ElementTargetHandler.ApplyColor(centerSphere, baseMat, centerElem.ThemeColor);

                float bondLength = 0.042f;
                int outerCount = atomList.Count;
                for (int i = 0; i < outerCount; i++)
                {
                    float angleRad;
                    float yOffset = 0f;
                    if (outerCount == 2 && compound.ChemicalFormula == "H2O")
                    {
                        float halfWaterRad = 52.25f * Mathf.Deg2Rad;
                        float sign = i == 0 ? -1f : 1f;
                        angleRad = sign * halfWaterRad;
                        Vector3 waterDir = new Vector3(Mathf.Sin(angleRad), -0.35f, Mathf.Cos(angleRad) * 0.25f).normalized;
                        CreateOuterAtomAndBond(spinRoot.transform, atomList[i], waterDir * bondLength, baseMat);
                        continue;
                    }

                    angleRad = (i / (float)outerCount) * Mathf.PI * 2f;
                    if (outerCount >= 4)
                    {
                        yOffset = (i % 2 == 0) ? 0.014f : -0.014f;
                    }

                    Vector3 localPos = new Vector3(Mathf.Cos(angleRad) * bondLength, yOffset, Mathf.Sin(angleRad) * bondLength);
                    CreateOuterAtomAndBond(spinRoot.transform, atomList[i], localPos, baseMat);
                }
            }

            var labelObj = new GameObject("CompoundLabel");
            labelObj.transform.SetParent(root.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.044f, 0f);
            labelObj.transform.localScale = Vector3.one * 0.0022f;

            var textMesh = labelObj.AddComponent<TextMesh>();
            Font titleFont = database != null ? database.TitleFont : null;
            if (titleFont != null)
            {
                textMesh.font = titleFont;
                MeshRenderer labelRenderer = labelObj.GetComponent<MeshRenderer>();
                if (labelRenderer != null && titleFont.material != null)
                {
                    labelRenderer.sharedMaterial = titleFont.material;
                }
            }
            textMesh.text = compound.ChemicalFormula;
            textMesh.fontSize = 40;
            textMesh.fontStyle = FontStyle.Normal;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(1f, 0.94f, 0.75f, 1f);

            return root;
        }

        private static void CreateOuterAtomAndBond(Transform parent, ElementSO elem, Vector3 targetPos, Material baseMat)
        {
            var outerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outerSphere.name = $"Atom_{elem.Symbol}";
            outerSphere.transform.SetParent(parent, false);
            outerSphere.transform.localPosition = targetPos;
            outerSphere.transform.localScale = Vector3.one * 0.024f;
            ElementTargetHandler.StripCollider(outerSphere);
            ElementTargetHandler.ApplyColor(outerSphere, baseMat, elem.ThemeColor);

            var bond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bond.name = $"Bond_{elem.Symbol}";
            bond.transform.SetParent(parent, false);
            bond.transform.localPosition = targetPos * 0.5f;
            bond.transform.localRotation = Quaternion.FromToRotation(Vector3.up, targetPos.normalized);
            bond.transform.localScale = new Vector3(0.006f, targetPos.magnitude * 0.5f, 0.006f);
            ElementTargetHandler.StripCollider(bond);
            ElementTargetHandler.ApplyColor(bond, baseMat, new Color(0.92f, 0.94f, 0.98f, 1f));
        }

        private void SpawnSmokePuff(Vector3 worldPosition)
        {
            var puffObj = new GameObject("ReactionSmokePuff");
            puffObj.transform.position = worldPosition;

            var ps = puffObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.032f);
            main.startColor = new Color(0.96f, 0.93f, 0.88f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.018f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.05f));

            var psRenderer = puffObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            psRenderer.mesh = GetBuiltInSphereMesh();
            Material mat = database != null && database.PlaceholderAtomMaterial != null
                ? database.PlaceholderAtomMaterial
                : Resources.Load<Material>("ChemistryData/PlaceholderAtomMaterial");
            if (mat != null)
            {
                psRenderer.sharedMaterial = mat;
            }

            if (Application.isPlaying)
            {
                ps.Play();
                Destroy(puffObj, 1.1f);
            }
            else
            {
                DestroyImmediate(puffObj);
            }
        }

        private Mesh GetBuiltInSphereMesh()
        {
            if (cachedSphereMesh != null)
                return cachedSphereMesh;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cachedSphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroySafe(temp);
            return cachedSphereMesh;
        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        public GameObject CreateStandaloneCompoundVisual(CompoundSO compound)
        {
            if (compound == null)
                return null;

            return BuildPlaceholderMolecule(compound, null, Vector3.zero, Quaternion.identity);
        }

        private void EnsureVirtualVisual(ElementSO element, Vector3? requestedWorldPos)
        {
            if (virtualAtomVisuals.TryGetValue(element, out GameObject existing) && existing != null)
                return;

            DropVisualSpawner dropSpawner = FindAnyObjectByType<DropVisualSpawner>();
            if (dropSpawner != null && (requestedWorldPos.HasValue || dropSpawner.HasVisualForElement(element)))
                return;

            Vector3 spawnPos;
            Transform parent = null;
            Camera cam = Camera.main;

            if (requestedWorldPos.HasValue)
            {
                spawnPos = requestedWorldPos.Value;
            }
            else if (cam != null)
            {
                int slot = virtualAtomVisuals.Count;
                float horizontalSpread = ((slot % 3) - 1) * 0.085f;
                float verticalSpread = ((slot / 3) % 2) * 0.065f;
                parent = cam.transform;
                spawnPos = new Vector3(horizontalSpread, verticalSpread - 0.02f, fallbackCameraDistance);
            }
            else
            {
                spawnPos = new Vector3(0f, 0f, fallbackCameraDistance);
            }

            GameObject visual = ElementTargetHandler.BuildBohrVisual(element, parent, spawnPos);
            if (visual != null)
            {
                visual.name = $"VirtualBohr_{element.Symbol}";
                if (parent == null)
                {
                    visual.transform.position = spawnPos;
                }
                virtualAtomVisuals[element] = visual;
            }
        }

        private void DestroyVirtualVisual(ElementSO element)
        {
            if (element != null && virtualAtomVisuals.TryGetValue(element, out GameObject visual))
            {
                virtualAtomVisuals.Remove(element);
                DestroySafe(visual);
            }
        }

        private void DestroyAllVirtualVisuals()
        {
            foreach (KeyValuePair<ElementSO, GameObject> kvp in virtualAtomVisuals)
            {
                DestroySafe(kvp.Value);
            }
            virtualAtomVisuals.Clear();
        }

        public void ClearActiveCompoundVisual()
        {
            compoundAnchorTargets.Clear();
            compoundMixedLocalOffset = Vector3.zero;
            if (activeCompoundInstance != null)
            {
                DestroySafe(activeCompoundInstance);
                activeCompoundInstance = null;
            }
        }

        private int GetManualExtra(ElementSO element)
        {
            return element != null && manualExtras.TryGetValue(element, out int extra) ? extra : 0;
        }

        private void ApplySuppressionForElement(ElementSO element, bool suppress)
        {
            for (int i = 0; i < trackedTargetOrder.Count; i++)
            {
                ElementTargetHandler target = trackedTargetOrder[i];
                if (target != null && target.ElementData == element)
                {
                    target.SetVisualSuppressed(suppress);
                }
            }
        }

        public void RegisterElementDiscovery(ElementSO element)
        {
            if (element != null && discoveredElements.Add(element))
            {
                discoveredElementOrder.Add(element);
                unreadElements.Add(element);
                OnElementDiscovered?.Invoke(element);
                OnUnreadStateChanged?.Invoke();
            }
        }

        public void RegisterCompoundDiscovery(CompoundSO compound)
        {
            if (compound != null && discoveredCompounds.Add(compound))
            {
                discoveredCompoundOrder.Add(compound);
                unreadCompounds.Add(compound);
                OnCompoundDiscovered?.Invoke(compound);
                OnUnreadStateChanged?.Invoke();
            }
        }

        private void SetReactionLockedInternal(bool locked)
        {
            if (reactionUnlockCoroutine != null)
            {
                StopCoroutine(reactionUnlockCoroutine);
                reactionUnlockCoroutine = null;
            }

            if (IsReactionLocked != locked)
            {
                IsReactionLocked = locked;
                OnReactionLockChanged?.Invoke(locked);
            }

            if (locked && autoUnlockTimeoutSeconds > 0f && Application.isPlaying && isActiveAndEnabled)
            {
                reactionUnlockCoroutine = StartCoroutine(AutoUnlockRoutine());
            }
        }

        private IEnumerator AutoUnlockRoutine()
        {
            float elapsed = 0f;
            while (elapsed < autoUnlockTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            reactionUnlockCoroutine = null;
            UnlockReactionCamera();
        }

        private void RecalculateElementQuantity(ElementSO element, bool forceNotify)
        {
            int effectivePhysical = GetEffectivePhysicalMarkerCount(element);
            int clampedExtra = Mathf.Clamp(GetManualExtra(element), -effectivePhysical, MaxQuantityPerElement - effectivePhysical);
            if (clampedExtra == 0)
            {
                manualExtras.Remove(element);
            }
            else
            {
                manualExtras[element] = clampedExtra;
            }

            int nextTotal = effectivePhysical + clampedExtra;
            int prevTotal = GetQuantity(element);

            if (GetPhysicalMarkerCount(element) > 0)
            {
                ApplySuppressionForElement(element, suppressedPhysicalElements.Contains(element) || nextTotal <= 0);
            }

            if (nextTotal <= 0)
            {
                bool removed = activeElements.Remove(element);
                activeOrder.Remove(element);
                if (removed || forceNotify)
                {
                    OnElementQuantityChanged?.Invoke(element, 0);
                }
                return;
            }

            if (!activeElements.ContainsKey(element))
            {
                activeOrder.Add(element);
            }

            activeElements[element] = nextTotal;
            if (nextTotal != prevTotal || forceNotify)
            {
                OnElementQuantityChanged?.Invoke(element, nextTotal);
            }
        }
    }
}
