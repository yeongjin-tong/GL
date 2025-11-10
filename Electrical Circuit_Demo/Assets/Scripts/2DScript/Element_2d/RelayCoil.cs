using TMPro;
using UnityEngine;

public class RelayCoil : ElectricalComponent
{
    /// <summary>
    /// CircuitSolver에 의해 isLive와 isGrounded가 모두 true일 때 호출됩니다.
    /// </summary>
    public override void PowerOn()
    {
        base.PowerOn();
        ControlLinkedSwitches(true);
    }

    /// <summary>
    /// CircuitSolver에 의해 전원이 끊겼을 때 호출됩니다.
    /// </summary>
    public override void PowerOff()
    {
        base.PowerOff();
        ControlLinkedSwitches(false);
    }

    /// <summary>
    /// 이 코일과 연결된 모든 릴레이 스위치를 찾아 상태를 변경합니다.
    /// </summary>
    private void ControlLinkedSwitches(bool newState)
    {
        // 씬에 있는 모든 릴레이 스위치를 찾습니다.
        RelaySwitch[] allSwitches = FindObjectsOfType<RelaySwitch>();

        foreach (var relay in allSwitches)
        {
            // ID가 일치하는 스위치만 제어합니다.
            if (relay.symbol_ID == this.symbol_ID)
            {
                relay.SetContactState(newState);
            }
        }
    }
}