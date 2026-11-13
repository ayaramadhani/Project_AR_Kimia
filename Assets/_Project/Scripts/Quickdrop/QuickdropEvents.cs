using System;
using UnityEngine;
using ARKimia.Data;

public static class QuickdropEvents
{
    public static event Action<ElementSO, Vector2> ElementDropped;
    public static event Action<CompoundSO, Vector2> CompoundDropped;

    public static event Action<ElementSO> ElementInfoRequested;
    public static event Action<CompoundSO> CompoundInfoRequested;

    public static void RaiseElementDropped(ElementSO e, Vector2 screenPos) => ElementDropped?.Invoke(e, screenPos);
    public static void RaiseCompoundDropped(CompoundSO c, Vector2 screenPos) => CompoundDropped?.Invoke(c, screenPos);
    public static void RaiseElementInfoRequested(ElementSO e) => ElementInfoRequested?.Invoke(e);
    public static void RaiseCompoundInfoRequested(CompoundSO c) => CompoundInfoRequested?.Invoke(c);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ElementDropped = null;
        CompoundDropped = null;
        ElementInfoRequested = null;
        CompoundInfoRequested = null;
    }
}
