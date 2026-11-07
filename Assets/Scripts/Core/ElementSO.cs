using UnityEngine;

namespace ARKimia.Data
{
    [CreateAssetMenu(fileName = "NewElement", menuName = "AR Kimia/Element Data")]
    public class ElementSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string elementId;
        [SerializeField] private string elementName;
        [SerializeField] private string symbol;
        [SerializeField] private int atomicNumber;

        [Header("Visuals")]
        [SerializeField] private Color themeColor = Color.white;
        [SerializeField] private GameObject bohrModelPrefab;
        [SerializeField] private Sprite icon;

        [Header("Information")]
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [TextArea(2, 4)]
        [SerializeField] private string funFact;

        public string ElementId => elementId;
        public string ElementName => elementName;
        public string Symbol => symbol;
        public int AtomicNumber => atomicNumber;
        public Color ThemeColor => themeColor;
        public GameObject BohrModelPrefab => bohrModelPrefab;
        public Sprite Icon => icon;
        public string Description => description;
        public string FunFact => funFact;
    }
}

