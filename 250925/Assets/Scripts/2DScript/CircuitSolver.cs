using UnityEngine;
using System.Collections.Generic;

public class CircuitSolver : MonoBehaviour
{
    public static CircuitSolver Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    // 회로 분석을 시작하는 메인 함수
    public void AnalyzeCircuit()
    {
        ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();
        foreach (var component in allComponents) component.Reset();

        // 1. 씬에 있는 모든 배터리를 찾습니다.
        Battery[] batteries = FindObjectsOfType<Battery>();
        foreach (var battery in batteries)
        {
            // ✨ 2. 배터리가 꺼져있으면(isOn == false), 이 배터리는 무시하고 다음 배터리로 넘어갑니다.
            if (!battery.isOn) continue;

            // 3. 배터리가 켜져 있을 때만, 양쪽 포트에서 신호를 보냅니다.
            if (battery.connectedComponents.Count >= 2)
            {
                FloodFill(battery.connectedComponents[0], true); // isLive 신호
                FloodFill(battery.connectedComponents[1], false); // isGrounded 신호
            }
        }

        // (분석 결과를 바탕으로 최종 전원을 켜고 끄는 나머지 로직은 변경할 필요 없음)
        foreach (var component in allComponents)
        {
            if (component.isLive && component.isGrounded)
            {
                Switch switchComp = component as Switch;
                if (switchComp != null && !switchComp.isOn) component.PowerOff();
                else component.PowerOn();
            }
            else
            {
                component.PowerOff();
            }
        }
    }

    // 특정 지점부터 신호를 전파시키는 함수 (Flood Fill)
    void FloodFill(ElectricalComponent startComponent, bool isPositiveSignal)
    {
        // 이미 해당 신호를 받은 컴포넌트는 다시 탐색할 필요 없음
        if (isPositiveSignal && startComponent.isLive) return;
        if (!isPositiveSignal && startComponent.isGrounded) return;

        Queue<ElectricalComponent> queue = new Queue<ElectricalComponent>();
        queue.Enqueue(startComponent);

        while (queue.Count > 0)
        {
            ElectricalComponent current = queue.Dequeue();

            // 현재 부품에 신호가 도달했다고 표시
            if (isPositiveSignal) current.isLive = true;
            else current.isGrounded = true;

            // 스위치가 꺼져있으면, 그 너머로는 신호를 전달하지 않음
            Switch switchComp = current as Switch;
            if (switchComp != null && !switchComp.isOn)
            {
                continue; // 다음 탐색 중단
            }

            // 연결된 모든 이웃들에게 신호를 전파
            foreach (var neighbour in current.connectedComponents)
            {
                bool alreadyReached = isPositiveSignal ? neighbour.isLive : neighbour.isGrounded;
                if (!alreadyReached)
                {
                    queue.Enqueue(neighbour);
                }
            }
        }
    }
}