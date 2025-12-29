using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class SubStep
{
    public string stepName;                 // 단계 이름
    public string targetPartID;

    [HideInInspector]
    public PartController runtimePartReference;
}

[System.Serializable]
public class MainStep
{
    public string stepName;
    public System.Collections.Generic.List<SubStep> subSteps;
}