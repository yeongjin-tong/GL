using UnityEngine;

public class Terminal : MonoBehaviour
{
    public enum TerminalType { PowerSource, PowerGround, Neutral, Other }

    [Tooltip("이 단자의 역할을 지정합니다 (R=Source, T=Ground 등)")]
    public TerminalType type;

    // 이 단자가 속한 부모 ElectricalComponent를 저장
    [HideInInspector]
    public ElectricalComponent parentComponent;

    void Awake()
    {
        parentComponent = GetComponentInParent<ElectricalComponent>();
    }
}