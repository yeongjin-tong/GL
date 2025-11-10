using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimulationManager : MonoBehaviour
{
    // isSimulating 변수를 static으로 만들어 다른 모든 스크립트에서 쉽게 접근 가능
    public static bool isSimulating = false;

    // UI 버튼이 호출할 함수
    public static void ToggleSimulation(bool isPlay)
    {

        isSimulating = isPlay;
        
        // 시뮬레이션이 켜질 때
        if (isSimulating)
        {
            ObjectManager.Instance.CleanUpList();

            foreach(GameObject obj in ObjectManager.Instance.objects_2d)
            {
                if(obj.GetComponent<ElectricalComponent>() != null)
                {
                    ElectricalComponent component = obj.GetComponent<ElectricalComponent>();
                    NameSetting(component);
                    component.OnSimulationStart();
                    SymbolController.Instance.DeselectAll();
                }
            }

            // 이전과 동일하게 회로를 분석해서 켤 부품들을 찾습니다.
            CircuitSolver.Instance.AnalyzeCircuit();
        }
        // 시뮬레이션이 꺼질 때 (수정된 로직)
        else
        {
            foreach (GameObject component in ObjectManager.Instance.objects_2d)
            {
                if (component.GetComponent<ElectricalComponent>() != null)
                {
                    component.GetComponent<ElectricalComponent>().OnSimulationStop();
                }
            }

            // 모든 전선의 색상을 리셋하도록 추가
            Wire[] allWires = FindObjectsOfType<Wire>();
            foreach (var wire in allWires)
            {
                wire.ResetColor();
            }
        }
    }

    private static void NameSetting(ElectricalComponent component)
    {
        foreach (TextMeshProUGUI tmp in component.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (tmp.name == "name_text")
            {
                component.symbol_ID = tmp.text;
            }
        }
    }
}