using System;
using UnityEngine;

public class Battery : ElectricalComponent
{
    [Tooltip("배터리의 현재 ON/OFF 상태")]
    public bool isOn = false; // true = 켜짐(전류 흐름)
    public event Action<bool> OnStateChanged;

    [HideInInspector]
    public Battery linkedPartner;
    private bool isBeingUpdatedByPartner = false;

    // 누르는 순간 호출될 함수
    public void ToggleEvent()
    {
        if (!SimulationManager.isSimulating) return;

        if(isOn)
        {
            isOn = false;
        }
        else
        {
            isOn = true;
        }

        UpdateStateAndNotify();
    }


    // 파트너로부터 상태를 업데이트 받는 함수
    public void SetState(bool newState)
    {
        isBeingUpdatedByPartner = true;
        if (isOn != newState)
        {
            isOn = newState;
            OnStateChanged?.Invoke(isOn);
            UpdateStateAndNotify(false); // 파트너에게 다시 신호를 보내지 않음
        }
        isBeingUpdatedByPartner = false;
    }

    // 상태 변경, 회로 분석, 방송을 함께 처리하는 공통 함수
    private void UpdateStateAndNotify(bool notifyPartner = true)
    {
        if (isBeingUpdatedByPartner) return;

        Debug.Log($"{gameObject.name} (ID: {uniqueID}) 상태 변경: {isOn}");
        CircuitSolver.Instance?.AnalyzeCircuit();
        OnStateChanged?.Invoke(isOn);

        if (notifyPartner && linkedPartner != null)
        {
            linkedPartner.SetState(isOn);
        }
    }
}