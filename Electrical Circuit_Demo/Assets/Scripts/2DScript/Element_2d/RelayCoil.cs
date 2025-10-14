using UnityEngine;

public class RelayCoil : ElectricalComponent
{
    [Tooltip("이 코일과 연결된 스위치 부분을 인스펙터에서 지정합니다.")]
    public RelaySwitch linkedSwitch;

    // 코일에 전원이 들어왔을 때
    public override void PowerOn()
    {
        base.PowerOn(); // isPowered = true;
        if (linkedSwitch != null)
        {
            // 연결된 스위치에게 켜지라고 명령
            linkedSwitch.TurnOnByCoil();
        }
    }

    // 코일의 전원이 꺼졌을 때
    public override void PowerOff()
    {
        base.PowerOff(); // isPowered = false;
        if (linkedSwitch != null)
        {
            // 연결된 스위치에게 꺼지라고 명령
            linkedSwitch.TurnOffByCoil();
        }
    }
}