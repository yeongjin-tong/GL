using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CircuitSolver : MonoBehaviour
{
    public static CircuitSolver Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    // CircuitSolver.cs의 AnalyzeCircuit 함수 (진짜 최종 수정안)

    /// <summary>
    /// 회로 분석을 시작하는 메인 함수입니다. (Wire 기준 로직으로 전면 수정)
    /// </summary>
    public void AnalyzeCircuit()
    {
        // 1. 모든 부품 및 전선 상태 초기화
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var component in allComponents)
        {
            component.Reset();
        }
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            wire.ResetColor();
        }

        // 2. ✨ '전선에 직접 연결된' 출발점을 찾는 새로운 로직
        var liveStartingPoints = new HashSet<ElectricalComponent>();
        var groundStartingPoints = new HashSet<ElectricalComponent>();

        foreach (var wire in allWires)
        {
            // 이 전선에 PowerSource나 PowerGround 터미널이 연결되어 있는지 확인
            ConnectionPoint sourcePort = null;
            bool isLiveSource = false;

            foreach (var point in wire.connectedPoints)
            {
                var terminal = point.GetComponent<Terminal>();
                if (terminal != null)
                {
                    var parentPowerSource = terminal.GetComponentInParent<Sym_3P4W>();
                    if (terminal.type == Terminal.TerminalType.PowerSource && (parentPowerSource == null || parentPowerSource.isOn))
                    {
                        sourcePort = point;
                        isLiveSource = true;
                        break; // Live 출발점을 찾았으므로 더 이상 이 전선의 포트를 볼 필요 없음
                    }
                    else if (terminal.type == Terminal.TerminalType.PowerGround)
                    {
                        sourcePort = point;
                        isLiveSource = false;
                        break; // Ground 출발점을 찾았으므로 더 이상 이 전선의 포트를 볼 필요 없음
                    }
                }
            }

            // 만약 이 전선이 출발점(SourcePort)에 연결되어 있다면,
            if (sourcePort != null)
            {
                // ✨ 전선 건너편에 있는 모든 부품들을 신호 전파의 '시작점'으로 등록합니다.
                foreach (var point in wire.connectedPoints)
                {
                    if (point != sourcePort) // 출발점 포트 자신은 제외
                    {
                        if (isLiveSource)
                            liveStartingPoints.Add(point.parentComponent);
                        else
                            groundStartingPoints.Add(point.parentComponent);
                    }
                }
            }
        }

        // 3. 찾은 출발점에서부터 신호를 전파합니다.
        foreach (var startPoint in liveStartingPoints)
        {
            FloodFillSignal(startPoint, isLiveSignal: true, allWires);
        }
        foreach (var startPoint in groundStartingPoints)
        {
            FloodFillSignal(startPoint, isLiveSignal: false, allWires);
        }

        // 4. 최종적으로 각 부품의 전원 상태를 결정합니다.
        foreach (var component in allComponents)
        {
            if (component.isLive && component.isGrounded)
                component.PowerOn();
            else
                component.PowerOff();
        }

        // 5. 전선 색상을 업데이트합니다.
        UpdateWireColors(allWires);
    }

    /// <summary>
    /// ✨ 전선을 따라가며 신호를 전파하는 최종 FloodFill 함수
    /// </summary>
    private void FloodFillSignal(ElectricalComponent startComponent, bool isLiveSignal, Wire[] allWires)
    {
        if (startComponent == null) return;
        if (isLiveSignal && startComponent.isLive) return;
        if (!isLiveSignal && startComponent.isGrounded) return;

        Queue<ElectricalComponent> queue = new Queue<ElectricalComponent>();
        HashSet<ElectricalComponent> visited = new HashSet<ElectricalComponent>();

        queue.Enqueue(startComponent);
        visited.Add(startComponent);

        while (queue.Count > 0)
        {
            ElectricalComponent current = queue.Dequeue();

            

            if (current is Switch switchComp && !switchComp.isOn)
            {
                continue;
            }

            if (isLiveSignal) current.isLive = true;
            else current.isGrounded = true;

            if (current.GetComponent<Sym_3P4W>() != null)
            {
                continue;
            }

            // ✨ '전선을 타고' 이웃을 찾는 로직
            foreach (var wire in allWires)
            {
                // 이 전선이 현재 부품(current)을 포함하고 있는지 확인
                if (wire.ConnectedComponents.Contains(current))
                {
                    // 그렇다면, 이 전선에 연결된 다른 모든 부품들은 이웃입니다.
                    foreach (var neighbor in wire.ConnectedComponents)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 완성된 회로의 전선만 색상을 변경하도록 수정한 함수
    /// </summary>
    private void UpdateWireColors(Wire[] allWires)
    {
        Color liveColor = Color.red;
        foreach (var wire in allWires)
        {
            // ✨ isPowered 상태를 기준으로 색상 변경
            // isPowered는 isLive && isGrounded일 때만 true가 됩니다.
            bool isWirePowered = wire.ConnectedComponents.All(c => c.isPowered);

            if (isWirePowered)
            {
                wire.SetColor(liveColor);
            }
            else
            {
                // ✨ isLive 신호만 있을 경우(회로 미완성)는 주황색으로 표시 (선택 사항)
                bool isWireLive = wire.ConnectedComponents.All(c => c.isLive);
                if (isWireLive)
                {
                    wire.SetColor(Color.yellow);
                }
                else
                {
                    wire.ResetColor();
                }
            }
        }
    }
}