// RelayCoil.cs (새 스크립트 파일)
using TMPro;
using UnityEngine;

public class RelayCoil : ElectricalComponent
{
    [Tooltip("이 코일이 제어할 릴레이 스위치들의 ID입니다.")]
    public string relayID;

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

    public override void OnSimulationStart()
    {
        foreach(TextMeshProUGUI tmp in gameObject.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if(tmp.name == "name_text")
            {
                relayID = tmp.text;
            }
        }
    }

    /// <summary>
    /// 이 코일과 연결된 모든 릴레이 스위치를 찾아 상태를 변경합니다.
    /// </summary>
    private void ControlLinkedSwitches(bool newState)
    {
        // 씬에 있는 모든 릴레이 스위치를 찾습니다.
        RelaySwitch[] allSwitches = FindObjectsOfType<RelaySwitch>();

        foreach (var sw in allSwitches)
        {
            // ID가 일치하는 스위치만 제어합니다.
            if (sw.relayID == this.relayID)
            {
                sw.SetContactState(newState);
            }
        }
    }
}