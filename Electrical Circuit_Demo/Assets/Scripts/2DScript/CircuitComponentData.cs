using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewComponentData", menuName = "Circuit/Component Data")]
public class CircuitComponentData : ScriptableObject
{
    [Header("기본 정보")]
    public string componentName;
    public Sprite symbolSprite;

    [Header("포트 위치 (로컬 좌표)")]
    // 이 리스트의 개수가 곧 포트의 개수가 됩니다. 0개도 가능합니다.
    public List<Vector2> topPorts = new List<Vector2>();
    public List<Vector2> bottomPorts = new List<Vector2>();

    // 기타 필요한 데이터
    // public float value;
    // public string componentID;
}