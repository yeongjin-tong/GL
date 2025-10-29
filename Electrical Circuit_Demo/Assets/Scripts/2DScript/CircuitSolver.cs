using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;

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
            component.isPowered = false; // isPowered도 확실하게 초기화
        }
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            wire.ResetColor();
        }

        // 2. '진입점(Entry Points)'과 '도착점(End Nodes)'을 명확히 구분하여 찾습니다.
        var entryPoints = new List<ConnectionPoint>(); // Live 신호가 회로로 들어오는 첫 부품들 - ㄴㄴ 그냥 진입하는 포트
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
                        entryPoints.Add(point);
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
                component.isPowered = true;
            }
        }

        foreach (var component in allComponents)
        {
            if (component.isPowered)
            {
                component.PowerOn(); // 릴레이 코일이 여기서 켜집니다.
            }
            else
            {
                component.PowerOff(); // 릴레이 코일이 여기서 꺼집니다.
            }
        }

        // 5. 전선 색상을 업데이트합니다.
        UpdateWireColors(allWires);
    }

    /// <summary>
    /// DFS(깊이 우선 탐색) 알고리즘을 사용하여 출발점에서 도착점까지의 모든 경로를 찾습니다.
    /// </summary>
    private void FindAllPaths(ConnectionPoint startPoint, List<ElectricalComponent> endNodes, Wire[] allWires, List<List<ElectricalComponent>> allCompletePaths)
    {
        var currentPath = new List<ElectricalComponent>();
        // visited Set은 사용하지 않습니다.

        // ✨ 시작점을 ConnectionPoint가 아닌 ElectricalComponent로 전달
        FindPathsRecursive(startPoint.parentComponent, endNodes, allWires, currentPath, allCompletePaths);
    }

    /// <summary>
    /// 재귀적으로 경로를 탐색하는 함수 (ElectricalComponent 기준)
    /// </summary>
    private void FindPathsRecursive(
        ElectricalComponent currentNode, // ✨ ConnectionPoint 대신 ElectricalComponent 사용
        List<ElectricalComponent> endNodes,
        Wire[] allWires,
        List<ElectricalComponent> currentPath, // visited 대신 이 리스트로 사이클 방지
        List<List<ElectricalComponent>> allCompletePaths)
    {
        // 1. 현재 노드를 현재 경로에 추가 (사이클 방지용)
        currentPath.Add(currentNode);

        // 2. 도착점에 도달했는지 확인
        if (endNodes.Contains(currentNode))
        {
            allCompletePaths.Add(new List<ElectricalComponent>(currentPath)); // 경로 복사해서 저장
            // 도착했어도 다른 병렬 경로가 있을 수 있으므로, 탐색을 멈추지 않고 Backtrack 준비
        }
        else // 도착지가 아니라면 계속 탐색
        {
            // 3. 현재 노드가 경로를 막는지 확인 (꺼진 스위치/릴레이 접점)
            if ((currentNode is Switch switchComp && !switchComp.isOn) ||
                (currentNode is RelaySwitch contactComp && !contactComp.isOn))
            {
                // 막혔으면 여기서 Backtrack (경로에서 제거하고 함수 종료)
                currentPath.Remove(currentNode);
                return;
            }
            // ✨ 전원 장치(종착역) 확인 추가 - 역류 방지 및 불필요 탐색 중단
            if (currentNode.GetComponent<Sym_3P4W>() != null)
            {
                currentPath.Remove(currentNode);
                return;
            }

            // 4. 이웃 노드 탐색: 'currentNode'에 연결된 모든 전선을 확인
            foreach (var wire in allWires)
            {
                // 이 전선이 'currentNode'를 포함하고 있는지 확인
                if (wire.ConnectedComponents.Contains(currentNode))
                {
                    // 그렇다면, 이 전선에 연결된 '다른' 부품들(neighbor)은 모두 이웃임
                    foreach (var neighbor in wire.ConnectedComponents)
                    {
                        // 자기 자신은 건너뛰기
                        if (neighbor == currentNode) continue;

                        // ✨ 핵심: currentPath.Contains()로 사이클(무한 루프)만 방지
                        //        visited가 없으므로 다른 경로가 방문했던 노드도 재방문 가능 (병렬 처리)
                        if (!currentPath.Contains(neighbor))
                        {
                            // 재귀 호출하여 이웃 탐색 계속
                            FindPathsRecursive(neighbor, endNodes, allWires, currentPath, allCompletePaths);
                        }
                    }
                }
            }
        }

        // 5. 현재 노드에서 시작하는 모든 탐색이 끝났으므로, 경로에서 현재 노드를 제거 (Backtrack)
        currentPath.Remove(currentNode);
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