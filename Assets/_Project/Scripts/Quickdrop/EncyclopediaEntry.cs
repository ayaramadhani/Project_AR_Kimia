using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Encyclopedia Entry")]
public class EncyclopediaEntry : ScriptableObject
{
    public string key;                      // simbol unsur ("H") atau rumus senyawa ("H2O")
    public string category;                 // "UNSUR" atau "SENYAWA"
    public string subtitle;                 // contoh: "Nomor atom 1 | Massa atom 1,008"
    [TextArea(3, 8)] public string description;
    [TextArea(2, 5)] public string funFact;
}