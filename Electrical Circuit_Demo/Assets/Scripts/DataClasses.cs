using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class CircuitSaveData
{
    public List<SymbolDataSave> symbols = new List<SymbolDataSave>();
    public List<WireDataSave> wires = new List<WireDataSave>();
}

[System.Serializable]
public class SymbolDataSave : ISerializationCallbackReceiver
{
    public string symbolID;
    public string prefabName; // 프리팹 식별을 위한 변수 추가
    public string instanceID;
    public Vector3 position;
    public float timerNum;
    public Dictionary<string, string> portAndIndex = new Dictionary<string, string>();

    [SerializeField] private List<string> _portKeys = new List<string>();
    [SerializeField] private List<string> _portValues = new List<string>();

    // ✨ [추가] 저장하기 직전에 호출됨 (Dictionary -> List)
    public void OnBeforeSerialize()
    {
        _portKeys.Clear();
        _portValues.Clear();

        foreach (var kvp in portAndIndex)
        {
            _portKeys.Add(kvp.Key);
            _portValues.Add(kvp.Value);
        }
    }

    // ✨ [추가] 불러온 직후에 호출됨 (List -> Dictionary)
    public void OnAfterDeserialize()
    {
        portAndIndex = new Dictionary<string, string>();

        int count = Math.Min(_portKeys.Count, _portValues.Count);
        for (int i = 0; i < count; i++)
        {
            // 중복 키 방지 안전장치
            if (!portAndIndex.ContainsKey(_portKeys[i]))
            {
                portAndIndex.Add(_portKeys[i], _portValues[i]);
            }
        }
    }
}

[System.Serializable]
public class WireDataSave
{
    public string startComponentID;
    public string startPortIndex;

    public string endComponentID;
    public string endPortIndex;

    public List<Vector3> pathPoints;
}