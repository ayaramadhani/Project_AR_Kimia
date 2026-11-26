using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GlossaryTerm
{
    public string term;
    [TextArea(2, 6)] public string definition;
}

[CreateAssetMenu(fileName = "GlossaryData", menuName = "Chemistry/Encyclopedia Glossary Data")]
public class EncyclopediaGlossaryData : ScriptableObject
{
    public List<GlossaryTerm> terms = new List<GlossaryTerm>();
}
