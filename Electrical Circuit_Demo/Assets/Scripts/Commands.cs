using System.Windows.Input;
using Unity.VisualScripting;
using UnityEngine;

public class Command_Move : ICommand
{
    private Transform target;
    private Vector3 oldPos;
    private Vector3 newPos;
    private ElectricalComponent component;

    public Command_Move(Transform target, Vector3 oldPos, Vector3 newPos, ElectricalComponent component)
    {
        this.target = target;
        this.oldPos = oldPos;
        this.newPos = newPos;
        this.component = component;
    }
    public void Execute()
    {
        // 이동 다시 실행 (Redo)
        target.position = newPos;
        if(component != null) WireManager.Instance.RedrawWiresForComponent(component);
    }

    public void Undo()
    {
        target.position = oldPos;
        if (component != null) WireManager.Instance.RedrawWiresForComponent(component);
    }
}

public class Command_ToggleActive : ICommand
{
    private GameObject targetObj;
    private bool isCreateAction;    // true면 '생성' 행동, false면 '삭제' 행동
    private ElectricalComponent component;

    public Command_ToggleActive(GameObject obj, bool isCreate)
    {
        this.targetObj = obj;
        this.isCreateAction = isCreate;
        this.component = obj.GetComponent<ElectricalComponent>();
    }

    public void Execute()
    {
        // 생성 행동의 실행 = 켜기 / 삭제 행동의 실행 = 끄기
        bool state = isCreateAction;
        SetState(state);
    }

    public void Undo()
    {
        // 생성 행동의 취소 = 끄기 / 삭제 행동의 취소 = 켜기
        bool state = !isCreateAction;
        SetState(state);
    }

    private void SetState(bool active)
    {
        if (targetObj == null) return;
        targetObj.SetActive(active);

        // 회로 그래프 등록/해제 처리
        if (component != null)
        {
            if (active)
            {
                CircuitGraph.Instance.RebuildGraph();
            }
            else
            {
                CircuitGraph.Instance.RemoveComponent(component);
            }
        }
        else if (targetObj.GetComponent<Wire>() != null)
        {
            // 와이어인 경우
            if (active) CircuitGraph.Instance.RebuildGraph();
            else CircuitGraph.Instance.RebuildGraph();
        }
    }
}
