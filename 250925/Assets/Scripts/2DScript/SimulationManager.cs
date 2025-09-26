using UnityEngine;
using UnityEngine.UI;

public class SimulationManager : MonoBehaviour
{
    // isSimulating 변수를 static으로 만들어 다른 모든 스크립트에서 쉽게 접근 가능
    public static bool isSimulating = false;

    // UI 버튼이 호출할 함수
    public static void ToggleSimulation()
    {
        isSimulating = !isSimulating;

        // 시뮬레이션이 켜질 때
        if (isSimulating)
        {
            // 이전과 동일하게 회로를 분석해서 켤 부품들을 찾습니다.
            CircuitSolver.Instance.AnalyzeCircuit();
        }
        // ✨ 시뮬레이션이 꺼질 때 (수정된 로직)
        else
        {
            // 씬에 있는 모든 전기 부품을 강제로 찾습니다.
            ElectricalComponent[] allComponents = FindObjectsOfType<ElectricalComponent>();

            // 각 부품의 PowerOff() 함수를 직접 호출하여 확실하게 끕니다.
            foreach (var component in allComponents)
            {
                component.OnSimulationStop();
            }
        }
    }
}