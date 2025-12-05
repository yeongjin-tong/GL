using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EOCR : ElectricalComponent
{
    public Button testBtn;
    public Button resetBtn;

    public bool isOn;

    private BoxCollider2D myCollider;

    public override void Awake()
    {
        base.Awake();
        testBtn.onClick.AddListener(() => { ClickEvent(true); });
        resetBtn.onClick.AddListener(() => { ClickEvent(false); });

        myCollider = gameObject.GetComponent<BoxCollider2D>();
    }

    public override void OnSimulationStart()
    {
        base.OnSimulationStart();
        isOn = false;
        myCollider.enabled = false;
    }
    public override void OnSimulationStop()
    {
        base.OnSimulationStop();
        myCollider.enabled = true;
    }

    /// <summary>
    /// CircuitSolver에 의해 isLive와 isGrounded가 모두 true일 때 호출됩니다.
    /// </summary>
    public override void PowerOn()
    {
        base.PowerOn();
        ControlLinkedSwitches(isOn, Type.EOCR);
    }

    /// <summary>
    /// CircuitSolver에 의해 전원이 끊겼을 때 호출됩니다.
    /// </summary>
    public override void PowerOff()
    {
        base.PowerOff();
        ControlLinkedSwitches(false, Type.EOCR);
    }

    /// <summary>
    /// 이 코일과 연결된 모든 릴레이 스위치를 찾아 상태를 변경합니다.
    /// </summary>
    private void ControlLinkedSwitches(bool newState, Type type)
    {
        // 씬에 있는 모든 릴레이 스위치를 찾습니다.
        RelaySwitch[] allSwitches = FindObjectsOfType<RelaySwitch>();

        foreach (var relay in allSwitches)
        {
            // ID가 일치하는 스위치만 제어합니다.
            if (relay.switchType == type)
            {
                bool initState = relay.GetInitState();

                bool targetState;

                if (newState)
                {
                    targetState = !initState;
                }
                else
                {
                    targetState = initState;
                }

                relay.SetContactState(targetState);
            }
        }
    }

    private void ClickEvent(bool b)
    {
        isOn = b;
        CircuitSolver.Instance?.AnalyzeCircuit();
    }
}