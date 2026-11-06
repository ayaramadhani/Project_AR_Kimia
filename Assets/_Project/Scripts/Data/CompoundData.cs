using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeItem
{
    public ElementData element;
    public int amount;
}

[CreateAssetMenu(menuName = "Chemistry/Compound")]
public class CompoundData : ScriptableObject
{
    public string formula;          // "CO2"
    public string displayName;      // "Karbon Dioksida"
    [TextArea] public string description;
    public List<RecipeItem> recipe;
    public GameObject modelPrefab;
}
