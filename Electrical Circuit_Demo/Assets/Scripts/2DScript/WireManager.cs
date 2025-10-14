using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net;

public class WireManager : MonoBehaviour
{
    [Tooltip("Wire 생성 위치")]
    public Transform content_2D;

    [Tooltip("포트에서 직선으로 뻗어 나오는 선의 길이입니다.")]
    public float clearance = 0.3f;

    public float wireWidth = 0.02f;

    private ConnectionPoint firstPoint;
    private LineRenderer currentWire;
    private GridManager gridManager;

    private Color lineColor;

    private Wire myWire;

    private int clickIndex = 0;

    [Header("Junction Settings")]
    [Tooltip("전선 분기점에 표시될 프리팹 (예: 작은 원 모양의 스프라이트)")]
    public GameObject junctionPrefab;
    [Tooltip("분기점 스냅이 작동할 반경")]
    public float junctionSnapRadius = 0.5f;

    // ✨ 현재 화면에 표시된 가상 접점 오브젝트
    private GameObject junctionPreview;
    // ✨ 가상 접점의 위치
    private Vector3 junctionPoint;

    private void Start()
    {
        gridManager = GridManager.Instance;
        lineColor = Color.gray;
    }

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
            UpdateWirePreview(currentWire);
        }
    }

    // InputManager의 방송을 수신하여 처리하는 함수
    private void HandlePhysicsClick(Collider2D hit)
    {
        if (hit == null && firstPoint != null)
        {
            // 마우스 위치를 로컬 좌표로 변환하고 그리드에 스냅
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)content_2D, Input.mousePosition, Camera.main, out Vector2 localMousePos);
            Vector2 snappedLocalPos = gridManager.SnapToGrid(localMousePos);

            if (junctionPreview != null)
            {
                // 스냅된 위치를 새로운 경유지로 추가
                AddPointToWire(currentWire);
            }
            else
            {
                AddPointToWire(currentWire);
                Destroy(junctionPreview);
            }
                return;
        }

        if (hit != null)
        {
            ConnectionPoint point = hit.GetComponent<ConnectionPoint>();
            if (point != null)
            {
                if (firstPoint == null)
                {
                    firstPoint = point;
                    currentWire = CreateWire();
                }
                else if (point != firstPoint)
                {
                    if (point.transform.parent == firstPoint.transform.parent)
                    {
                        Destroy(currentWire.gameObject);
                        ResetState();
                        return;
                    }
                    FinalizeWire(currentWire, firstPoint, point);
                    ResetState();
                }
                return;
            }

            if (isDrawing() && hit.CompareTag("Wire"))
            {
                Debug.Log("전선 클릭!");

                ConnectToExistingWire(currentWire, hit.transform, GetMouseWorldPosition());
                ResetState();
            }
        }
    }

    private void FinalizeWire(LineRenderer wire, ConnectionPoint startPoint, ConnectionPoint endPoint)
    {
        // 최종 연결 위치도 그리드에 스냅
        Vector2 endLocalPos = WorldToLocal(endPoint.transform.position);
        Vector2 snappedEndPos = gridManager.SnapToGrid(endLocalPos);

        // 마지막 점을 최종 위치로 고정
        wire.SetPosition(wire.positionCount - 1, snappedEndPos);

        wire.name = $"Wire_{startPoint.transform.parent.name}_to_{endPoint.transform.parent.name}";
        wire.gameObject.tag = "Wire";

        ElectricalComponent comp1 = startPoint.parentComponent;
        ElectricalComponent comp2 = endPoint.parentComponent;

        myWire.firstPoint = startPoint;
        myWire.lastPoint = endPoint;
        myWire.componentA = comp1;
        myWire.componentB = comp2;

        if (comp1 != null && comp2 != null)
        {
            //서로를 다음 부품으로 등록 (양방향 연결)
            comp1.AddConnection(startPoint, endPoint);
            comp2.AddConnection(endPoint, startPoint);
        }

        ColliderSetting();
    }

    //콜라이더 세팅
    private void ColliderSetting()
    {
        // 1. LineRenderer와 같은 게임 오브젝트에 있는 EdgeCollider2D를 가져옵니다.
        EdgeCollider2D edgeCollider = currentWire.GetComponent<EdgeCollider2D>();

        // 2. LineRenderer의 Vector3 좌표를 EdgeCollider2D가 사용하는 Vector2 좌표로 변환합니다.
        Vector2[] colliderPoints = new Vector2[currentWire.positionCount];
        for (int i = 0; i < currentWire.positionCount; i++)
        {
            // Z축 정보는 무시하고 X, Y 좌표만 사용합니다.
            colliderPoints[i] = new Vector2(currentWire.GetPosition(i).x, currentWire.GetPosition(i).y);
        }

        // 3. 변환된 좌표를 EdgeCollider2D의 points 속성에 설정합니다.
        edgeCollider.points = colliderPoints;
    }

    private void UpdateWirePreview(LineRenderer wire)
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        // 마우스의 로컬 좌표를 계산하고 스냅
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)content_2D, Input.mousePosition, Camera.main, out Vector2 localMousePos);
        Vector2 snappedLocalPos = gridManager.SnapToGrid(localMousePos);

        // 1. 마우스 주변에 가장 가까운 전선 위의 점을 찾습니다. (월드 좌표 기준)
        bool foundJunctionPointOnWire = FindNearestPointOnAllWires(mouseWorld, out junctionPoint);

        // 2. 만약 가까운 전선을 찾았다면
        if (foundJunctionPointOnWire)
        {
            // ✨ --- 이 부분이 핵심 수정 로직입니다 --- ✨
            // 2a. 찾은 월드 좌표(junctionPoint)를 UI 로컬 좌표로 변환합니다.
            Vector2 localJunctionPoint = WorldToLocal(junctionPoint);
            // 2b. 변환된 로컬 좌표를 그리드에 스냅하여 최종 접점 위치를 결정합니다.
            Vector2 snappedJunctionPos = gridManager.SnapToGrid(localJunctionPoint);
            // ✨ --- 여기까지 수정 --- ✨

            // 가상 접점 오브젝트가 없다면 새로 생성
            if (junctionPreview == null)
            {
                // junctionPrefab도 UI 요소이므로, 로컬 좌표를 월드 좌표로 변환하여 생성
                junctionPreview = Instantiate(junctionPrefab);
                junctionPreview.transform.SetParent(content_2D, false);
            }

            // 가상 접점의 위치(anchoredPosition)를 스냅된 로컬 좌표로 설정
            junctionPreview.GetComponent<RectTransform>().anchoredPosition = snappedJunctionPos;

            // 미리보기 선의 끝점을 가상 접점 위치로 고정
            snappedLocalPos = snappedJunctionPos;
        }
        // 3. 주변에 전선이 없다면
        else
        {
            // 이전에 표시했던 가상 접점이 있다면 파괴
            if (junctionPreview != null)
            {
                Destroy(junctionPreview);
                junctionPreview = null;
            }
        }

        // ✨ 마지막으로 고정된 지점을 가져옴
        Vector3 lastFixedPoint = wire.GetPosition(clickIndex);

        // ✨ 마지막 고정 지점과 스냅된 마우스 위치를 기준으로 L자 경로 계산
        //    (이전 경로의 방향을 고려하여 꺾이는 방향 결정)
        Vector3 previousSegment = (wire.positionCount > 2) ? (lastFixedPoint - wire.GetPosition(wire.positionCount - 3)) : Vector3.zero;

        Vector3 corner;
        // 이전 경로가 주로 수직이었으면, 이번엔 수평으로 먼저 꺾임
        if (Mathf.Abs(previousSegment.y) > Mathf.Abs(previousSegment.x))
        {
            corner = new Vector3(snappedLocalPos.x, lastFixedPoint.y, 0);
        }
        else // 이전 경로가 주로 수평이었으면, 이번엔 수직으로 먼저 꺾임
        {
            corner = new Vector3(lastFixedPoint.x, snappedLocalPos.y, 0);
        }

        // 시작점과 스냅점이 수평/수직일 때에는 2점으로 끝내기
        bool isAligned = (Mathf.Abs(lastFixedPoint.x - snappedLocalPos.x) < 10f) || (Mathf.Abs(lastFixedPoint.y - snappedLocalPos.y) < 10f);

        if(isAligned)
        {
            wire.positionCount = clickIndex + 2;
            wire.SetPosition(wire.positionCount - 1, snappedLocalPos);
        }
        else
        {

            wire.positionCount = clickIndex + 3;
            wire.SetPosition(wire.positionCount - 2, corner);
            wire.SetPosition(wire.positionCount - 1, snappedLocalPos);
        }
    }

    private List<Vector3> OptimizePath(List<Vector3> path)
    {
        if (path.Count < 3) return path;
        List<Vector3> optimizedPath = new List<Vector3> { path[0] };
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 prevDir = (path[i] - path[i - 1]).normalized;
            Vector3 nextDir = (path[i + 1] - path[i]).normalized;
            if (Vector3.Distance(prevDir, nextDir) > 0.01f)
            {
                optimizedPath.Add(path[i]);
            }
        }
        optimizedPath.Add(path.Last());
        return optimizedPath;
    }

    private void AddPointToWire(LineRenderer lr)
    {
        clickIndex = lr.positionCount - 1;
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
        if (startPoint.pointDirection == ConnectionPoint.Direction.Down && startStub.x != actualEndStub.x)
        {
            // D1.Down -> D2.Down 일 때 (오른쪽 그림처럼 아래로 크게 우회)
            float maxBottomY = Mathf.Min(startPoint.parentCollider.bounds.min.y, (endPoint != null ? endPoint.parentCollider.bounds.min.y : startStub.y - clearance)) - clearance;

            
            if (endPoint != null && endPoint.pointDirection == ConnectionPoint.Direction.Up)        // Down - Up 매칭
            {
                // 기본 L자 코너
                path.Add(new Vector3(actualEndStub.x, startStub.y, 0));
                path.Add(actualEndStub);
            }
            else
            {
                // 경로가 X축으로 먼저 꺾이도록 조정 (아래로 내려갔다가 횡 이동)
                path.Remove(startStub);
                path.Add(new Vector3(startStub.x, maxBottomY, 0)); // 스위치 아래로
                path.Add(new Vector3(actualEndStub.x, maxBottomY, 0)); // 전구 아래까지
                path.Add(new Vector3(actualEndStub.x, endPos.y, 0)); // 전구 포트까지
            }
        }
        else if (startPoint.pointDirection == ConnectionPoint.Direction.Up && startStub.x != actualEndStub.x)
        {
            // D1.Up -> D2.Up 일 때 (위쪽으로 크게 우회)
            float maxTopY = Mathf.Max(startPoint.parentCollider.bounds.max.y, (endPoint != null ? endPoint.parentCollider.bounds.max.y : startStub.y + clearance)) + clearance;

            if (endPoint != null && endPoint.pointDirection == ConnectionPoint.Direction.Down)      // Up - Down 매칭
            {
                // 기본 L자 코너
                path.Add(new Vector3(actualEndStub.x, startStub.y, 0));
                path.Add(actualEndStub);
            }
            else
            {
                // 경로가 X축으로 먼저 꺾이도록 조정 (위로 올라갔다가 횡 이동)
                path.Remove(startStub);
                path.Add(new Vector3(startStub.x, maxTopY, 0)); // 첫번째 심볼 최대지점
                path.Add(new Vector3(actualEndStub.x, maxTopY, 0)); // 두번째 심볼 위까지
                path.Add(new Vector3(actualEndStub.x, endPos.y, 0)); // 두번째 심볼 최소지점까지
            } 
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

    private Vector3 GetStubPoint(ConnectionPoint point, Vector3 basePos)
    {
        switch (point.pointDirection)
        {
            case ConnectionPoint.Direction.Up: return basePos + Vector3.up * clearance;
            case ConnectionPoint.Direction.Down: return basePos + Vector3.down * clearance;
            default: return basePos;
        }
    }


    private void ResetState() { firstPoint = null; currentWire = null; clickIndex = 0; }
    private LineRenderer CreateWire()
    {
        var wireObject = new GameObject("Wire_Preview");
        //wireObject.transform.parent = content_2D;
        wireObject.transform.SetParent(content_2D, false); // ✨ 월드 좌표 유지 없이 부모 설정
        wireObject.transform.localPosition = Vector3.zero;

        var lr = wireObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        // ✨ 시작점을 로컬 좌표로 변환하여 설정
        Vector2 startLocalPos = WorldToLocal(firstPoint.transform.position);
        lr.positionCount = 2; // 시작점 + 미리보기 끝점
        lr.SetPosition(0, startLocalPos);

        lr.startWidth = wireWidth; lr.endWidth = wireWidth;         
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.startColor = lineColor; lr.endColor = lineColor;
        lr.sortingOrder = 1;

        wireObject.AddComponent<EdgeCollider2D>();
        myWire = wireObject.AddComponent<Wire>();

        CircuitSolver.Instance.allWires.Add(myWire);
        return lr;
    }

    /// <summary>
    /// 현재 그리던 선을 기존의 다른 전선에 연결합니다.
    /// </summary>
    private void ConnectToExistingWire(LineRenderer drawingWire, Transform targetWireTransform, Vector3 clickPosition)
    {
        LineRenderer targetWire = targetWireTransform.GetComponent<LineRenderer>();
        if (targetWire == null) return;

        // 1. 클릭된 위치에서 가장 가까운 기존 전선의 점(vertex) 찾기
        float minDistance = float.MaxValue;
        int closestSegmentIndex = -1;
        Vector3 closestPointOnLine = Vector3.zero;

        for (int i = 0; i < targetWire.positionCount - 1; i++)
        {
            // LineRenderer의 로컬 좌표를 월드 좌표로 변환하여 계산
            Vector3 p1 = targetWire.transform.TransformPoint(targetWire.GetPosition(i));
            Vector3 p2 = targetWire.transform.TransformPoint(targetWire.GetPosition(i + 1));

            Vector3 pointOnSegment = FindNearestPointOnLineSegment(p1, p2, clickPosition);
            float distance = Vector3.Distance(clickPosition, pointOnSegment);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestSegmentIndex = i;
                closestPointOnLine = pointOnSegment;
            }
        }

        if (closestSegmentIndex != -1)
        {
            // 2. 그리던 선의 끝점을 가장 가까운 지점(로컬 좌표로 변환)으로 설정하여 연결 완료
            Vector3 finalLocalPos = drawingWire.transform.InverseTransformPoint(closestPointOnLine);
            drawingWire.SetPosition(drawingWire.positionCount - 1, finalLocalPos);

            //drawingWire.name = $"Wire_{startPoint.transform.parent.name}_to_{endPoint.transform.parent.name}";
            drawingWire.gameObject.tag = "Wire";

            // 3. 기존 전선에 새로운 점을 삽입하여 T자 형태 만들기
            Vector3 newPointInLocalSpace = targetWire.transform.InverseTransformPoint(closestPointOnLine);

            List<Vector3> points = new List<Vector3>();
            Vector3[] existingPoints = new Vector3[targetWire.positionCount];
            targetWire.GetPositions(existingPoints);
            points.AddRange(existingPoints);

            points.Insert(closestSegmentIndex + 1, newPointInLocalSpace);

            //targetWire.positionCount = points.Count;
            //targetWire.SetPositions(points.ToArray());

            ColliderSetting();

            // TODO: ElectricalComponent의 연결 정보(connections)도 업데이트하는 로직 필요
        }
    }

    // 선분(p1-p2) 위의 점 중 clickPosition에서 가장 가까운 점을 찾는 함수
    private Vector3 FindNearestPointOnLineSegment(Vector3 p1, Vector3 p2, Vector3 clickPosition)
    {
        Vector3 lineDirection = (p2 - p1).normalized;
        float projectionLength = Vector3.Dot(clickPosition - p1, lineDirection);
        float segmentLength = Vector3.Distance(p1, p2);

        projectionLength = Mathf.Clamp(projectionLength, 0, segmentLength);

        return p1 + lineDirection * projectionLength;
    }

    private Vector3 PosToV3(Vector3 v) => new Vector3(v.x, v.y, 0);

    private Vector2 WorldToLocal(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)content_2D, Camera.main.WorldToScreenPoint(worldPos), Camera.main, out Vector2 localPos);
        return localPos;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private bool isDrawing()
    {
        return currentWire != null;
    }

    private bool FindNearestPointOnAllWires(Vector3 position, out Vector3 nearestPoint)
    {
        nearestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        // 씬의 모든 Wire 태그를 가진 전선을 찾음
        GameObject[] wires = GameObject.FindGameObjectsWithTag("Wire");

        foreach (var wireObject in wires)
        {
            LineRenderer lr = wireObject.GetComponent<LineRenderer>();
            if (lr == null || lr == currentWire) continue; // 자기 자신은 제외

            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 p1 = lr.transform.TransformPoint(lr.GetPosition(i));
                Vector3 p2 = lr.transform.TransformPoint(lr.GetPosition(i + 1));

                Vector3 pointOnSegment = FindNearestPointOnLineSegment(p1, p2, position);
                float distance = Vector3.Distance(position, pointOnSegment);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = pointOnSegment;
                }
            }
        }

        // 가장 가까운 전선과의 거리가 스냅 반경 안에 있을 때만 true 반환
        return minDistance < junctionSnapRadius;
    }
}