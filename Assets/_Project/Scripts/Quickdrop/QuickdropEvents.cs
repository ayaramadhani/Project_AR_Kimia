using System;
using UnityEngine;

public static class QuickdropEvents
{
    // Drop ke area AR (dibaca DropVisualSpawner dan counter Si A)
    public static event Action<ElementData, Vector2> ElementDropped;
    public static event Action<CompoundData, Vector2> CompoundDropped;

    // Permintaan membuka Ensiklopedia (long-press, atau "Lihat Selengkapnya" milik Si A)
    public static event Action<ElementData> ElementInfoRequested;
    public static event Action<CompoundData> CompoundInfoRequested;

    public static void RaiseElementDropped(ElementData e, Vector2 screenPos) => ElementDropped?.Invoke(e, screenPos);
    public static void RaiseCompoundDropped(CompoundData c, Vector2 screenPos) => CompoundDropped?.Invoke(c, screenPos);
    public static void RaiseElementInfoRequested(ElementData e) => ElementInfoRequested?.Invoke(e);
    public static void RaiseCompoundInfoRequested(CompoundData c) => CompoundInfoRequested?.Invoke(c);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ElementDropped = null;
        CompoundDropped = null;
        ElementInfoRequested = null;
        CompoundInfoRequested = null;
    }
}