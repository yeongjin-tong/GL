using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;

public class AssemblyManager : MonoBehaviour
{
    public static AssemblyManager instance;

    [Header("시나리오 설정")]
    public List<MainStep> scenarioList; // 인스펙터에서 여기에 단계를 할당해 줌!

    [Header("현재 진행 파라미터")]
    public int currentMainIndex = 0; // 현재 대단계 (0부터 시작)
    public int currentSubIndex = 0;  // 현재 소단계

    // [추가] ID로 부품을 빠르게 찾기 위한 자료구조(Dictionary)
    public Dictionary<string, PartController> partRegistry = new Dictionary<string, PartController>();

    public event Action OnStepChanged;

    NoticeUI notice;

    void Awake()
    {
        instance = this;
        notice = FindObjectOfType<NoticeUI>();
    }

    // [추가] 부품들이 시작할 때 호출하여 자신을 등록하는 함수
    public void RegisterPart(PartController part)
    {
        if (!partRegistry.ContainsKey(part.myPartID))
        {
            partRegistry.Add(part.myPartID, part);
            // Debug.Log($"부품 등록됨: {part.myPartID}");
        }

        // 시나리오의 SubStep의 마지막 부품 표시
        for(int i = 0; i < scenarioList.Count; i++)
        {
            if (scenarioList[i].subSteps.Last().targetPartID == part.myPartID)
            {
                part.isLastPort = true;
            }
        }

        UpdateCurrentStepHighlight();
    }

    SubStep GetCurrentStep()
    {
        if (currentMainIndex >= scenarioList.Count) return null;
        var mainStep = scenarioList[currentMainIndex];
        if (currentSubIndex >= mainStep.subSteps.Count) return null;
        return mainStep.subSteps[currentSubIndex];
    }

    public void UpdateCurrentStepHighlight()
    {
        SubStep current = GetCurrentStep();

        // 등록된 모든 부품을 깜빡임을 끔
        foreach(var part in partRegistry.Values)
        {
            part.StopBlinking();
        }

        if (current == null) return;

        // 현재 대상인 부품을 찾아서 깜빡이게 하기
        if(partRegistry.ContainsKey(current.targetPartID))
        {
            partRegistry[current.targetPartID].StartBlinking();
        }
    }

    public void OnPartClick(PartController part)
    {
        SubStep current = GetCurrentStep();
        if (current == null) return;

        // [수정] 참조 같음(==) 대신 ID(문자열) 비교
        if (part.myPartID == current.targetPartID)
        {
            // 해당 부품 실행
            part.ExecutePartAction();
            Debug.Log($"정답! {current.stepName} 완료");

            // (선택사항) 런타임 참조 저장해둠
            current.runtimePartReference = part;

            currentSubIndex++; // 소단계 +1

            UpdateCurrentStepHighlight();
        }
        else
        {
            //notice.SUB("해당 부품이 아닙니다.");
            Debug.Log($"틀림! 목표: {current.targetPartID}, 클릭됨: {part.myPartID}");
        }
    }

    public void NextStep(bool isNext)
    {
        if(isNext)
        {
            currentMainIndex++; // 다음 대단계로 이동
            Debug.Log(">>> 대단계 완료! 다음 단계로 넘어갑니다.");
        }
        else
        {
            currentMainIndex--; // 이전 대단계로 이동
            Debug.Log(">>> 이전 단계로 되돌아갑니다.");
        }
        
        currentSubIndex = 0; // 소단계 초기화

        FinishComponent();

        // 시나리오의 SubStep 전부 CleanUp 
        for (int i = 0; i < scenarioList.Count; i++)
        {
            foreach(var element in scenarioList[i].subSteps)
            {
                if(element.runtimePartReference != null)
                {
                    element.runtimePartReference.CleanUpPart();
                }
            }
        }

        if(OnStepChanged != null) OnStepChanged.Invoke();

        UpdateCurrentStepHighlight();
    }

    public void InitAllParts()
    {
        foreach(PartController part in partRegistry.Values)
        {
            part.InitPart();
        }

        currentMainIndex = 0;
        currentSubIndex = 0;

        if (OnStepChanged != null) OnStepChanged.Invoke();

        UpdateCurrentStepHighlight();
    }

    public void FinishComponent()
    {
        foreach (PartController part in partRegistry.Values)
        {
            part.InitPart();
        }

        for (int i = 0; i < scenarioList.Count; i++)
        {
            foreach(var step in scenarioList[i].subSteps)
            {
                step.runtimePartReference = null;
            }
        }

        for(int i = 0; i < currentMainIndex; i++)
        {
            foreach(var step in scenarioList[i].subSteps)
            {
                if(partRegistry.ContainsKey(step.targetPartID))
                {
                    if(step.runtimePartReference == null)
                    {
                        step.runtimePartReference = partRegistry[step.targetPartID];
                    }
                }
            }
        }
    }
}
