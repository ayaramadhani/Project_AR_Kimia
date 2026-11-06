using System.Linq;
using UnityEngine;

public class ReactionService : MonoBehaviour
{
    public ChemistryDatabase db;

    public CompoundData TryCombine(ElementInventory inv)
    {
        foreach (var c in db.compounds)
        {
            if (c == null || c.recipe.Count != inv.Items.Count) continue;

            bool match = c.recipe.All(r =>
                inv.Items.TryGetValue(r.element, out int n) && n == r.amount);

            if (match) return c;
        }
        return null;
    }
}
