using System;
using UnityEngine;
using System.Collections.Generic;

public class Switch : ElectricalComponent
{
    [Tooltip("스위치의 현재 ON/OFF 상태. true = ON")]
    public bool isOn = true;

    // "내 상태가 바뀌었다!"고 외부에 방송하는 이벤트
    public event Action<bool> OnStateChanged;
    public event Action OnStateInit;

    // [복원] 3D 부품 등 1:1로 연결된 파트너
    [HideInInspector]
    public Switch linkedPartner;

    // symbol_ID를 기반으로 모든 스위치를 그룹화하는 정적(static) 딕셔너리
    private static Dictionary<string, List<Switch>> switchGroups = new Dictionary<string, List<Switch>>();

    // initState가 이 스위치의 "비활성" (기본) 상태를 저장합니다.
    private bool initState = false;

    private void Awake()
    {
        // initState는 스위치의 "기본(Default)" 또는 "비활성(Inactive)" 상태입니다.
        // NO 스위치라면 false, NC 스위치라면 true가 됩니다.
        initState = isOn;
    }

    private void OnDisable()
    {
        if (string.IsNullOrEmpty(symbol_ID)) return;
        if (switchGroups.ContainsKey(symbol_ID))
        {
            switchGroups[symbol_ID].Remove(this);
            if (switchGroups[symbol_ID].Count == 0)
            {
                switchGroups.Remove(symbol_ID);
            }
        }
    }

    /// <summary>
    /// ✨ [핵심] 그룹 전체를 "활성" 또는 "비활성(기본)" 상태로 설정합니다.
    /// </summary>
    /// <param name="setActive">true = 활성(!initState), false = 비활성(initState)</param>
    public void TriggerGroupState(bool setActive)
    {
        if (!SimulationManager.isSimulating) return;

        // 1. 이 스위치가 속한 그룹을 찾습니다.
        if (string.IsNullOrEmpty(symbol_ID) || !switchGroups.ContainsKey(symbol_ID))
        {
            // 그룹이 없으면 자기 자신만 상태 변경
            bool targetState = setActive ? !initState : initState;
            SetState(targetState, true); // 3D 파트너에게 알림
        }
        else
        {
            // 2. 그룹이 있으면, 그룹의 *모든* 스위치 상태 변경
            List<Switch> group = new List<Switch>(switchGroups[symbol_ID]);

            foreach (Switch partnerSwitch in group)
            {
                // NO(initState=false) -> setActive? true : false
                // NC(initState=true)  -> setActive? false : true
                bool targetState = setActive ? !partnerSwitch.initState : partnerSwitch.initState;

                // 각 스위치의 상태를 개별적으로 설정 (3D 파트너에게도 알림)
                partnerSwitch.SetState(targetState, true);
            }
        }
    }

    /// <summary>
    /// [유지] 스위치의 상태를 설정하고, 3D 파트너(linkedPartner)에게 알립니다.
    /// </summary>
    public void SetState(bool newState, bool notifyPartner)
    {
        // 시뮬레이션 중이 아니거나, 이미 같은 상태이면 아무것도 하지 않음 (무한 루프 방지)
        if (!SimulationManager.isSimulating || isOn == newState) return;

        isOn = newState;
        CircuitSolver.Instance?.AnalyzeCircuit(); // Dirty Flag 방식
        OnStateChanged?.Invoke(isOn);

        // [유지] 3D 파트너(linkedPartner)에게 상태 동기화
        if (notifyPartner && linkedPartner != null)
        {
            linkedPartner.SetState(isOn, false);
        }
    }

    public override void OnSimulationStart()
    {
        base.OnSimulationStart();
        if (string.IsNullOrEmpty(symbol_ID)) return;
        if (!switchGroups.ContainsKey(symbol_ID))
        {
            switchGroups[symbol_ID] = new List<Switch>();
        }
        switchGroups[symbol_ID].Add(this);
    }

    public override void OnSimulationStop()
    {
        base.OnSimulationStop();
        isOn = initState;                       // 처음 isOn값으로 초기화
        switchGroups.Clear();                   // 이름별로 매칭된 스위치 그룹 초기화
        OnStateChanged?.Invoke(initState);      // 2d 또는 3d 부품 상태 이미지 변경
        OnStateInit?.Invoke();                  // 2d 또는 3d 부품에게 초기화 신호 전달 (각 부품에서 초기화할 부분 알아서 변경)
    }
}