using UnityEngine;

// ✨ 기존 Switch 스크립트를 상속받아 모든 기능을 물려받음
public class RelaySwitch : Switch
{
    
    // Awake나 Start에서 isOn의 초기값을 false로 설정하여 항상 꺼진 상태로 시작
    void Awake()
    {
        isOn = false;
    }

    // 사용자가 클릭하지 못하도록 OnMouseDown을 비워버림
    void OnMouseDown()
    {
        // 릴레이 스위치는 코일로만 제어되므로 클릭에 반응하지 않음
    }

    // 코일로부터 켜지라는 명령을 받는 함수
    public void TurnOnByCoil()
    {
        if (isOn) return; // 이미 켜져있으면 무시

        isOn = true;

        // 스위치가 켜졌으므로, 회로 전체를 다시 분석하도록 요청
        if (CircuitSolver.Instance != null)
            CircuitSolver.Instance.AnalyzeCircuit();
    }

    // 코일로부터 꺼지라는 명령을 받는 함수
    public void TurnOffByCoil()
    {
        if (!isOn) return; // 이미 꺼져있으면 무시

        isOn = false;

        // 스위치가 꺼졌으므로, 회로 전체를 다시 분석하도록 요청
        if (CircuitSolver.Instance != null)
            CircuitSolver.Instance.AnalyzeCircuit();
    }
}