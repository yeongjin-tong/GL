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

    // CircuitSolver.cs의 AnalyzeCircuit 함수 (최종 수정안)

    /// <summary>
    /// 회로 분석을 시작하는 메인 함수입니다. (탐색 경계 수정)
    /// </summary>
    public void AnalyzeCircuit()
    {
        // 1. 모든 부품 및 전선 상태 초기화
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var component in allComponents)
        {
            component.Reset();
            component.PowerOff(); // isPowered도 확실하게 초기화
        }
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            wire.ResetColor();
        }

        // 2. '진입점(Entry Points)'과 '도착점(End Nodes)'을 명확히 구분하여 찾습니다.
        var entryPoints = new List<ElectricalComponent>(); // Live 신호가 회로로 들어오는 첫 부품들
        var groundTerminals = new List<ElectricalComponent>(); // Ground 신호의 종착점

        foreach (var wire in allWires)
        {
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
                    }
                    else if (terminal.type == Terminal.TerminalType.PowerGround)
                    {
                        // Ground 터미널 자체를 도착점으로 지정합니다.
                        groundTerminals.Add(point.parentComponent);
                    }
                }
            }

            // 만약 이 전선이 PowerSource에 연결되어 있다면,
            if (sourcePort != null && isLiveSource)
            {
                // 전선 건너편에 있는 모든 부품들을 '진입점'으로 등록합니다.
                foreach (var point in wire.connectedPoints)
                {
                    if (point != sourcePort)
                    {
                        entryPoints.Add(point.parentComponent);
                    }
                }
            }
        }

        // 3. 모든 '완성된 경로'를 찾습니다.
        var allCompletePaths = new List<List<ElectricalComponent>>();
        // 각각의 '진입점'에서부터 모든 '도착점'까지의 경로를 탐색합니다.
        foreach (var startNode in entryPoints)
        {
            FindAllPaths(startNode, groundTerminals, allWires, allCompletePaths);
        }

        // --- 여기까지 수정 ---

        // 4. 완성된 경로에 포함된 모든 부품들을 'Powered' 상태로 만듭니다.
        foreach (var path in allCompletePaths)
        {
            foreach (var component in path)
            {
                component.isLive = true;
                component.isGrounded = true;
                component.PowerOn();
            }
        }

        // 5. 전선 색상을 업데이트합니다.
        UpdateWireColors(allWires);
    }

    /// <summary>
    /// DFS(깊이 우선 탐색) 알고리즘을 사용하여 출발점에서 도착점까지의 모든 경로를 찾습니다.
    /// </summary>
    private void FindAllPaths(ElectricalComponent startNode, List<ElectricalComponent> endNodes, Wire[] allWires, List<List<ElectricalComponent>> allCompletePaths)
    {
        var currentPath = new List<ElectricalComponent>();
        var visited = new HashSet<ElectricalComponent>();

        // 재귀적으로 경로 탐색 시작
        FindPathsRecursive(startNode, endNodes, allWires, currentPath, visited, allCompletePaths, false);
    }

    private void FindPathsRecursive(
        ElectricalComponent currentNode,
        List<ElectricalComponent> endNodes,
        Wire[] allWires,
        List<ElectricalComponent> currentPath,
        HashSet<ElectricalComponent> visited,
        List<List<ElectricalComponent>> allCompletePaths, bool isRecursion)
    {
        // 현재 노드를 방문 처리하고 현재 경로에 추가
        visited.Add(currentNode);
        currentPath.Add(currentNode);

        // 만약 현재 노드가 도착점 중 하나라면, 경로를 찾은 것임
        if (endNodes.Contains(currentNode))
        {
            allCompletePaths.Add(new List<ElectricalComponent>(currentPath)); // 현재 경로를 복사하여 결과 목록에 추가
        }
        else // 도착지가 아니라면 계속 탐색
        {
            // 현재 노드가 꺼진 스위치라면 더 이상 진행하지 않고 되돌아감
            if (currentNode is Switch switchComp && !switchComp.isOn)
            {
                // Backtrack
                currentPath.Remove(currentNode);
                visited.Remove(currentNode);
                return;
            }

            // 현재 노드와 연결된 이웃들을 찾아서 재귀 호출
            foreach (var wire in allWires)
            {
                if (wire.ConnectedComponents.Contains(currentNode))
                {
                    foreach (var neighbor in wire.ConnectedComponents)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            if(!isRecursion && endNodes.Contains(neighbor))
                            {
                                continue;
                            }
                            else
                            {
                                FindPathsRecursive(neighbor, endNodes, allWires, currentPath, visited, allCompletePaths, true);
                            }
                        }
                    }
                }
            }
        }

        // 현재 노드에서 시작하는 모든 경로 탐색이 끝났으므로, 이전 노드로 되돌아감 (Backtracking)
        currentPath.Remove(currentNode);
        visited.Remove(currentNode);
    }

    /// <summary>
    /// isPowered 상태를 기준으로 전선 색상을 변경합니다.
    /// </summary>
    private void UpdateWireColors(Wire[] allWires)
    {
        Color liveColor = Color.red;
        foreach (var wire in allWires)
        {
            // 전선에 연결된 모든 부품이 isPowered 상태일 때만 색상 변경
            if (wire.ConnectedComponents.Count > 0 && wire.ConnectedComponents.All(c => c.isPowered))
            {
                wire.SetColor(liveColor);
            }
            else
            {
                wire.ResetColor();
            }
        }
    }
}