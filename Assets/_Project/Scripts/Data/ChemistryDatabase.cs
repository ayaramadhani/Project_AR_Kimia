using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChemistryDatabase : MonoBehaviour
{
    public List<ElementData> elements = new();
    public List<CompoundData> compounds = new();

    public ElementData FindByCardName(string imageName) =>
        elements.FirstOrDefault(e => e != null && e.cardImageName == imageName);
}
