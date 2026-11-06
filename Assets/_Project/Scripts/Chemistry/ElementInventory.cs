using System;
using System.Collections.Generic;

public class ElementInventory
{
    public Dictionary<ElementData, int> Items = new();
    public event Action OnChanged;

    public void Add(ElementData e)
    {
        Items[e] = Items.TryGetValue(e, out var n) ? n + 1 : 1;
        OnChanged?.Invoke();
    }

    public void Remove(ElementData e)
    {
        if (!Items.ContainsKey(e)) return;
        if (--Items[e] <= 0) Items.Remove(e);
        OnChanged?.Invoke();
    }

    public void Clear() { Items.Clear(); OnChanged?.Invoke(); }
}
