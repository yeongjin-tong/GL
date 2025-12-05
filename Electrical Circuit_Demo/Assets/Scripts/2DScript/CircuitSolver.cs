using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CircuitSolver : MonoBehaviour
{
    public static CircuitSolver Instance { get; private set; }
    private bool isAnalyzing = false; // 재귀 호출 방지 플래그
    private bool analysisPending = false;

    // ✨ [새 변수] Nodal Analysis를 위한 넷(Net) 리스트
    private List<HashSet<ConnectionPoint>> allNets;
    private HashSet<ConnectionPoint> liveNet;
    private HashSet<ConnectionPoint> groundNet;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        allNets = new List<HashSet<ConnectionPoint>>();
        liveNet = new HashSet<ConnectionPoint>();
        groundNet = new HashSet<ConnectionPoint>();
    }

    void Update()
    {
        if (SimulationManager.isSimulating && analysisPending)
        {
            analysisPending = false;
            AnalyzeCircuit();
        }
    }

    public void RequestAnalysis()
    {
        analysisPending = true;
    }

    /// <summary>
    /// ✨ [핵심 수정] Nodal Analysis (Dijkstra/BFS 방식) 메인 함수
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

        // --- 1단계: 모든 "Net" 찾기 (BFS/DFS 탐색) ---
        allNets.Clear();
        liveNet.Clear();
        groundNet.Clear();
        HashSet<ConnectionPoint> visitedPorts = new HashSet<ConnectionPoint>(); // 전체 방문 기록
        ConnectionPoint[] allPoints = FindObjectsOfType<ConnectionPoint>();

        foreach (var point in allPoints)
        {
            if (!visitedPorts.Contains(point))
            {
                // 새 Net을 생성하고, BFS/DFS로 이 Net에 연결된 모든 포트를 찾음
                HashSet<ConnectionPoint> newNet = new HashSet<ConnectionPoint>();
                FindNetRecursive(point, newNet, visitedPorts, allWires);

                if (newNet.Count > 0)
                {
                    allNets.Add(newNet);
                }
            }
        }

        // --- 2단계: Live Net과 Ground Net 식별 ---
        FindLiveAndGroundNets();

        // --- 3단계: 부품 전원 인가 ---
        foreach (var component in allComponents)
        {
            // (스위치, 전원 등은 전력 소모 부품이 아니므로 제외)
            if (component is Switch || component is RelaySwitch || component is Sym_3P4W)
            {
                continue;
            }

            // ✨ [추가] 모터(Motor) 전용 로직
            if (component is Motor)
            {
                int liveConnectionCount = 0;

                ConnectionPoint[] motorPorts = component.GetComponentsInChildren<ConnectionPoint>();

                foreach (var port in motorPorts)
                {
                    if (liveNet.Contains(port))
                    {
                        liveConnectionCount++;
                    }
                }

                // 3상(R,S,T)이 모두 연결되어야 작동
                if (liveConnectionCount >= 3)
                {
                    component.isPowered = true;
                    component.PowerOn();

                    // ✨ [중요] 모터가 켜지면 모터의 포트들도 '전류가 흐르는 곳'으로 등록되어야
                    // 색칠 BFS가 모터를 통과하지 못하더라도(부하니까) 모터까지의 선은 칠해짐
                    // (AnalyzeCircuit에서는 poweredPorts를 쓰지 않고 component.isPowered만 켜두면, 
                    //  UpdateWireColors가 알아서 처리합니다.)
                }
                else
                {
                    component.isPowered = false;
                    component.PowerOff();
                }
                continue;
            }

            // (RL, Timer, RelayCoil 등 "부하" 부품만 검사)
            ConnectionPoint[] ports = component.GetComponentsInChildren<ConnectionPoint>();
            if (ports.Length < 2) continue; // 포트가 2개 미만인 부하는 무시

            // 부하의 양단 포트
            ConnectionPoint portA = ports[0];
            ConnectionPoint portB = ports[1];

            bool isA_Live = liveNet.Contains(portA);
            bool isA_Ground = groundNet.Contains(portA);
            bool isB_Live = liveNet.Contains(portB);
            bool isB_Ground = groundNet.Contains(portB);

            // ✨ [핵심 수정] 전원 인가 조건 완화 (Line-to-Line 허용)
            // 조건: 양쪽 다 전압(Live/Ground)이 들어와야 하며, 둘 다 Ground인 경우는 제외
            // (즉, Live-Ground (220V) 또는 Live-Live (380V, R-T 연결) 모두 허용)
            bool hasPotentialDifference = (isA_Live || isA_Ground) &&
                                          (isB_Live || isB_Ground) &&
                                          !(isA_Ground && isB_Ground);

            if (hasPotentialDifference)
            {
                component.isLive = true;
                component.isGrounded = true;
                component.isPowered = true;
                component.PowerOn();
            }
            else
            {
                component.isPowered = false;
                component.PowerOff();
            }
        }

        // 4. 와이어 색상 업데이트
        // (Live Net과 Ground Net에 속한 모든 포트를 poweredPorts에 추가)
        
        UpdateWireColors(allWires, allComponents);

        isAnalyzing = false;
        Debug.Log("분석 완료!");
    }

    /// <summary>
    /// BFS/DFS로 "Net"을 찾는 재귀 함수 (allWires 인자 추가됨)
    /// </summary>
    private void FindNetRecursive(ConnectionPoint currentPoint, HashSet<ConnectionPoint> currentNet, HashSet<ConnectionPoint> visitedPorts, Wire[] allWires)
    {
        if (currentPoint == null || visitedPorts.Contains(currentPoint)) return;

        visitedPorts.Add(currentPoint);
        currentNet.Add(currentPoint);

        ElectricalComponent currentComponent = currentPoint.parentComponent;

        // 탐색 1: 전선 따라가기
        foreach (var wire in allWires)
        {
            if (wire.connectedPoints.Contains(currentPoint))
            {
                foreach (var neighborPoint in wire.connectedPoints)
                {
                    if (neighborPoint != currentPoint)
                    {
                        FindNetRecursive(neighborPoint, currentNet, visitedPorts, allWires);
                    }
                }
            }
        }

        // 탐색 2: 닫힌 스위치 통과
        bool canPassThrough = false;
        if (currentComponent is Switch switchComp && switchComp.isOn) canPassThrough = true;
        else if (currentComponent is RelaySwitch relayComp && relayComp.isOn) canPassThrough = true;

        else if (currentComponent is EOCR) canPassThrough = true;
        else if (currentComponent is Fuse) canPassThrough = true;
        else if (currentComponent is EOCRCoil) canPassThrough = true;

        if (canPassThrough)
        {
            ConnectionPoint[] allPortsOnComponent = currentComponent.GetComponentsInChildren<ConnectionPoint>();
            foreach (var internalNeighborPort in allPortsOnComponent)
            {
                if (internalNeighborPort != currentPoint)
                {
                    FindNetRecursive(internalNeighborPort, currentNet, visitedPorts, allWires);
                }
            }
        }
    }

    private void FindLiveAndGroundNets()
    {
        foreach (var net in allNets)
        {
            bool isThisNetLive = false;
            bool isThisNetGround = false;

            foreach (var point in net)
            {
                if (isThisNetLive && isThisNetGround) break;

                var terminal = point.GetComponent<Terminal>();
                if (terminal != null)
                {
                    var parentPowerSource = terminal.GetComponentInParent<Sym_3P4W>();

                    if (!isThisNetLive && terminal.type == Terminal.TerminalType.PowerSource && (parentPowerSource == null || parentPowerSource.isOn))
                    {
                        isThisNetLive = true;
                    }
                    else if (!isThisNetGround && terminal.type == Terminal.TerminalType.PowerGround)
                    {
                        isThisNetGround = true;
                    }
                }
            }

            if (isThisNetLive) liveNet.UnionWith(net);
            if (isThisNetGround) groundNet.UnionWith(net);
        }
    }

    private void UpdateWireColors(Wire[] allWires, ElectricalComponent[] allComponents)
    {
        Color liveColor = Color.red;
        HashSet<ConnectionPoint> currentCarryingPorts = new HashSet<ConnectionPoint>();
        Queue<ConnectionPoint> queue = new Queue<ConnectionPoint>();

        // BFS 시작점 1: 켜진 부품들
        foreach (var component in allComponents)
        {
            if (component.isPowered)
            {
                foreach (var p in component.GetComponentsInChildren<ConnectionPoint>())
                {
                    if (!currentCarryingPorts.Contains(p))
                    {
                        currentCarryingPorts.Add(p);
                        queue.Enqueue(p);
                    }
                }
            }
        }

        // BFS 시작점 2: 전원 및 접지 터미널
        FindSourceAndGroundTerminals(queue, currentCarryingPorts);

        while (queue.Count > 0)
        {
            ConnectionPoint currentPoint = queue.Dequeue();

            // 전선 탐색
            foreach (var wire in allWires)
            {
                if (wire.connectedPoints.Contains(currentPoint))
                {
                    foreach (var neighbor in wire.connectedPoints)
                    {
                        if (neighbor != currentPoint && !currentCarryingPorts.Contains(neighbor))
                        {
                            currentCarryingPorts.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // 닫힌 스위치 탐색
            ElectricalComponent comp = currentPoint.parentComponent;
            bool canPassThrough = (comp is Switch s && s.isOn) ||
                                  (comp is RelaySwitch rs && rs.isOn) ||
                                  (comp is EOCR) ||
                                  (comp is EOCRCoil) ||
                                  (comp is Fuse);
            if (canPassThrough)
            {
                foreach (var internalNeighbor in comp.GetComponentsInChildren<ConnectionPoint>())
                {
                    if (internalNeighbor != currentPoint && !currentCarryingPorts.Contains(internalNeighbor))
                    {
                        currentCarryingPorts.Add(internalNeighbor);
                        queue.Enqueue(internalNeighbor);
                    }
                }
            }
        }

        foreach (var wire in allWires)
        {
            if (wire.connectedPoints.Count > 0 &&
                wire.connectedPoints.All(p => currentCarryingPorts.Contains(p)))
            {
                wire.SetColor(liveColor);
            }
            else
            {
                wire.ResetColor();
            }
        }
    }

    private void FindSourceAndGroundTerminals(Queue<ConnectionPoint> queue, HashSet<ConnectionPoint> currentCarryingPorts)
    {
        Terminal[] allTerminals = FindObjectsOfType<Terminal>();
        foreach (var terminal in allTerminals)
        {
            var parentPowerSource = terminal.GetComponentInParent<Sym_3P4W>();
            bool isSourceLive = (terminal.type == Terminal.TerminalType.PowerSource && (parentPowerSource == null || parentPowerSource.isOn));
            bool isGround = (terminal.type == Terminal.TerminalType.PowerGround);

            if (isSourceLive || isGround)
            {
                ConnectionPoint terminalPort = terminal.GetComponent<ConnectionPoint>();
                if (terminalPort != null && !currentCarryingPorts.Contains(terminalPort))
                {
                    currentCarryingPorts.Add(terminalPort);
                    queue.Enqueue(terminalPort);
                }
            }
        }
    }
}