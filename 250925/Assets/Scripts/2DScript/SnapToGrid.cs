using UnityEngine;

// 이 스크립트가 붙은 오브젝트는 반드시 Collider2D가 있어야 마우스 입력을 받을 수 있습니다.
[RequireComponent(typeof(Collider2D))]
public class SnapToGrid : MonoBehaviour
{
    private Vector3 offset;

    void OnMouseDown()
    {
        // 클릭 시 마우스 위치와 오브젝트 위치의 차이를 계산 (오프셋)
        offset = transform.position - GetMouseWorldPosition();
    }

    void OnMouseDrag()
    {
        // 1. 현재 마우스 위치를 가져옴
        Vector3 mousePos = GetMouseWorldPosition() + offset;

        // 2. 마우스 위치를 그리드 좌표로 변환
        Vector2Int gridPos = GridManager.Instance.GetGridPosition(mousePos);

        // 3. 그리드 좌표를 다시 월드 좌표(칸의 중앙)로 변환하여 오브젝트 위치 설정
        transform.position = GridManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);
    }

    // 마우스 위치를 2D 월드 좌표로 변환하는 헬퍼 함수
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}