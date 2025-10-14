using UnityEngine;

public class Terminal : MonoBehaviour
{
    public enum TerminalType { PowerSource, PowerGround, Neutral, Other }

    [Tooltip("�� ������ ������ �����մϴ� (R=Source, T=Ground ��)")]
    public TerminalType type;

    // �� ���ڰ� ���� �θ� ElectricalComponent�� ����
    [HideInInspector]
    public ElectricalComponent parentComponent;

    void Awake()
    {
        parentComponent = GetComponentInParent<ElectricalComponent>();
    }
}