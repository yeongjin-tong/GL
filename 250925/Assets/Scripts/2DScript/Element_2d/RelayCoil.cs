using UnityEngine;

public class RelayCoil : ElectricalComponent
{
    [Tooltip("�� ���ϰ� ����� ����ġ �κ��� �ν����Ϳ��� �����մϴ�.")]
    public RelaySwitch linkedSwitch;

    // ���Ͽ� ������ ������ ��
    public override void PowerOn()
    {
        base.PowerOn(); // isPowered = true;
        if (linkedSwitch != null)
        {
            // ����� ����ġ���� ������� ���
            linkedSwitch.TurnOnByCoil();
        }
    }

    // ������ ������ ������ ��
    public override void PowerOff()
    {
        base.PowerOff(); // isPowered = false;
        if (linkedSwitch != null)
        {
            // ����� ����ġ���� ������� ���
            linkedSwitch.TurnOffByCoil();
        }
    }
}