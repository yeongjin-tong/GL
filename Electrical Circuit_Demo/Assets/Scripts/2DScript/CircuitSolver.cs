using System.Collections.Generic;
using UnityEngine;

public class CircuitSolver : MonoBehaviour
{
    public static CircuitSolver Instance { get; private set; }

    private ElectricalComponent power;

    public List<Wire> allWires;     // WireManager에서 알아서 추가
    private List<Wire> passWire;    

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        allWires = new List<Wire>();
        passWire = new List<Wire>();
    }

    public void AnalyzeCircuit()
    {
        // 연결상태 초기화
        RebuildAllConnections();

        // 초기화
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var component in allComponents)
        {
            component.Reset();
            // ✨ 분석 시작 전, 모든 부품의 isPowered 상태도 false로 초기화
            component.isPowered = false;
        }

        // --- 신호 출발점 찾기 (통합 로직) ---

        // ✨ 1. '닫힌 회로'용 배터리(Battery)를 먼저 찾아서 처리
        Battery[] batteries = FindObjectsOfType<Battery>();
        foreach (var battery in batteries)
        {
            // 배터리가 켜져 있고, 연결선이 2개 이상일 때만 신호 출발
            if (battery.isOn && battery.connections.Count >= 2)
            {
                FloodFill(battery.connections[0].connectedComponent, true); // isLive 신호
                FloodFill(battery.connections[1].connectedComponent, false); // isGrounded 신호
            }
        }

        // ✨ 2. Sym_3W4P 부품을 찾아서 처리
        Sym_3W4P[] threePhasePowers = FindObjectsOfType<Sym_3W4P>();
        foreach (var powerComp in threePhasePowers)
        {
            // 전원이 꺼져있으면 무시
            if (!powerComp.isOn) continue;

            // 이 전원에 연결된 모든 전선을 확인
            foreach (var connection in powerComp.connections)
            {
                // 전원에 연결된 상대방 부품
                ElectricalComponent connectedPartner = connection.connectedComponent;
                if (connectedPartner == null) continue;

                // 나의 어떤 단자에 연결되었는지 확인 (myPortDirection, myPortIndex)
                // (이 예시에서는 Terminal의 Type으로 확인)

                // 나의 포트 오브젝트를 찾아 Terminal 스크립트를 가져옴
                // (이 부분을 구현하려면 ConnectionPoint에 대한 참조가 필요하거나,
                //  ElectricalComponent가 자신의 포트 목록을 가지고 있어야 합니다.)

                // ✨ --- 더 간단하고 직접적인 방법 --- ✨
                // 모든 '전원' 및 '접지' 단자를 직접 찾아서,
                // 그 단자에 연결된 부품에서부터 신호를 출발시킵니다.
            }
        }

        Terminal[] allTerminals = FindObjectsOfType<Terminal>();

        foreach (var terminal in allTerminals)
        {
            // 이 터미널을 포함하는 부모 부품(예: ThreePhasePower)이 켜져있는지 확인
            var parentPowerSource = terminal.GetComponentInParent<Sym_3W4P>();
            if (parentPowerSource != null && !parentPowerSource.isOn) continue;

            // 이 터미널에 연결된 전선이 있는지 확인
            if (terminal.parentComponent != null && terminal.parentComponent.connections.Count > 0)
            {
                // 이 터미널의 포트 정보와 일치하는 연결을 찾음
                foreach (var conn in terminal.parentComponent.connections)
                {
                    // (ConnectionPoint에 portID가 있다는 가정 하에, conn.myPortID == terminal.portID)
                    // 여기서는 Terminal 자체에서 연결된 부품을 찾아 신호를 보냅니다.
                    if (conn.myPortIndex == terminal.GetComponent<ConnectionPoint>().portIndex) // 포트 인덱스로 구분
                    {
                        if (terminal.type == Terminal.TerminalType.PowerSource) // 'R' 단자라면
                        {
                            terminal.parentComponent.isLive = true;
                            FloodFill(conn.connectedComponent, true); // isLive 신호 출발

                        }
                        else if (terminal.type == Terminal.TerminalType.PowerGround) // 'T' 단자라면
                        {
                            FloodFill(conn.connectedComponent, false); // isGrounded 신호 출발

                        }
                    }
                }
            }
        }

        // --- 최종 전원 결정 로직 (변경 없음) ---
        foreach (var component in allComponents)
        {
            if (component.name.Contains("Clone"))
            {
                // ✨ 1. 먼저, isLive와 isGrounded 신호가 모두 도달했는지 확인하여 전원 공급 여부를 결정
                if (component.isLive && component.isGrounded)
                {
                    component.PowerOn(); // isPowered를 true로 설정하고 시각적 효과 표시
                }
                else
                {
                    component.PowerOff(); // isPowered를 false로 설정하고 시각적 효과 끄기
                }
            }
        }

        UpdateWireColors();
    }

    void FloodFill(ElectricalComponent startComponent, bool isLiveSignal)
    {
        if (startComponent == null) return;

        Queue<ElectricalComponent> queue = new Queue<ElectricalComponent>();
        queue.Enqueue(startComponent);

        HashSet<ElectricalComponent> visited = new HashSet<ElectricalComponent> { startComponent };

        while (queue.Count > 0)
        {
            ElectricalComponent current = queue.Dequeue();

            // ✨ 2. 현재 부품이 '꺼진 스위치'인지 확인
            Switch switchComp = current as Switch;
            if (switchComp != null && !switchComp.isOn)
            {
                // 스위치가 꺼져있으면, 이 너머로는 신호를 전달하지 않고 건너뜀
                continue;
            }

            // ✨ 1. 현재 부품에 신호가 도달했다고 먼저 표시
            if (isLiveSignal)
            {
                current.isLive = true;
            }
            else
            {
                current.isGrounded = true;
            }

            // 3. 이웃 부품들에게 신호 전파
            foreach (var connection in current.connections)
            {
                ElectricalComponent neighbour = connection.connectedComponent;
                if (neighbour != null && !visited.Contains(neighbour))
                {
                    visited.Add(neighbour);
                    queue.Enqueue(neighbour);
                }
            }
        }
    }

    public void RebuildAllConnections()
    {
        // 1. 모든 부품의 기존 연결 정보를 초기화합니다.
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var comp in allComponents)
        {
            comp.connections.Clear();
        }

        // missing된 선 정리
        allWires.RemoveAll(x => x == null);

        // 3. 각 전선의 시작점과 끝점을 찾아, 연결된 두 부품에 AddConnection을 호출합니다.
        foreach (var wire in allWires)
        {
            ElectricalComponent comp1 = wire.firstPoint.parentComponent;
            ElectricalComponent comp2 = wire.lastPoint.parentComponent;

            if (comp1 != null && comp2 != null)
            {
                comp1.AddConnection(wire.firstPoint, wire.lastPoint);
                comp2.AddConnection(wire.lastPoint, wire.firstPoint);
            }
        }
    }

    // 전선 색상을 업데이트하는 메인 함수
    private void UpdateWireColors()
    {
        Color liveColor = Color.red; // 전류가 흐를 때의 색상
        passWire.Clear();


        // 1. 모든 전선을 기본 색상으로 초기화
        foreach (var wire in allWires)
        {
            wire.ResetColor();
        }

        // 2. 전류가 흐르기 시작하는 모든 지점(전원 R)을 찾음
        Queue<ElectricalComponent> queue = new Queue<ElectricalComponent>();
        HashSet<ElectricalComponent> visited = new HashSet<ElectricalComponent>();

        Terminal[] allTerminals = FindObjectsOfType<Terminal>();
        foreach (var terminal in allTerminals)
        {
            // 터미널이 '전원(R)'이고, 그 부품이 켜져 있으며, 실제 전력이 흐르는 상태일 때
            if (terminal.type == Terminal.TerminalType.PowerSource && terminal.parentComponent.isLive)
            {
                // 이 터미널에서 시작되는 모든 연결을 큐에 추가
                foreach (var connection in terminal.parentComponent.connections)
                {
                    if (connection.myPortIndex == terminal.GetComponent<ConnectionPoint>().portIndex)
                    {
                        var neighbour = connection.connectedComponent;
                        power = terminal.parentComponent;
                        if (neighbour != null && !visited.Contains(neighbour))
                        {
                            //Wire wireToColor = FindWireConnecting(terminal.parentComponent, terminal.GetComponent<ConnectionPoint>());
                            //if (wireToColor != null) wireToColor.SetColor(liveColor);

                            List<Wire> w = FindWireExample(terminal.parentComponent, terminal.GetComponent<ConnectionPoint>());
                            if (w != null)
                            {
                                foreach(var wire in w)
                                {
                                    wire.SetColor(liveColor);
                                }
                            }
                            visited.Add(neighbour);
                            queue.Enqueue(neighbour);

                        }
                    }
                }
            }
        }

        // (배터리 모델도 사용한다면 여기에 배터리 시작점 찾는 로직 추가)



        // 3. 큐를 따라가며 단방향으로 전선 색칠
        while (queue.Count > 0)
        {
            ElectricalComponent current = queue.Dequeue();

            // 현재 부품이 꺼진 스위치라면, 더 이상 진행하지 않음
            Switch switchComp = current as Switch;
            if (switchComp != null && !switchComp.isOn)
            {
                continue;
            }

            // 현재 부품에서 나가는 모든 연결을 확인
            foreach (var connection in current.connections)
            {
                ElectricalComponent neighbour = connection.connectedComponent;

                // 현재 부품의 isLive가 true이고, 아직 방문하지 않았다면
                if (neighbour != null && current.isLive && !visited.Contains(neighbour))
                {
                    //wire wiretocolor = findwireconnecting(current, neighbour);
                    //if (wiretocolor != null)
                    //{
                    //    wiretocolor.setcolor(livecolor);
                    //    debug.log($"wire2: {wiretocolor.name}");
                    //}

                    List<Wire> w = FindWireExample(current);
                    if (w != null)
                    {
                        foreach (var wire in w)
                        {
                            wire.SetColor(liveColor);
                        }
                    }

                    visited.Add(neighbour);
                    queue.Enqueue(neighbour);
                }
                else if (visited.Contains(neighbour) && neighbour == power)
                {
                    //Wire wireToColor = FindWireConnecting(current, neighbour);
                    //if (wireToColor != null)
                    //{
                    //    wireToColor.SetColor(liveColor);
                    //    Debug.Log($"Wire3: {wireToColor.name}");
                    //}
                    List<Wire> w = FindWireExample(current);
                    if (w != null)
                    {
                        foreach (var wire in w)
                        {
                            wire.SetColor(liveColor);
                        }
                    }

                }
            }
        }
    }

    // 두 부품을 연결하는 Wire를 찾는 헬퍼 함수
    private Wire FindWireConnecting(ElectricalComponent symbol, ConnectionPoint symbolPoint)
    {
        List<Wire> outWire = new List<Wire>();

        foreach (var wire in allWires)
        {
            if ((wire.componentA == symbol && wire.firstPoint == symbolPoint) ||
                (wire.componentB == symbol && wire.lastPoint == symbolPoint))
            {
                return wire;
            }
        }
        return null;
    }

    private List<Wire> FindWireExample(ElectricalComponent symbol, ConnectionPoint symbolPoint)     // 패스한 wire가 없을 때 (시작점)
    {
        if (passWire.Count == 0)
        {
            foreach (var wire in allWires)
            {
                if ((wire.componentA == symbol && wire.firstPoint == symbolPoint) || (wire.componentB == symbol && wire.lastPoint == symbolPoint))
                {
                    passWire.Add(wire);
                }
            }
        }
        return passWire;
    }

    private List<Wire> FindWireExample(ElectricalComponent symbol)      // 패스 o
    {
        List<Wire> outWire = new List<Wire>();
        ConnectionPoint passPoint = null;

        // 패스한 포트 찾기
        if (passWire.Count != 0)
        {
            foreach(var pass in passWire)
            {
                if(pass.componentA == symbol)
                {
                    passPoint = pass.firstPoint;
                }

                if(pass.componentB == symbol)
                {
                    passPoint = pass.lastPoint;
                }
            }
        }

        // 패스 포트가 아닌 wire 찾아서 return
        if (passPoint != null)
        {
            foreach (var wire in allWires)
            {
                if ((wire.componentA == symbol && wire.firstPoint != passPoint) || (wire.componentB == symbol && wire.lastPoint != passPoint))
                {
                    outWire.Add(wire);
                    passWire.Add(wire);
                }
            }
        }


        return outWire;
    }
}