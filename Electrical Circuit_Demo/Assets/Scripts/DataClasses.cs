using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CircuitSaveData
{
    public List<SymbolDataSave> symbols = new List<SymbolDataSave>();
    public List<WireDataSave> wires = new List<WireDataSave>();
}

[System.Serializable]
public class SymbolDataSave
{
    public string symbolID;
    public string prefabName; // 프리팹 식별을 위한 변수 추가
    public string instanceID;
    public Vector3 position;
}

[System.Serializable]
public class WireDataSave
{
    public string startComponentID;
    public int startPortIndex;

    public string endComponentID;
    public int endPortIndex;

    public List<Vector3> pathPoints;
}