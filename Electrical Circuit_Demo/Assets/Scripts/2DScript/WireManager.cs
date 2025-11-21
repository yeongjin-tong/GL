using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net;
using System;
using Unity.VisualScripting;

public class WireManager : MonoBehaviour
{
    public static WireManager Instance { get; private set; }

    public Material wireMaterial;

    [Tooltip("Wire 생성 위치")]
    public Transform content_2D;

    [Tooltip("포트에서 직선으로 뻗어 나오는 선의 길이입니다.")]
    public float clearance = 0.3f;

    public float wireWidth = 0.02f;

    private ConnectionPoint firstPoint;
    public LineRenderer currentWire;
    private GridManager gridManager;

    [HideInInspector]
    public Color defaultLineColor = Color.gray;
    private Color32 movingLineColor = new Color32(121, 237, 255, 255);      // 현재 하늘색

    private Wire myWire;

    private int lineIndex = 0;      // 라인 꺾을 때 쓰는 인덱스

    private int clickIndex = 0;     // 클릭 횟수

    [Header("Junction Settings")]
    [Tooltip("전선 분기점에 표시될 프리팹 (예: 작은 원 모양의 스프라이트)")]
    public GameObject junctionPrefab;
    [Tooltip("분기점 스냅이 작동할 반경")]
    public float junctionSnapRadius = 0.2f;

    //  현재 화면에 표시된 가상 접점 오브젝트
    private GameObject junctionPreview;
    //  가상 접점의 위치
    private Vector3 junctionPoint;

    private bool isPreviewColliding = false;

    private int junctionIndex = 0;
    // 가장 가까운 전선으로 감지된 오브젝트의 Transform
    private Transform nearestWireForPreview;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        gridManager = GridManager.Instance;
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

            if(Input.GetMouseButtonDown(1))
            {
                Destroy(currentWire.gameObject);

                ResetState();

                if (junctionPreview != null)
                {
                    Destroy(junctionPreview);
                    junctionPreview = null;
                }
            }
        }
    }

    // InputManager의 방송을 수신하여 처리하는 함수
    private void HandlePhysicsClick(Collider2D hit)
    {
        if (isDrawing() && junctionPreview != null && nearestWireForPreview != null)
        {
            if (isPreviewColliding)
            {
                Debug.Log("배치 실패: 그리는 중인 선이 충돌 상태입니다.");

                Destroy(currentWire.gameObject); // 빨간색 미리보기 선 파괴
                ResetState(); // 그리기 취소
                return; // 연결 로직 실행 중단
            }

            Debug.Log("Junction preview 클릭! 병렬 연결을 시작합니다.");
            ConnectToExistingWire(currentWire, nearestWireForPreview, GetMouseWorldPosition());
            ResetState();
            return; // 여기서 함수를 종료하여 아래 로직이 실행되지 않도록 함
        }

        if (hit == null && firstPoint != null)  // 허공에 클릭해서 선 꺾기
        {
            clickIndex++;
            if (clickIndex  < 3)
            {
                // 마우스 위치를 로컬 좌표로 변환하고 그리드에 스냅
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)content_2D, Input.mousePosition, Camera.main, out Vector2 localMousePos);
                Vector2 snappedLocalPos = gridManager.SnapToGrid(localMousePos);

                AddPointToWire(currentWire);

                if (junctionPreview != null)
                {
                    Destroy(junctionPreview);
                }
            }
            else
            {
                Debug.Log("선 연결 실패: 선을 너무 많이 꺾지 마세요.");
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

                if (isPreviewColliding)
                {
                    Debug.Log("배치 실패: 그리는 중인 선이 충돌 상태입니다.");

                    Destroy(currentWire.gameObject); // 빨간색 미리보기 선 파괴
                    ResetState(); // 그리기 취소
                    return; // 연결 로직 실행 중단
                }
                // (충돌 중이 아닐 때만 아래 연결 로직 실행)
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

        // 2. LineRenderer에서 현재까지 그린 경로를 가져옵니다.
        Vector3[] currentPathPoints = new Vector3[wire.positionCount];
        wire.GetPositions(currentPathPoints);

        // 3. 경로의 마지막 점을 스냅된 최종 위치로 업데이트합니다.
        currentPathPoints[wire.positionCount - 1] = snappedEndPos;

        // 4. ✨ [핵심 수정] OptimizePath를 호출하여 경로를 최적화합니다. ✨
        List<Vector3> optimizedPath = OptimizePath(currentPathPoints.ToList());

        // 5. 최적화된 경로를 LineRenderer에 최종 설정합니다.
        wire.positionCount = optimizedPath.Count;
        wire.SetPositions(optimizedPath.ToArray());

        // 마지막 점을 최종 위치로 고정
        wire.name = $"Wire_{startPoint.transform.parent.name}_to_{endPoint.transform.parent.name}";
        wire.gameObject.tag = "Wire";

        myWire.connectedPoints.Add(endPoint);
        wire.startColor = defaultLineColor;
        wire.endColor = defaultLineColor;

        // CircuitGraph에 연결 등록
        CircuitGraph.Instance.RegisterConnection(startPoint.parentComponent, endPoint.parentComponent);

        wire.AddComponent<EdgeCollider2D>();
        //Wire wire1 = wire.GetComponent<Wire>();
        //RedrawWire(wire1, endPoint.parentComponent);        // 가끔 대각선으로 그려질 때가 있어, 한번 더 정리

        currentWire = wire;
        ColliderSetting();
        currentWire = null;
    }

    //콜라이더 세팅
    private void ColliderSetting()
    {
        if (currentWire == null) return;

        // 1. LineRenderer와 같은 게임 오브젝트에 있는 EdgeCollider2D를 가져옵니다.
        EdgeCollider2D edgeCollider = currentWire.GetComponent<EdgeCollider2D>();
        if (edgeCollider == null) return;

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
        bool foundJunctionPointOnWire = FindNearestPointOnAllWires(mouseWorld, out junctionPoint, out nearestWireForPreview);

        // 2. 만약 가까운 전선을 찾았다면
        if (foundJunctionPointOnWire)
        {
            // 2a. 찾은 월드 좌표(junctionPoint)를 UI 로컬 좌표로 변환합니다.
            Vector2 localJunctionPoint = WorldToLocal(junctionPoint);
            // 2b. 변환된 로컬 좌표를 그리드에 스냅하여 최종 접점 위치를 결정합니다.
            Vector2 snappedJunctionPos = gridManager.SnapToGrid(localJunctionPoint);

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
            nearestWireForPreview = null;
        }

        Vector3 lastFixedPoint = wire.GetPosition(lineIndex);

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
            wire.positionCount = lineIndex + 2;
            wire.SetPosition(wire.positionCount - 1, snappedLocalPos);
        }
        else
        {

            wire.positionCount = lineIndex + 3;
            wire.SetPosition(wire.positionCount - 2, corner);
            wire.SetPosition(wire.positionCount - 1, snappedLocalPos);
        }
    }

    private void AddPointToWire(LineRenderer lr)
    {
        lineIndex = lr.positionCount - 1;
    }

    private void ResetState() 
    {
        firstPoint = null; 
        currentWire = null; 
        lineIndex = 0;
        clickIndex = 0;
        isPreviewColliding = false;
    }
    private LineRenderer CreateWire()
    {
        var wireObject = new GameObject("Wire_Preview");
        wireObject.transform.SetParent(content_2D, false); // ✨ 월드 좌표 유지 없이 부모 설정
        wireObject.transform.localPosition = Vector3.zero;

        var lr = wireObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        Vector2 startLocalPos = WorldToLocal(firstPoint.transform.position);
        Vector2 snappedStartPos = gridManager.SnapToGrid(startLocalPos);

        lr.positionCount = 2; // 시작점 + 미리보기 끝점
        lr.SetPosition(0, snappedStartPos);

        lr.startWidth = wireWidth; lr.endWidth = wireWidth;
        lr.material = wireMaterial;
        lr.startColor = movingLineColor; lr.endColor = movingLineColor;
        lr.sortingOrder = 1;

        //EdgeCollider2D wireCollider = wireObject.AddComponent<EdgeCollider2D>();
        myWire = wireObject.AddComponent<Wire>();

        myWire.connectedPoints.Add(firstPoint);
        return lr;
    }


    private void ConnectToExistingWire(LineRenderer drawingWire, Transform targetWireTransform, Vector3 clickPosition)
    {
        Wire targetWire = targetWireTransform.GetComponent<Wire>();
        LineRenderer targetLineRenderer = targetWireTransform.GetComponent<LineRenderer>();

        if (targetWire == null || targetLineRenderer == null) return;

        Vector2 snappedJunctionPos;
        Vector3 worldPositionToSplit; // 분할 지점을 찾기 위한 월드 좌표

        // 1. junctionPreview가 존재한다면, clickPosition을 무시하고
        //    junctionPreview의 '이미 스냅된' 로컬 좌표를 사용합니다.
        if (junctionPreview != null)
        {
            snappedJunctionPos = junctionPreview.GetComponent<RectTransform>().anchoredPosition;
            // 로컬 좌표를 다시 월드 좌표로 변환하여 '가장 가까운 선분'을 찾는 데 사용합니다.
            worldPositionToSplit = junctionPreview.transform.position;
        }
        else
        {
            // 2. junctionPreview가 없다면 (fallback, 예: 전선을 직접 클릭), 
            //    기존처럼 clickPosition을 기반으로 위치를 계산합니다.
            Vector2 localClickPos = WorldToLocal(clickPosition);
            snappedJunctionPos = gridManager.SnapToGrid(localClickPos);
            worldPositionToSplit = clickPosition; // fallback은 그냥 마우스 월드 클릭 좌표
        }

        // --- (이하 로직은 'snappedJunctionPos'와 'worldPositionToSplit'를 사용) ---

        // 2a. 기존 전선의 전체 경로와 끝점 정보를 '복사'해둡니다.
        Vector3[] originalPathPoints = new Vector3[targetLineRenderer.positionCount];
        targetLineRenderer.GetPositions(originalPathPoints);
        ConnectionPoint originalPointA = targetWire.connectedPoints[0];
        ConnectionPoint originalPointB = targetWire.connectedPoints[1];

        // 2b. 수정: 'worldPositionToSplit' (스냅된 프리뷰 위치)를 기준으로 가장 가까운 점의 인덱스를 찾습니다.
        int closestSegmentIndex = -1;
        float minDistance = float.MaxValue;
        for (int i = 0; i < originalPathPoints.Length - 1; i++)
        {
            Vector3 p1 = targetLineRenderer.transform.TransformPoint(originalPathPoints[i]);
            Vector3 p2 = targetLineRenderer.transform.TransformPoint(originalPathPoints[i + 1]);
            // clickPosition 대신 worldPositionToSplit 사용
            Vector3 pointOnSegment = FindNearestPointOnLineSegment(p1, p2, worldPositionToSplit);
            float distance = Vector3.Distance(worldPositionToSplit, pointOnSegment); // ✨ clickPosition 대신 worldPositionToSplit 사용

            if (distance < minDistance)
            {
                minDistance = distance;
                closestSegmentIndex = i;
            }
        }

        // 3. 분기점(Junction) 오브젝트를 생성합니다.
        junctionIndex++;
        //  junctionPreview가 있으면 재활용하고, 없으면 새로 만듭니다.
        var junctionObj = (junctionPreview != null) ? junctionPreview : Instantiate(junctionPrefab);
        junctionObj.name = "Junction_" + junctionIndex;
        junctionObj.transform.SetParent(content_2D, false);
        junctionObj.GetComponent<RectTransform>().anchoredPosition = snappedJunctionPos;
        var junctionPoint = junctionObj.AddComponent<ConnectionPoint>();
        var junctionComp = junctionObj.AddComponent<Junction>();
        junctionPoint.parentComponent = junctionComp;

        // 4. 기존 전선과 임시 전선을 파괴합니다.
        Destroy(targetWire.gameObject);
        Wire drawingWireInfo = drawingWire.GetComponent<Wire>();
        ConnectionPoint newPointC = drawingWireInfo.connectedPoints[0];
        Destroy(drawingWire.gameObject);
        junctionPreview = null;

        // 5. 복사해둔 경로를 분할하여 새 전선 3개를 생성합니다.
        // (이하 모든 로직은 이미 'snappedJunctionPos'를 사용하므로 올바르게 동작합니다)

        // 5a. 새 전선 1 (A -> Junction): 원본 경로의 앞부분을 계승합니다.
        List<Vector3> pathForA = originalPathPoints.Take(closestSegmentIndex + 1).ToList();
        pathForA.Add(snappedJunctionPos);
        CreateWireWithPath(originalPointA, junctionPoint, pathForA);

        // 5b. 새 전선 2 (B -> Junction): 원본 경로의 뒷부분을 계승합니다.
        List<Vector3> pathForB = originalPathPoints.Skip(closestSegmentIndex + 1).ToList();
        pathForB.Insert(0, snappedJunctionPos);
        CreateWireWithPath(originalPointB, junctionPoint, pathForB);

        // 5c. 새 전선 3 (C -> Junction): '방금까지 그리던' 경로를 계승합니다.
        LineRenderer drawingLineRenderer = drawingWire.GetComponent<LineRenderer>();
        Vector3[] drawingPathPoints = new Vector3[drawingLineRenderer.positionCount];
        drawingLineRenderer.GetPositions(drawingPathPoints);

        List<Vector3> pathForC = drawingPathPoints.ToList();
        pathForC[pathForC.Count - 1] = snappedJunctionPos;

        CreateWireWithPath(newPointC, junctionPoint, pathForC);
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

    public Vector2 WorldToLocal(Vector3 worldPos)
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

    private bool FindNearestPointOnAllWires(Vector3 position, out Vector3 nearestPoint, out Transform nearestWireTransform)
    {
        nearestPoint = Vector3.zero;
        nearestWireTransform = null;
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
                    nearestWireTransform = wireObject.transform;
                                
                }
            }
        }

        // 가장 가까운 전선과의 거리가 스냅 반경 안에 있을 때만 true 반환
        return minDistance < junctionSnapRadius;
    }

    /// <summary>
    /// 지정된 경로(points)를 사용하여 새로운 Wire를 생성하는 헬퍼 함수입니다.
    /// </summary>
    public void CreateWireWithPath(ConnectionPoint startPoint, ConnectionPoint endPoint, List<Vector3> pathPoints)
    {
        // 1. 새 Wire 오브젝트 생성
        var wireObject = new GameObject($"Wire_{startPoint.parentComponent.name}_to_{endPoint.parentComponent.name}");
        wireObject.transform.SetParent(content_2D, false);
        wireObject.tag = "Wire";

        // 2. LineRenderer 설정 및 경로 적용
        var lr = wireObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = wireWidth; lr.endWidth = wireWidth;
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.startColor = defaultLineColor; lr.endColor = defaultLineColor;
        lr.sortingOrder = 1;

        List<Vector3> optimizedPath = OptimizePath(pathPoints);

        Vector2 startPos = gridManager.SnapToGrid(WorldToLocal(startPoint.transform.position));
        Vector2 endPos = gridManager.SnapToGrid(WorldToLocal(endPoint.transform.position));

        Vector2 pathStartPos = optimizedPath[0];
        Vector2 pathEndPos = optimizedPath.Last();

        var newWire = wireObject.AddComponent<Wire>();

        if(Vector2.Distance(startPos, pathStartPos) < 0.1f && Vector2.Distance(endPos, pathEndPos) < 0.1f)
        {
            // 정방향 P0 -> Pn (startPoint -> endPoint)
            newWire.connectedPoints.Add(startPoint);
            newWire.connectedPoints.Add(endPoint);
        }
        else if(Vector2.Distance(startPos, pathEndPos) < 0.1f && Vector2.Distance(endPos, pathStartPos) < 0.1f)
        {
            // 역방향 Pn -> P0 (endPoint -> startPoint)
            optimizedPath.Reverse();

            newWire.connectedPoints.Add(startPoint);
            newWire.connectedPoints.Add(endPoint);
        }
        else
        {
            Debug.LogError($"Wire 경로 매칭 실패: {wireObject.name}");
            newWire.connectedPoints.Add(startPoint);
            newWire.connectedPoints.Add(endPoint);
        }

        lr.positionCount = optimizedPath.Count;
        lr.SetPositions(optimizedPath.ToArray());

        // 4. EdgeCollider2D 설정
        var edgeCollider = wireObject.AddComponent<EdgeCollider2D>();
        Vector2[] colliderPoints = optimizedPath.Select(p => new Vector2(p.x, p.y)).ToArray();
        edgeCollider.points = colliderPoints;

        // 5. CircuitGraph에 최종 연결 등록
        CircuitGraph.Instance.RegisterConnection(startPoint.parentComponent, endPoint.parentComponent);
    }

    /// <summary>
    /// 움직인 부품에 연결된 모든 전선을 다시 그립니다.
    /// </summary>
    public void RedrawWiresForComponent(ElectricalComponent movedComponent)
    {
        if (movedComponent == null) return;

        // FindObjectsOfType은 무거우므로, allWires 리스트를 직접 관리하는 것이 더 효율적입니다.
        Wire[] allWires = FindObjectsOfType<Wire>();

        foreach (var wire in allWires)
        {
            if (wire.ConnectedComponents.Contains(movedComponent))
            {
                RedrawWire(wire, movedComponent);
            }
        }
    }

    /// <summary>
    /// (최종 수정) 와이어의 기존 직각 구조(3+점)를 '기억'하고,
    //  2점 경로는 4점으로 확장합니다.
    /// </summary>
    public void RedrawWire(Wire wireToRedraw, ElectricalComponent movedComponent)
    {
        LineRenderer lr = wireToRedraw.GetComponent<LineRenderer>();
        if (lr == null) return;

        // --- 1. 핵심 정보 가져오기 ---
        ConnectionPoint movedPoint = null;
        ConnectionPoint staticPoint = null;
        foreach (var point in wireToRedraw.connectedPoints)
        {
            if (movedComponent == point.parentComponent)
                movedPoint = point;
            else
                staticPoint = point;
        }
        if (movedPoint == null || staticPoint == null) return;

        // --- 2. 두 끝점의 목표 좌표 계산 ---
        Vector2 newEndPos = gridManager.SnapToGrid(WorldToLocal(movedPoint.transform.position));
        Vector2 staticPos = gridManager.SnapToGrid(WorldToLocal(staticPoint.transform.position));

        bool isStartMoving = (wireToRedraw.connectedPoints[0] == movedPoint);

        Vector2 pStart = isStartMoving ? newEndPos : staticPos;
        Vector2 pEnd = isStartMoving ? staticPos : newEndPos;

        Vector3[] currentPath = new Vector3[lr.positionCount];
        lr.GetPositions(currentPath);
        int pointCount = currentPath.Length;

        // --- 3. 두 끝점이 수평/수직으로 정렬되어 있는지 (직선 가능 여부) 확인 ---
        bool isAligned = Mathf.Abs(pStart.x - pEnd.x) < 0.01f || Mathf.Abs(pStart.y - pEnd.y) < 0.01f;

        List<Vector3> newPath;

        // --- 4. 경로 상태에 따라 분기 ---

        if (pointCount < 3) // CASE 1: 기존 경로가 2점(직선)이었을 때
        {
            if (isAligned)
            {
                // Case A: 2점 -> 2점 (여전히 직선 유지)
                newPath = new List<Vector3> { pStart, pEnd };
            }
            else
            {
                // Case B: 2점 -> 4점 ('Z'자 꺾임 경로로 확장)
                float midY = (pStart.y + pEnd.y) / 2.0f;

                Vector2 p1 = gridManager.SnapToGrid(new Vector2(pStart.x, midY));
                Vector2 p2 = gridManager.SnapToGrid(new Vector2(pEnd.x, midY));

                newPath = new List<Vector3> { pStart, p1, p2, pEnd };
            }

            lr.positionCount = newPath.Count;
            lr.SetPositions(newPath.ToArray());
        }
        else // ✨ [수정된 CASE 2] : 기존 경로가 3점 이상(꺾임)이었을 때
        {
            newPath = currentPath.ToList(); // 기존 경로를 복사

            int endIndex = isStartMoving ? 0 : (pointCount - 1);
            int elbowIndex = isStartMoving ? 1 : (pointCount - 2);

            Vector3 newElbowPos = Vector3.zero; // 계산된 새 팔꿈치 위치

            if (pointCount == 3) // --- 3점 경로("ㄱ", "ㄴ") 전용 로직 ---
            {
                Vector3 anchorPoint;
                Vector3 elbowPoint = newPath[1]; // P1은 항상 팔꿈치
                bool isStaticSegmentHorizontal;

                if (isStartMoving) // P0 드래그
                {
                    anchorPoint = newPath[2]; // P2가 고정점
                    isStaticSegmentHorizontal = Mathf.Abs(anchorPoint.y - elbowPoint.y) < 0.1f;
                }
                else // P2 드래그
                {
                    anchorPoint = newPath[0]; // P0가 고정점
                    isStaticSegmentHorizontal = Mathf.Abs(anchorPoint.y - elbowPoint.y) < 0.1f;
                }

                if (isStaticSegmentHorizontal) // "ㄱ" 또는 "г" 형태
                {
                    newElbowPos.y = anchorPoint.y;
                    newElbowPos.x = newEndPos.x;
                }
                else // "ㄴ" 또는 "L" 형태
                {
                    newElbowPos.x = anchorPoint.x;
                    newElbowPos.y = newEndPos.y;
                }
            }
            else // --- 4점+ 경로("Z" 등) 전용 로직 ---
            {
                Vector3 staticAnchorPoint; // 팔꿈치의 기준점 (P1 또는 P2)

                // ✨ [핵심 버그 수정] : 고정점(Anchor) 인덱스 수정
                if (isStartMoving)
                {
                    // P0 드래그. 팔꿈치(P1)의 고정점은 P2.
                    staticAnchorPoint = newPath[elbowIndex + 1]; // P2
                }
                else
                {
                    // Pn 드래그. 팔꿈치(Pn-1)의 고정점은 Pn-2.
                    staticAnchorPoint = newPath[elbowIndex - 1]; // Pn-2
                }

                // "메모리"는 가장 바깥쪽 고정 세그먼트를 기준으로 함
                bool isOutermostStaticSegmentVertical;
                if (isStartMoving)
                {
                    // P0 드래그 시, 메모리는 Pn -> Pn-1 (예: P3->P2)
                    Vector3 farAnchor = newPath[pointCount - 1];
                    Vector3 farElbow = newPath[pointCount - 2];
                    isOutermostStaticSegmentVertical = Mathf.Abs(farAnchor.x - farElbow.x) < 0.1f;
                }
                else
                {
                    // Pn 드래그 시, 메모리는 P0 -> P1
                    Vector3 farAnchor = newPath[0];
                    Vector3 farElbow = newPath[1];
                    isOutermostStaticSegmentVertical = Mathf.Abs(farAnchor.x - farElbow.x) < 0.1f;
                }

                // Z 경로는 방향이 *반대*여야 함
                if (isStartMoving)
                {
                    // P0 드래그. 메모리(P3->P2), 팔꿈치(P1), 고정점(P2)
                    if (isOutermostStaticSegmentVertical) // P3->P2가 수직 (이미지 1의 경우)
                    {
                        // P0->P1은 수평이어야 함
                        newElbowPos.x = newEndPos.x;         // X는 P0 따름
                        newElbowPos.y = staticAnchorPoint.y; // Y는 P2에 고정 (버그 수정)
                    }
                    else // P3->P2가 수평
                    {
                        // P0->P1은 수직이어야 함
                        newElbowPos.x = staticAnchorPoint.x; // X는 P2에 고정
                        newElbowPos.y = newEndPos.y;         // Y는 P0 따름
                    }
                }
                else // Pn 드래그
                {
                    // Pn 드래그. 메모리(P0->P1), 팔꿈치(Pn-1), 고정점(Pn-2)
                    if (isOutermostStaticSegmentVertical) // P0->P1이 수직
                    {
                        // Pn-1->Pn은 수평이어야 함
                        newElbowPos.x = newEndPos.x;         // X는 Pn 따름
                        newElbowPos.y = staticAnchorPoint.y; // Y는 Pn-2에 고정
                    }
                    else // P0->P1이 수평
                    {
                        // Pn-1->Pn은 수직이어야 함
                        newElbowPos.x = staticAnchorPoint.x; // X는 Pn-2에 고정
                        newElbowPos.y = newEndPos.y;         // Y는 Pn 따름
                    }
                }
            }

            // 계산된 새 위치를 경로에 반영
            newPath[elbowIndex] = gridManager.SnapToGrid(newElbowPos);
            newPath[endIndex] = newEndPos;

            // 경로 기억 로직: OptimizePath()를 호출하지 않고 점 개수를 유지
            lr.positionCount = newPath.Count;
            lr.SetPositions(newPath.ToArray());
        }

        // --- 5. 콜라이더 업데이트 ---
        currentWire = lr;
        ColliderSetting();
        currentWire = null;

        // --- ✨ [핵심 수정] 콜라이더 순서 정렬 ---
        Collider2D colliderA = staticPoint.GetComponentInParent<Collider2D>();
        Collider2D colliderB = movedComponent.GetComponent<Collider2D>();

        Collider2D startCollider;
        Collider2D endCollider;

        // isStartMoving 플래그를 사용해 순서를 *항상* (P0, Pn) 순서로 맞춥니다.
        if (isStartMoving)
        {
            startCollider = colliderB; // Moved (P0)
            endCollider = colliderA;   // Static (Pn)
        }
        else
        {
            startCollider = colliderA; // Static (P0)
            endCollider = colliderB;   // Moved (Pn)
        }
    }

    /// <summary>
    /// [수정] 부품이 삭제될 때, 연결된 와이어의 끝점을 가상 접점으로 교체합니다.
    /// </summary>
    public void ReplaceConnectionWithVirtualJunction(Wire wire, ElectricalComponent componentToReplace)
    {
        // ... (1~3. 기존 로직: pointToReplace, staticPoint, junctionLocalPos 찾기) ...
        if (wire == null || componentToReplace == null) return;
        LineRenderer lr = wire.GetComponent<LineRenderer>();
        if (lr == null) return;

        // 1. 교체할 점(pointToReplace)과 유지할 점(staticPoint)을 찾습니다.
        ConnectionPoint pointToReplace = null;
        ConnectionPoint staticPoint = null;
        int pointIndexInWireList = -1; // 와이어 리스트에서의 인덱스 (0 또는 1)

        for (int i = 0; i < wire.connectedPoints.Count; i++)
        {
            if (wire.connectedPoints[i].parentComponent == componentToReplace)
            {
                pointToReplace = wire.connectedPoints[i];
                pointIndexInWireList = i;
            }
            else
            {
                staticPoint = wire.connectedPoints[i];
            }
        }

        // 2. 와이어가 이 부품과 연결된 게 아니거나, 유효하지 않으면 (수리 불가)
        if (pointToReplace == null || staticPoint == null)
        {
            Destroy(wire.gameObject);
            return;
        }

        // 3. 교체할 점의 로컬 좌표를 LineRenderer 경로에서 가져옵니다.
        int pathIndex = (pointIndexInWireList == 0) ? 0 : (lr.positionCount - 1);
        Vector2 junctionLocalPos = lr.GetPosition(pathIndex);


        // 4. 새 가상 접점(Junction)을 생성합니다.
        junctionIndex++;
        var junctionObj = Instantiate(junctionPrefab);
        junctionObj.name = "Junction_Virtual_" + junctionIndex;
        junctionObj.transform.SetParent(content_2D, false);
        junctionObj.GetComponent<RectTransform>().anchoredPosition = junctionLocalPos;

        var junctionComp = junctionObj.AddComponent<Junction>();
        var junctionPoint = junctionObj.AddComponent<ConnectionPoint>();
        junctionPoint.parentComponent = junctionComp;

        // ✨ [핵심 수정] 1번에서 만든 '마커' 컴포넌트를 추가합니다.
        junctionObj.AddComponent<VirtualJunction>();

        // 5. 와이어의 연결 정보를 업데이트합니다. (삭제된 점 -> 새 접점)
        wire.connectedPoints[pointIndexInWireList] = junctionPoint;

        // 6. 회로도 정보(CircuitGraph)를 업데이트합니다.
        CircuitGraph.Instance.RemoveComponent(componentToReplace);
        CircuitGraph.Instance.RegisterConnection(staticPoint.parentComponent, junctionComp);

        // 7. 와이어 이름을 변경하여 상태를 명시합니다.
        wire.name = $"Wire_{staticPoint.parentComponent.name}_to_Junction";
    }

    /// <summary>
    /// 주어진 경로(path)에서 일직선상에 있는 불필요한 중간 점들과
    /// 중복된 점들을 모두 제거합니다. (수정된 최종본)
    /// </summary>
    /// <param name="path">최적화할 점들의 리스트</param>
    /// <returns>최적화된 점들의 리스트</returns>
    private List<Vector3> OptimizePath(List<Vector3> path)
    {
        // 점이 3개 미만이면 최적화할 필요가 없음
        if (path.Count < 3)
        {
            return path;
        }

        List<Vector3> optimizedPath = new List<Vector3>();
        optimizedPath.Add(path[0]); // 첫 번째 점은 항상 포함

        // 1번 인덱스부터 마지막에서 두 번째 점까지 순회 (중간점들만 검사)
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 lastAddedPoint = optimizedPath.Last();
            Vector3 currentPoint = path[i];
            Vector3 nextPoint = path[i + 1];

            if(Vector3.Distance(lastAddedPoint, currentPoint) < 0.001f)
            {
                continue;
            }

            if(Vector3.Distance(currentPoint, nextPoint) < 0.001f)
            {
                continue;
            }

            // 동일 직선 제거
            // dir1: 마지막 추가된 점 -> 현재 점 벡터
            Vector3 dir1 = currentPoint - lastAddedPoint;

            // dir2: 현재 점 -> 다음 점 벡터
            Vector3 dir2 = nextPoint - currentPoint;

            // 두 벡터의 외적 계산
            // 외적의 크기가 0에 가까우면 두 벡터는 동일 직선상에 있음
            float crossMagnitude = Vector3.Cross(dir1, dir2).magnitude;
            
            // 외적의 크기가 0.01f보다 크다면, 두 벡터는 동일 직선상에 없는 것
            if(crossMagnitude > 0.01f)
            {
                optimizedPath.Add(currentPoint);
            }
        }
        if (Vector3.Distance(optimizedPath.Last(), path.Last()) > 0.001f)
        {
            optimizedPath.Add(path.Last());
        }

        int count = optimizedPath.Count;

        if (count >= 3)
        {
            int indexPrev = count - 2;
            int indexPrevPrev = count - 3;

            Vector3 pLast = optimizedPath[count - 1];
            Vector3 pPrev = optimizedPath[indexPrev];
            Vector3 pPrevPrev = optimizedPath[indexPrevPrev];

            bool isVertical = Mathf.Abs(pPrev.x - pPrevPrev.x) < 0.001f;

            bool isHorizontal = Mathf.Abs(pPrev.y - pPrevPrev.y) < 0.001f;

            if(isVertical)
            {
                pPrev.y = pLast.y;
            }
            else if(isHorizontal)
            {
                pPrev.x = pLast.x;
            }

            optimizedPath[indexPrev] = pPrev;
        }

        return optimizedPath;
    }
}