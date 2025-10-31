using System;
using UnityEngine;

public class Switch : ElectricalComponent
{
    [Tooltip("스위치의 현재 ON/OFF 상태. true = ON")]
    public bool isOn = true;

    // "내 상태가 바뀌었다!"고 외부에 방송하는 이벤트
    public event Action<bool> OnStateChanged;

    // 초기화 방송
    public event Action OnStateInit;

    // 링크된 파트너 스위치를 저장할 변수
    [HideInInspector]
    public Switch linkedPartner;

    private bool initState = false;

    private void Awake()
    {
        // 초기화를 해주기 위해 저장하는 초기값
        initState = isOn;
    }

    /// <summary>
    /// 외부(주로 링크된 파트너)에서 스위치의 상태를 특정 값으로 설정할 때 호출하는 함수.
    /// </summary>
    public void SetState(bool newState, bool notifyPartner)
    {
        // 시뮬레이션 중이 아니거나, 이미 같은 상태이면 아무것도 하지 않음 (무한 루프 방지)
        if (!SimulationManager.isSimulating || isOn == newState) return;

        // 1. 상태를 새로운 값으로 변경
        isOn = newState;

        // 2. 상태가 변경되었으므로, 회로 전체를 다시 분석하도록 Solver에 요청
        CircuitSolver.Instance?.AnalyzeCircuit();

        // 3. 'OnStateChanged' 이벤트를 방송하여, 구독 중인 모든 View(2D, 3D 컨트롤러)에게 상태 변경을 알림
        OnStateChanged?.Invoke(isOn);

        // 4. 파트너에게 알려야 하고, 파트너가 존재한다면 파트너의 SetState도 호출
        if (notifyPartner && linkedPartner != null)
        {
            // 파트너에게는 "너는 나한테 다시 알릴 필요 없어" 라는 의미로 false를 전달
            linkedPartner.SetState(isOn, false);
        }
    }

    public override void OnSimulationStop()
    {
        base.OnSimulationStop();
        isOn = initState;
        OnStateChanged?.Invoke(initState);
        OnStateInit?.Invoke();
    }
}