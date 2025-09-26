using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WireManager : MonoBehaviour
{
    [Tooltip("Wire 생성 위치")]
    public Transform content_2D;

    [Tooltip("포트에서 직선으로 뻗어 나오는 선의 길이입니다.")]
    public float clearance = 0.3f;

    private ConnectionPoint firstPoint;
    private LineRenderer currentWire;

    // 스크립트 활성화 시 이벤트 구독
    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPhysicsObjectClicked += HandlePhysicsClick;
    }

    // 스크립트 비활성화 시 이벤트 구독 해제 (메모리 누수 방지)
    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPhysicsObjectClicked -= HandlePhysicsClick;
    }

    void Update()
    {
        if (currentWire != null)
        {
            UpdateWirePreview(currentWire, firstPoint, GetMouseWorldPosition());
        }
    }

    // InputManager의 방송을 수신하여 처리하는 함수
    private void HandlePhysicsClick(Collider2D hit)
    {
        if (hit == null && firstPoint != null) { Destroy(currentWire.gameObject); ResetState(); return; }

        if (hit != null)
        {
            ConnectionPoint point = hit.GetComponent<ConnectionPoint>();
            if (point != null)
            {
                if (firstPoint == null) { firstPoint = point; currentWire = CreateWire(); }
                else if (point != firstPoint)
                {
                    if (point.transform.parent == firstPoint.transform.parent) { Destroy(currentWire.gameObject); ResetState(); return; }
                    FinalizeWire(currentWire, firstPoint, point);
                    ResetState();
                }
            }
        }
    }

    private void FinalizeWire(LineRenderer wire, ConnectionPoint startPoint, ConnectionPoint endPoint)
    {
        List<Vector3> finalPath = GenerateOptimalPath(startPoint, endPoint);
        wire.positionCount = finalPath.Count;
        wire.SetPositions(finalPath.ToArray());
        wire.name = $"Wire_{startPoint.transform.parent.name}_to_{endPoint.transform.parent.name}";

        // ✨ 생성된 전선 오브젝트에 "Wire" 태그를 설정합니다.
        wire.gameObject.tag = "Wire";

        ElectricalComponent comp1 = startPoint.GetComponentInParent<ElectricalComponent>();
        ElectricalComponent comp2 = endPoint.GetComponentInParent<ElectricalComponent>();

        if (comp1 != null && comp2 != null)
        {
            //서로를 다음 부품으로 등록 (양방향 연결)
            comp1.AddConnection(comp2);
            comp2.AddConnection(comp1);
        }

        // 1. LineRenderer와 같은 게임 오브젝트에 있는 EdgeCollider2D를 가져옵니다.
        EdgeCollider2D edgeCollider = wire.GetComponent<EdgeCollider2D>();

        // 2. LineRenderer의 Vector3 좌표를 EdgeCollider2D가 사용하는 Vector2 좌표로 변환합니다.
        Vector2[] colliderPoints = new Vector2[wire.positionCount];
        for (int i = 0; i < wire.positionCount; i++)
        {
            // Z축 정보는 무시하고 X, Y 좌표만 사용합니다.
            colliderPoints[i] = new Vector2(finalPath[i].x, finalPath[i].y);
        }

        // 3. 변환된 좌표를 EdgeCollider2D의 points 속성에 설정합니다.
        edgeCollider.points = colliderPoints;
    }

    private void UpdateWirePreview(LineRenderer wire, ConnectionPoint startPoint, Vector3 mousePos)
    {
        List<Vector3> previewPath;

        // 마우스 위치에 연결 가능한 포트가 있는지 확인
        Collider2D hoveredCollider = Physics2D.OverlapPoint(mousePos);
        ConnectionPoint hoveredPoint = hoveredCollider?.GetComponent<ConnectionPoint>();

        // 마우스가 유효한 '다른' 포트 위에 있다면
        if (hoveredPoint != null && hoveredPoint != firstPoint && hoveredPoint.transform.parent != startPoint.transform.parent)
        {
            //  해당 포트와 연결될 때의 '완벽한 최종 경로'를 미리 보여줌
            previewPath = GenerateOptimalPath(startPoint, hoveredPoint);
        }
        else // 마우스가 허공에 있다면
        {
            //  마우스 위치를 기준으로 '가장 가능성 높은 최종 경로'를 예측해서 보여줌
            // 이 로직은 GenerateOptimalPath를 마우스 위치에 맞게 간소화한 버전입니다.

            // 1. 마우스 위치가 시작점과 거의 수직일 때 (2점 경로 예측)
            if (Mathf.Abs(startPoint.transform.position.x - mousePos.x) < 0.5f) // 0.5f는 약간의 오차 허용
            {
                previewPath = new List<Vector3> { startPoint.transform.position, mousePos };
            }
            // 2. 시작 포트가 위/아래 방향일 때 ('ㄷ'자 또는 'L'자 경로 예측)
            else if (startPoint.pointDirection == ConnectionPoint.Direction.Up || startPoint.pointDirection == ConnectionPoint.Direction.Down)
            {
                // 시작점이 마우스보다 위에 있고, 시작 포트가 'Up'일 경우 'ㄷ'자 경로 예측
                if (startPoint.transform.position.y > mousePos.y && startPoint.pointDirection == ConnectionPoint.Direction.Up)
                {
                    previewPath = Calculate_C_Path_ForPreview(startPoint, mousePos);
                }
                // 그 외에는 일반 'L'자 경로 예측
                else
                {
                    previewPath = Calculate_L_Path(startPoint, mousePos);
                }
            }
            // 3. 시작 포트가 좌/우 방향일 때
            else
            {
                // 여기에도 위와 같이 더 정교한 규칙을 추가할 수 있으나, 우선 일반 경로로 예측
                previewPath = Calculate_L_Path(startPoint, mousePos);
            }
        }

        wire.positionCount = previewPath.Count;
        wire.SetPositions(previewPath.ToArray());
    }

    // 미리보기를 위한 'ㄷ'자 경로 계산 함수
    private List<Vector3> Calculate_C_Path_ForPreview(ConnectionPoint startPoint, Vector3 endPos)
    {
        Vector3 startPos = PosToV3(startPoint.transform.position);
        Vector3 startStub = GetStubPoint(startPoint, startPos);

        Vector3 midPoint1, midPoint2;

        // 시작점이 끝점보다 왼쪽에 있으면 오른쪽으로, 오른쪽에 있으면 왼쪽으로 우회
        float xOffsetDirection = (startPos.x < endPos.x) ? 1f : -1f;
        float boundaryX = startPoint.parentCollider.bounds.center.x + (startPoint.parentCollider.bounds.extents.x * xOffsetDirection);
        float detourX = boundaryX + (clearance * xOffsetDirection);

        midPoint1 = new Vector3(detourX, startStub.y, 0);
        midPoint2 = new Vector3(detourX, endPos.y, 0);

        return new List<Vector3> { startPos, startStub, midPoint1, midPoint2, endPos };
    }

    // 사용자의 모든 규칙을 적용한 최종 경로 결정 함수
    private List<Vector3> GenerateOptimalPath(ConnectionPoint startPoint, ConnectionPoint endPoint)
    {
        Vector3 startPos = PosToV3(startPoint.transform.position);
        Vector3 endPos = PosToV3(endPoint.transform.position);
        var startDir = startPoint.pointDirection;
        var endDir = endPoint.pointDirection;
        const float alignmentThreshold = 0.01f;

        bool isAligned = Mathf.Abs(startPos.x - endPos.x) < alignmentThreshold;
        bool isD1Above = startPos.y > endPos.y;

        // --- 규칙 1: 2점 직선 연결 (가장 높은 우선순위) ---
        if (isAligned)
        {
            if ((isD1Above && startDir == ConnectionPoint.Direction.Down && endDir == ConnectionPoint.Direction.Up) ||
                (!isD1Above && startDir == ConnectionPoint.Direction.Up && endDir == ConnectionPoint.Direction.Down))
                return new List<Vector3> { startPos, endPos };
        }

        // --- 규칙 2: 6점 우회 연결 ---
        bool requiresCPath = false;
        // Case A: 일직선이지만, 2점 직선 연결 조건이 아닐 때 (예: Up->Up)
        if (isAligned)
        {
            requiresCPath = true;
        }
        // Case B: 일직선이 아니면서, 포트가 서로 등을 돌린 형태일 때
        else
        {
            if ((isD1Above && startDir == ConnectionPoint.Direction.Up && endDir == ConnectionPoint.Direction.Down) ||
                (!isD1Above && startDir == ConnectionPoint.Direction.Down && endDir == ConnectionPoint.Direction.Up))
                requiresCPath = true;
        }

        if (requiresCPath)
        {
            return Calculate_C_Path(startPoint, endPoint);
        }

        // --- 규칙 3: 그 외 모든 경우는 기본 4점 'L'자 연결 ---
        // (이제 Down -> Down 연결은 이 규칙을 따르게 됩니다)
        return Calculate_L_Path(startPoint, endPos, endPoint);
    }

    // --- 경로 계산을 위한 헬퍼 함수들 ---

    //  부품을 우회하는 4점 'L'자 경로 계산 (오른쪽 그림과 같이 수정)
    private List<Vector3> Calculate_L_Path(ConnectionPoint startPoint, Vector3 endPosVec3, ConnectionPoint endPoint = null)
    {
        Vector3 startPos = PosToV3(startPoint.transform.position);
        Vector3 endPos = (endPoint != null) ? PosToV3(endPoint.transform.position) : PosToV3(endPosVec3);

        List<Vector3> path = new List<Vector3>();
        path.Add(startPos);

        // 1. 시작 포트에서 clearance만큼 수직으로 뻗어나오는 stub
        Vector3 startStub = GetStubPoint(startPoint, startPos);
        path.Add(startStub);

        // 2. 최종 도착 지점의 stub (마우스 위치일 경우 endPosVec3 그대로 사용)
        Vector3 actualEndStub = (endPoint != null) ? GetStubPoint(endPoint, endPos) : PosToV3(endPosVec3);

        // 3. 부품을 우회하기 위한 중간 지점 계산
        // 시작 포트의 수직선과 도착 포트의 수평선이 만나는 지점을 찾습니다.
        // X축 기준으로 정렬되어 있지 않다면, X축으로 먼저 이동합니다.
        float targetX = (startPoint.pointDirection == ConnectionPoint.Direction.Up || startPoint.pointDirection == ConnectionPoint.Direction.Down)
            ? actualEndStub.x
            : startStub.x;

        float targetY = (startPoint.pointDirection == ConnectionPoint.Direction.Up || startPoint.pointDirection == ConnectionPoint.Direction.Down)
            ? startStub.y
            : actualEndStub.y;

        // 부품의 바깥쪽으로 경로를 유도하기 위한 추가 조정
        if (startPoint.pointDirection == ConnectionPoint.Direction.Down && startStub.y < actualEndStub.y && startStub.x != actualEndStub.x)
        {
            // D1.Down -> D2.Down 일 때 (오른쪽 그림처럼 아래로 크게 우회)
            float maxBottomY = Mathf.Min(startPoint.parentCollider.bounds.min.y, (endPoint != null ? endPoint.parentCollider.bounds.min.y : startStub.y - clearance)) - clearance;

            // 경로가 X축으로 먼저 꺾이도록 조정 (아래로 내려갔다가 횡 이동)
            path.Add(new Vector3(startStub.x, maxBottomY, 0)); // 스위치 아래로
            path.Add(new Vector3(actualEndStub.x, maxBottomY, 0)); // 전구 아래까지
            path.Add(new Vector3(actualEndStub.x, actualEndStub.y, 0)); // 전구 포트까지
        }
        else if (startPoint.pointDirection == ConnectionPoint.Direction.Up && startStub.y > actualEndStub.y && startStub.x != actualEndStub.x)
        {
            // D1.Up -> D2.Up 일 때 (위쪽으로 크게 우회)
            float maxTopY = Mathf.Max(startPoint.parentCollider.bounds.max.y, (endPoint != null ? endPoint.parentCollider.bounds.max.y : startStub.y + clearance)) + clearance;

            // 경로가 X축으로 먼저 꺾이도록 조정 (위로 올라갔다가 횡 이동)
            path.Add(new Vector3(startStub.x, maxTopY, 0)); // 스위치 위로
            path.Add(new Vector3(actualEndStub.x, maxTopY, 0)); // 전구 위까지
            path.Add(new Vector3(actualEndStub.x, actualEndStub.y, 0)); // 전구 포트까지
        }
        else
        {
            // 일반적인 L자 형태
            if (Mathf.Abs(startStub.x - actualEndStub.x) < 0.01f || Mathf.Abs(startStub.y - actualEndStub.y) < 0.01f)
            {
                // 거의 직선인 경우
                path.Add(actualEndStub);
            }
            else
            {
                // 기본 L자 코너
                path.Add(new Vector3(actualEndStub.x, startStub.y, 0));
                path.Add(actualEndStub);
            }
        }

        // 최종 도착점
        if (endPoint != null)
        {
            path.Add(endPos);
        }

        return path.Distinct().ToList(); // 중복 포인트 제거
    }

    // 6점 'ㄷ'자 우회 경로 계산 (바깥쪽으로 크게 우회하는 로직으로 수정)
    private List<Vector3> Calculate_C_Path(ConnectionPoint startPoint, ConnectionPoint endPoint)
    {
        Vector3 startPos = PosToV3(startPoint.transform.position);
        Vector3 endPos = PosToV3(endPoint.transform.position);
        Vector3 startStub = GetStubPoint(startPoint, startPos);
        Vector3 endStub = GetStubPoint(endPoint, endPos);

        Vector3 midPoint1, midPoint2;

        bool isVertical = (startPoint.pointDirection == ConnectionPoint.Direction.Up ||
                           startPoint.pointDirection == ConnectionPoint.Direction.Down);

        // 우회 방향 결정 (시작점이 끝점보다 왼쪽에 있으면 오른쪽으로, 오른쪽에 있으면 왼쪽으로 우회)
        float xOffsetDirection = (startPos.x < endPos.x) ? 1f : -1f;
        // 우회 방향 결정 (시작점이 끝점보다 아래에 있으면 위쪽으로, 위에 있으면 아래쪽으로 우회)
        float yOffsetDirection = (startPos.y < endPos.y) ? 1f : -1f;

        if (isVertical) // 포트가 위/아래 방향일 때 (좌/우로 우회)
        {
            // 두 부품의 좌우 경계 중 더 바깥쪽 X좌표를 찾고, 거기에 clearance를 더해 우회 지점 설정
            float boundaryX1 = startPoint.parentCollider.bounds.center.x + startPoint.parentCollider.bounds.extents.x * xOffsetDirection;
            float boundaryX2 = endPoint.parentCollider.bounds.center.x + endPoint.parentCollider.bounds.extents.x * xOffsetDirection;
            float detourX = (xOffsetDirection > 0) ? Mathf.Max(boundaryX1, boundaryX2) : Mathf.Min(boundaryX1, boundaryX2);

            midPoint1 = new Vector3(detourX + (clearance * xOffsetDirection), startStub.y, 0);
            midPoint2 = new Vector3(detourX + (clearance * xOffsetDirection), endStub.y, 0);
        }
        else // 포트가 좌/우 방향일 때 (위/아래로 우회)
        {
            // 두 부품의 상하 경계 중 더 바깥쪽 Y좌표를 찾고, 거기에 clearance를 더해 우회 지점 설정
            float boundaryY1 = startPoint.parentCollider.bounds.center.y + startPoint.parentCollider.bounds.extents.y * yOffsetDirection;
            float boundaryY2 = endPoint.parentCollider.bounds.center.y + endPoint.parentCollider.bounds.extents.y * yOffsetDirection;
            float detourY = (yOffsetDirection > 0) ? Mathf.Max(boundaryY1, boundaryY2) : Mathf.Min(boundaryY1, boundaryY2);

            midPoint1 = new Vector3(startStub.x, detourY + (clearance * yOffsetDirection), 0);
            midPoint2 = new Vector3(endStub.x, detourY + (clearance * yOffsetDirection), 0);
        }

        return new List<Vector3> { startPos, startStub, midPoint1, midPoint2, endStub, endPos };
    }

    private Vector3 CalculateCornerPoint(ConnectionPoint.Direction startDir, Vector3 startStub, Vector3 endPos)
    {
        return (startDir == ConnectionPoint.Direction.Up || startDir == ConnectionPoint.Direction.Down)
            ? new Vector3(endPos.x, startStub.y, 0)
            : new Vector3(startStub.x, endPos.y, 0);
    }

    private Vector3 GetStubPoint(ConnectionPoint point, Vector3 basePos)
    {
        switch (point.pointDirection)
        {
            case ConnectionPoint.Direction.Up: return basePos + Vector3.up * clearance;
            case ConnectionPoint.Direction.Down: return basePos + Vector3.down * clearance;
            // Left, Right가 없으므로 주석 처리 또는 삭제
            // case ConnectionPoint.Direction.Left:  return basePos + Vector3.left * clearance;
            // case ConnectionPoint.Direction.Right: return basePos + Vector3.right * clearance;
            default: return basePos;
        }
    }


    private void ResetState() { firstPoint = null; currentWire = null; }
    private LineRenderer CreateWire()
    {
        var wireObject = new GameObject("Wire_Preview");
        wireObject.transform.parent = content_2D;
        var lr = wireObject.AddComponent<LineRenderer>();
        lr.SetPosition(0, firstPoint.transform.position);
        lr.startWidth = 0.03f; lr.endWidth = 0.03f;         // 카메라 제어에 따라 0.05f , 0.03f
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.startColor = Color.gray; lr.endColor = Color.gray;
        lr.sortingOrder = 1;
        wireObject.AddComponent<EdgeCollider2D>();
        lr.useWorldSpace = false;
        return lr;
    }
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
    private Vector3 PosToV3(Vector3 v) => new Vector3(v.x, v.y, 0);
}