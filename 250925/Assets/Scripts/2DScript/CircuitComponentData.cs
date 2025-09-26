using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewComponentData", menuName = "Circuit/Component Data")]
public class CircuitComponentData : ScriptableObject
{
    [Header("�⺻ ����")]
    public string componentName;
    public Sprite symbolSprite;

    [Header("��Ʈ ��ġ (���� ��ǥ)")]
    // �� ����Ʈ�� ������ �� ��Ʈ�� ������ �˴ϴ�. 0���� �����մϴ�.
    public List<Vector2> topPorts = new List<Vector2>();
    public List<Vector2> bottomPorts = new List<Vector2>();

    // ��Ÿ �ʿ��� ������
    // public float value;
    // public string componentID;
}