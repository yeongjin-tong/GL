using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Sym_3P4W : ElectricalComponent
{
    // 이 부품의 ON/OFF 상태를 관리 (기존 Switch와 유사)
    public bool isOn = true;

    // ✨ 이 부품에 속한 모든 단자(Terminal)들의 목록
    private List<Terminal> terminals = new List<Terminal>();

    void Awake()
    {
        // 시작할 때 자신의 자식들 중에서 모든 Terminal을 찾아 리스트에 저장
        terminals = GetComponentsInChildren<Terminal>().ToList();
    }

    // ✨ 이 부품이 가진 '전원' 역할의 단자를 찾아 반환하는 함수
    public Terminal GetPowerSourceTerminal()
    {
        return terminals.FirstOrDefault(t => t.type == Terminal.TerminalType.PowerSource);
    }

    // ✨ 이 부품이 가진 '접지' 역할의 단자를 찾아 반환하는 함수
    public Terminal GetPowerGroundTerminal()
    {
        return terminals.FirstOrDefault(t => t.type == Terminal.TerminalType.PowerGround);
    }

    // (선택) 사용자가 클릭하여 ON/OFF 할 수 있는 기능
    //private void OnMouseDown()
    //{
    //    if (!SimulationManager.isSimulating) return;

    //    isOn = !isOn;
    //    Debug.Log($"{uniqueID} 전원 상태: {isOn}");
    //    CircuitSolver.Instance?.AnalyzeCircuit();
    //}
}