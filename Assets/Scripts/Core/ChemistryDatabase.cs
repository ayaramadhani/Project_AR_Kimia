using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARKimia.Data
{
    [CreateAssetMenu(fileName = "ChemistryDatabase", menuName = "AR Kimia/Chemistry Database")]
    public class ChemistryDatabase : ScriptableObject
    {
        [SerializeField] private List<ElementSO> elements = new List<ElementSO>();
        [SerializeField] private List<CompoundSO> compounds = new List<CompoundSO>();

        [Header("Shared Visual & Typography Assets")]
        [SerializeField] private Material placeholderAtomMaterial;
        [SerializeField] private Font logoUnsurFont;
        [SerializeField] private Font titleFont;
        [SerializeField] private Font textFont;

        public IReadOnlyList<ElementSO> Elements => elements;
        public IReadOnlyList<CompoundSO> Compounds => compounds;
        public Material PlaceholderAtomMaterial => placeholderAtomMaterial;
        public Font LogoUnsurFont => logoUnsurFont;
        public Font TitleFont => titleFont;
        public Font TextFont => textFont;

        public CompoundSO GetCompoundByFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return null;

            for (int i = 0; i < compounds.Count; i++)
            {
                CompoundSO comp = compounds[i];
                if (comp != null && string.Equals(comp.ChemicalFormula, formula, StringComparison.OrdinalIgnoreCase))
                    return comp;
            }

            return null;
        }

        public ElementSO GetElementById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < elements.Count; i++)
            {
                ElementSO elem = elements[i];
                if (elem != null && string.Equals(elem.ElementId, id, StringComparison.Ordinal))
                    return elem;
            }

            return null;
        }

        public ElementSO GetElementBySymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return null;

            for (int i = 0; i < elements.Count; i++)
            {
                ElementSO elem = elements[i];
                if (elem != null && string.Equals(elem.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                    return elem;
            }

            return null;
        }

        public CompoundSO EvaluateCombination(IReadOnlyDictionary<ElementSO, int> activeElements)
        {
            if (activeElements == null || activeElements.Count == 0)
                return null;

            for (int i = 0; i < compounds.Count; i++)
            {
                CompoundSO compound = compounds[i];
                if (compound != null && compound.MatchesRecipe(activeElements))
                    return compound;
            }

            return null;
        }

        public bool HasRelatedCompound(IReadOnlyDictionary<ElementSO, int> activeElements)
        {
            if (activeElements == null || activeElements.Count < 2)
                return false;

            for (int i = 0; i < compounds.Count; i++)
            {
                CompoundSO compound = compounds[i];
                if (compound != null && compound.MatchesElementSet(activeElements))
                    return true;
            }

            return false;
        }
    }
}
