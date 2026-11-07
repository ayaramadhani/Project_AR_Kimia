using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARKimia.Data
{
    [Serializable]
    public struct ElementIngredient : IEquatable<ElementIngredient>
    {
        public ElementSO element;
        [Min(1)] public int quantity;

        public ElementIngredient(ElementSO element, int quantity)
        {
            this.element = element;
            this.quantity = Mathf.Max(1, quantity);
        }

        public bool Equals(ElementIngredient other)
        {
            return element == other.element && quantity == other.quantity;
        }

        public override bool Equals(object obj)
        {
            return obj is ElementIngredient other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(element, quantity);
        }
    }

    [CreateAssetMenu(fileName = "NewCompound", menuName = "AR Kimia/Compound Recipe")]
    public class CompoundSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string compoundId;
        [SerializeField] private string compoundName;
        [SerializeField] private string chemicalFormula;

        [Header("Recipe")]
        [SerializeField] private List<ElementIngredient> ingredients = new List<ElementIngredient>();

        [Header("Visuals")]
        [SerializeField] private GameObject moleculeModelPrefab;
        [SerializeField] private Sprite icon;

        [Header("Information")]
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [TextArea(2, 4)]
        [SerializeField] private string applications;

        public string CompoundId => compoundId;
        public string CompoundName => compoundName;
        public string ChemicalFormula => chemicalFormula;
        public IReadOnlyList<ElementIngredient> Ingredients => ingredients;
        public GameObject MoleculeModelPrefab => moleculeModelPrefab;
        public Sprite Icon => icon;
        public string Description => description;
        public string Applications => applications;

        public bool MatchesRecipe(IReadOnlyDictionary<ElementSO, int> activeElements)
        {
            if (activeElements == null || ingredients.Count == 0 || activeElements.Count != ingredients.Count)
                return false;

            for (int i = 0; i < ingredients.Count; i++)
            {
                ElementIngredient ingredient = ingredients[i];
                if (ingredient.element == null)
                    return false;

                if (!activeElements.TryGetValue(ingredient.element, out int count) || count != ingredient.quantity)
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<ElementSO>();
            for (int i = 0; i < ingredients.Count; i++)
            {
                ElementIngredient item = ingredients[i];
                if (item.quantity < 1)
                {
                    item.quantity = 1;
                    ingredients[i] = item;
                }

                if (item.element != null && !seen.Add(item.element))
                {
                    Debug.LogWarning($"[CompoundSO] Duplicate element '{item.element.Symbol}' in recipe '{name}'. Combine quantities into one entry.", this);
                }
            }
        }
#endif
    }
}
