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
                FindNetRecursive(point, newNet, visitedPorts);

                if (newNet.Count > 0)
                {
                    allNets.Add(newNet);
                }
            }
        }

        // --- 2단계: Live Net과 Ground Net 식별 ---
        FindLiveAndGroundNets();

        // --- 3단계: 부품 전원 인가 ---
        var poweredPorts = new HashSet<ConnectionPoint>(); // (와이어 색칠용)

        foreach (var component in allComponents)
        {
            // (스위치, 전원 등은 전력 소모 부품이 아니므로 제외)
            if (component is Switch || component is RelaySwitch || component is Sym_3P4W)
            {
                continue;
            }

            // (RL, Timer, RelayCoil 등 "부하" 부품만 검사)
            ConnectionPoint[] ports = component.GetComponentsInChildren<ConnectionPoint>();
            if (ports.Length < 2) continue; // 포트가 2개 미만인 부하는 무시

            // 부하의 양단 포트
            ConnectionPoint portA = ports[0];
            ConnectionPoint portB = ports[1];

            // A가 Live이고 B가 Ground인지 확인
            bool isPoweredAtoB = liveNet.Contains(portA) && groundNet.Contains(portB);
            // B가 Live이고 A가 Ground인지 확인 (교류 등)
            bool isPoweredBtoA = liveNet.Contains(portB) && groundNet.Contains(portA);

            if (isPoweredAtoB || isPoweredBtoA)
            {
                // 전원 인가 조건 충족!
                component.isLive = true;
                component.isGrounded = true;
                component.isPowered = true;
                component.PowerOn();

                // 와이어 색칠을 위해 이 부품의 포트들을 poweredPorts에 추가
                poweredPorts.Add(portA);
                poweredPorts.Add(portB);

                //poweredPorts.UnionWith(liveNet);
                //poweredPorts.UnionWith(groundNet);
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
    /// ✨ [새 함수] BFS/DFS로 "Net"을 찾는 재귀 함수
    /// </summary>
    private void FindNetRecursive(ConnectionPoint currentPoint, HashSet<ConnectionPoint> currentNet, HashSet<ConnectionPoint> visitedPorts)
    {
        // 1. 이미 방문했거나 유효하지 않으면 중단
        if (currentPoint == null || visitedPorts.Contains(currentPoint))
            return;

        // 2. 방문 기록 및 Net에 추가
        visitedPorts.Add(currentPoint);
        currentNet.Add(currentPoint);

        ElectricalComponent currentComponent = currentPoint.parentComponent;

        // --- 탐색 1: 병렬 탐색 (전선을 따라가기) ---
        // (FindObjectsOfType은 매우 느리므로, WireManager가 allWires를 캐시하는 것이 좋음)
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            if (wire.connectedPoints.Contains(currentPoint))
            {
                foreach (var neighborPoint in wire.connectedPoints)
                {
                    if (neighborPoint != currentPoint)
                    {
                        FindNetRecursive(neighborPoint, currentNet, visitedPorts); // 재귀 호출
                    }
                }
            }
        }

        // --- 탐색 2: 직렬 탐색 ("닫힌 스위치" 통과하기) ---
        bool canPassThrough = false;

        if (currentComponent is Switch switchComp && switchComp.isOn) canPassThrough = true;
        else if (currentComponent is RelaySwitch relayComp && relayComp.isOn) canPassThrough = true;

        if (canPassThrough)
        {
            // 닫힌 스위치라면, 반대편 포트로 탐색 계속
            ConnectionPoint[] allPortsOnComponent = currentComponent.GetComponentsInChildren<ConnectionPoint>();
            foreach (var internalNeighborPort in allPortsOnComponent)
            {
                if (internalNeighborPort != currentPoint)
                {
                    FindNetRecursive(internalNeighborPort, currentNet, visitedPorts); // 재귀 호출
                }
            }
        }
        // (부품이 RL, Coil 등 "부하"이거나 "열린 스위치"면 여기서 탐색 중단)
    }

    /// <summary>
    /// ✨ [새 함수] 빌드된 Net 리스트를 순회하며 Live/Ground Net을 식별합니다.
    /// </summary>
    private void FindLiveAndGroundNets()
    {
        foreach (var net in allNets)
        {
            foreach (var point in net)
            {
                var terminal = point.GetComponent<Terminal>();
                if (terminal != null)
                {
                    var parentPowerSource = terminal.GetComponentInParent<Sym_3P4W>();

                    // (Live) 전원 소스 터미널을 포함하는 Net
                    if (terminal.type == Terminal.TerminalType.PowerSource && (parentPowerSource == null || parentPowerSource.isOn))
                    {
                        liveNet.UnionWith(net); // 이 Net 전체를 Live로 간주
                        break; // 다음 Net 검사
                    }
                    // (Ground) 접지 터미널을 포함하는 Net
                    else if (terminal.type == Terminal.TerminalType.PowerGround)
                    {
                        groundNet.UnionWith(net); // 이 Net 전체를 Ground로 간주
                        break; // 다음 Net 검사
                    }
                }
            }
        }
    }

    /// <summary>
    /// ✨ [수정] "전류가 흐르는" 경로만 빨간색으로 칠합니다.
    /// </summary>
    private void UpdateWireColors(Wire[] allWires, ElectricalComponent[] allComponents)
    {
        Color liveColor = Color.red;

        // 1. "전류가 흐르는" 포트들만 담을 새 Set을 생성합니다.
        HashSet<ConnectionPoint> currentCarryingPorts = new HashSet<ConnectionPoint>();

        // 2. BFS(너비 우선 탐색) 큐를 준비합니다.
        Queue<ConnectionPoint> queue = new Queue<ConnectionPoint>();

        // 3. [시작점 1] 전원이 켜진(isPowered) 모든 부품의 포트를 큐에 추가
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

        // 4. [시작점 2] Live 전원(R,S,T)과 Ground(N) 터미널 포트도 큐에 추가
        FindSourceAndGroundTerminals(queue, currentCarryingPorts);

        // 5. BFS 탐색 시작 (Dijkstra와 유사하게 "전류가 흐르는" Net을 탐색)
        while (queue.Count > 0)
        {
            ConnectionPoint currentPoint = queue.Dequeue();

            // 6. 탐색 1: (전선) 이 포트에 연결된 모든 전선을 탐색
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

            // 7. 탐색 2: (닫힌 스위치) 이 포트가 속한 부품이 "닫힌 스위치"인지 확인
            ElectricalComponent comp = currentPoint.parentComponent;
            bool canPassThrough = (comp is Switch s && s.isOn) ||
                                  (comp is RelaySwitch rs && rs.isOn);

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
        } // BFS 종료

        // 8. 최종 색칠
        foreach (var wire in allWires)
        {
            // 와이어의 양 끝점이 *모두* "전류가 흐르는" Set에 포함될 때만
            if (wire.connectedPoints.Count > 0 &&
                wire.connectedPoints.All(p => currentCarryingPorts.Contains(p)))
            {
                wire.SetColor(liveColor);
            }
            else
            {
                wire.ResetColor(); // PB1으로 가는 선 등 "Dead" 선은 회색
            }
        }
    }

    /// <summary>
    /// UpdateWireColors의 BFS 시작점으로 사용될 Live/Ground 터미널 포트를 찾습니다.
    /// </summary>
    private void FindSourceAndGroundTerminals(Queue<ConnectionPoint> queue, HashSet<ConnectionPoint> currentCarryingPorts)
    {
        Terminal[] allTerminals = FindObjectsOfType<Terminal>();
        foreach (var terminal in allTerminals)
        {
            var parentPowerSource = terminal.GetComponentInParent<Sym_3P4W>();
            bool isSourceLive = (terminal.type == Terminal.TerminalType.PowerSource && (parentPowerSource == null || parentPowerSource.isOn));
            bool isGround = (terminal.type == Terminal.TerminalType.PowerGround);

            if (isSourceLive)
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