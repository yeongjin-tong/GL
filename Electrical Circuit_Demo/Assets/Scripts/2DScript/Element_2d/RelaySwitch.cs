// RelayContact.cs (새 스크립트 파일)
using System;
using TMPro;
using UnityEngine;

public class RelaySwitch : ElectricalComponent
{
    [Tooltip("이 접점을 제어할 코일의 ID입니다.")]
    public string relayID;

    [Tooltip("접점의 현재 ON/OFF 상태. true = ON (Closed)")]
    public bool isOn = false; // 기본값은 '열림' (꺼짐)

    // 시각적 표현(UI 등)을 위한 이벤트
    public event Action<bool> OnStateChanged;

    /// <summary>
    /// RelayCoil이 이 함수를 호출하여 스위치 상태를 강제로 변경합니다.
    /// </summary>
    public void SetContactState(bool newState)
    {
        // 이미 같은 상태이거나, 시뮬레이션 중이 아니면 무시 (무한 루프 방지)
        if (isOn == newState || !SimulationManager.isSimulating) return;

        isOn = newState;
        OnStateChanged?.Invoke(isOn); // 시각 효과를 위해 이벤트 방송

        // ✨ 중요: 상태가 바뀌었으니 회로 재분석 요청
        CircuitSolver.Instance?.AnalyzeCircuit();
    }

    public override void OnSimulationStart()
    {
        foreach (TextMeshProUGUI tmp in gameObject.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (tmp.name == "name_text")
            {
                relayID = tmp.text;
            }
        }
    }
}