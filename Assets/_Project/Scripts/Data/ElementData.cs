using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Element")]
public class ElementData : ScriptableObject
{
    public string symbol;          // "C"
    public string displayName;     // "Carbon"
    public int atomicNumber;
    public string cardImageName;   // harus sama dengan nama gambar di Reference Image Library
    public GameObject markerPrefab;
}
