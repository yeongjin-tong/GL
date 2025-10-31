using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CircuitSolver : MonoBehaviour
{
    public static CircuitSolver Instance { get; private set; }
    private bool isAnalyzing = false; // 재귀 호출 방지 플래그

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// 회로 분석 메인 함수 (ConnectionPoint 기반 경로 탐색)
    /// </summary>
    public void AnalyzeCircuit()
    {
        if (isAnalyzing) return;
        isAnalyzing = true;

        // 1. 초기화
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var component in allComponents) { component.Reset(); component.isPowered = false; }
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires) { wire.ResetColor(); }

        // --- ✨ 핵심 수정: ConnectionPoint를 저장 ---
        // 2. '진입 포트'와 '도착 포트' 찾기
        var entryPoints = new List<ConnectionPoint>(); // Live 신호가 회로로 들어오는 첫 '포트'
        var groundTerminals = new List<ConnectionPoint>(); // Ground 신호의 종착 '포트'
        FindEntryAndGroundPoints(allWires, entryPoints, groundTerminals);

        // 3. 모든 '완성된 경로' 찾기 (DFS)
        //    경로를 List<ConnectionPoint>로 저장
        var allCompletePaths = new List<List<ConnectionPoint>>();
        foreach (var startPoint in entryPoints)
        {
            FindAllPaths(startPoint, groundTerminals, allWires, allCompletePaths);
        }

        // --- ✨ 핵심 수정: 'Powered Ports' 해시셋 생성 ---
        // 4. 완성된 경로에 포함된 *모든 포트*를 HashSet에 저장합니다.
        var poweredPorts = new HashSet<ConnectionPoint>();
        foreach (var path in allCompletePaths)
        {
            foreach (var point in path)
            {
                poweredPorts.Add(point);
            }
        }

        // 5. 'isPowered' 변수 및 PowerOn/Off 호출
        foreach (var component in allComponents)
        {
            // 부품에 속한 포트 중 하나라도 poweredPorts에 포함되면 켠 것으로 간주
            // (더 정확하게는 isLive/isGrounded를 포트 기준으로 설정해야 하지만,
            //  일단 PowerOn/Off는 부품 단위로 처리)
            bool isComponentPowered = component.GetComponentsInChildren<ConnectionPoint>().Any(p => poweredPorts.Contains(p));

            if (isComponentPowered)
            {
                // ✨ isLive/isGrounded도 함께 설정 (릴레이 등을 위해)
                component.isLive = true;
                component.isGrounded = true;
                component.isPowered = true;
                component.PowerOn();
            }
            else
            {
                component.isPowered = false; // AnalyzeCircuit 시작 시 이미 초기화됨
                component.PowerOff();
            }
        }

        // 7. 시각화
        UpdateWireColors(allWires, poweredPorts);

        isAnalyzing = false; // 분석 완료
    }

    /// <summary>
    /// 전선들을 검사하여 회로의 Live 진입 포트와 Ground 도착 포트를 찾습니다.
    /// </summary>
    private void FindEntryAndGroundPoints(Wire[] allWires, List<ConnectionPoint> liveEntryPoints, List<ConnectionPoint> groundTerminals)
    {
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
                    { sourcePort = point; isLiveSource = true; }
                    else if (terminal.type == Terminal.TerminalType.PowerGround)
                    {
                        // ✨ Ground 터미널 '포트' 자체를 도착점으로 추가
                        if (!groundTerminals.Contains(point))
                            groundTerminals.Add(point);
                    }
                }
            }

            if (sourcePort != null && isLiveSource)
            {
                foreach (var point in wire.connectedPoints)
                {
                    // ✨ 터미널 건너편 '포트'를 진입점으로 추가
                    if (point != sourcePort && !liveEntryPoints.Contains(point))
                        liveEntryPoints.Add(point);
                }
            }
        }
    }

    /// <summary>
    /// DFS 알고리즘을 시작하는 함수 (ConnectionPoint 기준)
    /// </summary>
    private void FindAllPaths(ConnectionPoint startNode, List<ConnectionPoint> endNodes, Wire[] allWires, List<List<ConnectionPoint>> allCompletePaths)
    {
        var currentPath = new List<ConnectionPoint>();
        FindPathsRecursive(startNode, null, endNodes, allWires, currentPath, allCompletePaths);
    }

    /// <summary>
    /// 재귀적으로 경로를 탐색하는 함수 (ConnectionPoint 기준, 직렬/병렬 탐색 분리)
    /// </summary>
    private void FindPathsRecursive(
        ConnectionPoint currentPoint,
        ConnectionPoint previousPoint, // 이전 포트 정보
        List<ConnectionPoint> endNodes,
        Wire[] allWires,
        List<ConnectionPoint> currentPath, // ConnectionPoint 경로
        List<List<ConnectionPoint>> allCompletePaths)
    {
        // 1. 현재 포트를 경로에 추가 (사이클 방지용)
        currentPath.Add(currentPoint);

        ElectricalComponent currentComponent = currentPoint.parentComponent;

        // 2. 도착점에 도달했는지 확인
        if (endNodes.Contains(currentPoint))
        {
            allCompletePaths.Add(new List<ConnectionPoint>(currentPath));
        }
        else // 도착지가 아니라면 계속 탐색
        {
            // 3. 전원 장치(종착역) 확인 - 역류 방지
            if (currentComponent.GetComponent<Sym_3P4W>() != null)
            {
                currentPath.Remove(currentPoint);
                return;
            }

            // --- ✨ 4. 탐색 1: 병렬 탐색 (전선을 따라가는 이웃) ---
            //    (스위치 상태와 *상관없이* 항상 탐색)
            foreach (var wire in allWires)
            {
                // 현재 포트(currentPoint)를 포함하는 전선인지 확인
                if (wire.connectedPoints.Contains(currentPoint))
                {
                    // 이 전선에 연결된 '다른' 포트들(neighborPoint)을 순회
                    foreach (var neighborPoint in wire.connectedPoints)
                    {
                        if (neighborPoint == currentPoint) continue; // 자기 자신 건너뛰기

                        // ✨ 핵심: currentPath.Contains()로 사이클 방지
                        if (!currentPath.Contains(neighborPoint))
                        {
                            // 재귀 호출 (현재 포트를 '이전 포트'로 전달)
                            FindPathsRecursive(neighborPoint, currentPoint, endNodes, allWires, currentPath, allCompletePaths);
                        }
                    }
                }
            }

            // --- ✨ 5. 탐색 2: 직렬 탐색 (부품 내부를 통과하는 이웃) ---
            //    (스위치 상태에 *영향을 받음*)
            bool isCurrentSwitchOff = (currentComponent is Switch switchComp && !switchComp.isOn) ||
                                      (currentComponent is RelaySwitch contactComp && !contactComp.isOn);

            // 스위치가 켜져 있거나, 스위치가 아니거나, 릴레이가 아니어야만 통과
            if (!isCurrentSwitchOff)
            {
                // 현재 부품(currentComponent)에 속한 *다른* 모든 포트를 찾습니다.
                ConnectionPoint[] allPortsOnComponent = currentComponent.GetComponentsInChildren<ConnectionPoint>();

                foreach (var internalNeighborPort in allPortsOnComponent)
                {
                    // 현재 포트(currentPoint)는 이미 처리했으므로 건너뜁니다.
                    if (internalNeighborPort == currentPoint) continue;

                    // ✨ 핵심: currentPath.Contains()로 사이클 방지
                    if (!currentPath.Contains(internalNeighborPort))
                    {
                        // 재귀 호출 (현재 포트를 '이전 포트'로 전달)
                        FindPathsRecursive(internalNeighborPort, currentPoint, endNodes, allWires, currentPath, allCompletePaths);
                    }
                }
            }
        }

        // 6. Backtrack
        currentPath.Remove(currentPoint);
    }

    // CircuitSolver.cs의 UpdateWireColors 함수 (수정)

    /// <summary>
    /// isPowered 상태를 기준으로 전선 색상을 변경합니다.
    /// (✨ '첫 번째 전선'과 '마지막 전선'을 특별 처리)
    /// </summary>
    private void UpdateWireColors(Wire[] allWires, HashSet<ConnectionPoint> poweredPorts)
    {
        Color liveColor = Color.red;

        foreach (var wire in allWires)
        {
            // 1. 전선에 연결된 모든 포트가 poweredPorts에 있는지 확인 (기본 검사)
            if (wire.connectedPoints.Count > 0 &&
                wire.connectedPoints.All(p => poweredPorts.Contains(p)))
            {
                wire.SetColor(liveColor);
                continue; // 이 전선은 칠했으므로 다음 전선으로
            }

            // --- ✨ 2. '첫 번째/마지막 전선' 특별 검사 ---
            // (기본 검사에서 실패한 경우에만 실행됨)
            bool isSourceWire = false;
            bool isGroundWire = false;
            bool otherPortIsPowered = false;

            foreach (var point in wire.connectedPoints)
            {
                var terminal = point.GetComponent<Terminal>();
                if (terminal != null)
                {
                    // 이 포트가 PowerSource인지 확인
                    if (terminal.type == Terminal.TerminalType.PowerSource)
                        isSourceWire = true;
                    // 이 포트가 PowerGround인지 확인
                    else if (terminal.type == Terminal.TerminalType.PowerGround)
                        isGroundWire = true;
                }
                // 이 포트가 아닌 '다른' 포트가 poweredPorts에 있는지 확인
                else if (poweredPorts.Contains(point))
                {
                    otherPortIsPowered = true;
                }
            }

            // 3. 최종 판정
            //    (이 선은 Source 선이고, 건너편 포트가 켜져있음)
            //    || (이 선은 Ground 선이고, 건너편 포트가 켜져있음)
            if ((isSourceWire || isGroundWire) && otherPortIsPowered)
            {
                wire.SetColor(liveColor);
            }
            else
            {
                wire.ResetColor(); // 두 조건 모두 아니면 회색
            }
            // --- ✨ 특별 검사 끝 ---
        }
    }
}