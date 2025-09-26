using System;
using UnityEngine;
using static UnityEngine.CullingGroup;

public class Switch : ElectricalComponent
{
    public bool isOn = true;
    public event Action<bool> OnStateChanged;

    // 링크된 파트너 스위치를 저장할 변수
    [HideInInspector]
    public Switch linkedPartner;

    //  무한 피드백 루프를 방지하기 위한 플래그
    private bool isBeingUpdatedByPartner = false;

    // 사용자가 직접 클릭하거나 외부에서 호출할 함수
    public void Toggle()
    {
        if (!SimulationManager.isSimulating) return;

        // 파트너에 의해 업데이트 중이라면, 다시 파트너에게 신호를 보내지 않음 (무한 루프 방지)
        if (isBeingUpdatedByPartner) return;

        // 1. 자신의 상태를 변경
        isOn = !isOn;

        OnStateChanged?.Invoke(isOn);

        // 2. 자신의 회로를 재분석하도록 요청 (2D 또는 3D)
        CircuitSolver.Instance?.AnalyzeCircuit();

        // 3. 링크된 파트너가 있다면, 파트너의 상태도 업데이트하도록 명령
        if (linkedPartner != null)
        {
            linkedPartner.SetState(isOn);
        }
    }

    // 파트너로부터 상태를 업데이트 받기 위한 함수
    public void SetState(bool newState)
    {
        isBeingUpdatedByPartner = true; // 지금부터는 파트너에 의한 업데이트임을 표시

        isOn = newState;
        CircuitSolver.Instance?.AnalyzeCircuit();

        isBeingUpdatedByPartner = false; // 업데이트 완료 후 플래그 해제
        OnStateChanged?.Invoke(isOn);
    }

}