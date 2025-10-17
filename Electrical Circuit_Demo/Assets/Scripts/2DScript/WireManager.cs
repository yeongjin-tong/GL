using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net;

public class WireManager : MonoBehaviour
{
    public static WireManager Instance { get; private set; }

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

    private int junctionIndex = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

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

            if(Input.GetMouseButtonDown(1))
            {
                Destroy(currentWire.gameObject);
                ResetState();
            }
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

            AddPointToWire(currentWire);

            if (junctionPreview != null)
            {
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

        myWire.connectedPoints.Add(endPoint);

        // CircuitGraph에 연결 등록
        CircuitGraph.Instance.RegisterConnection(startPoint.parentComponent, endPoint.parentComponent);

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

    private void AddPointToWire(LineRenderer lr)
    {
        clickIndex = lr.positionCount - 1;
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

        myWire.connectedPoints.Add(firstPoint);
        return lr;
    }


    private void ConnectToExistingWire(LineRenderer drawingWire, Transform targetWireTransform, Vector3 clickPosition)
    {
        Wire targetWire = targetWireTransform.GetComponent<Wire>();
        LineRenderer targetLineRenderer = targetWireTransform.GetComponent<LineRenderer>();

        if (targetWire == null || targetLineRenderer == null) return;

        // 1. 분기점이 될 위치를 계산하고 그리드에 스냅합니다.
        Vector2 localClickPos = WorldToLocal(clickPosition);
        Vector2 snappedJunctionPos = gridManager.SnapToGrid(localClickPos);

        // 2a. 기존 전선의 전체 경로와 끝점 정보를 '복사'해둡니다.
        Vector3[] originalPathPoints = new Vector3[targetLineRenderer.positionCount];
        targetLineRenderer.GetPositions(originalPathPoints);
        ConnectionPoint originalPointA = targetWire.connectedPoints[0];
        ConnectionPoint originalPointB = targetWire.connectedPoints[1];

        // 2b. 경로에서 분기점에 가장 가까운 점의 인덱스를 찾습니다.
        int closestSegmentIndex = -1;
        float minDistance = float.MaxValue;
        for (int i = 0; i < originalPathPoints.Length - 1; i++)
        {
            Vector3 p1 = targetLineRenderer.transform.TransformPoint(originalPathPoints[i]);
            Vector3 p2 = targetLineRenderer.transform.TransformPoint(originalPathPoints[i + 1]);
            Vector3 pointOnSegment = FindNearestPointOnLineSegment(p1, p2, clickPosition);
            float distance = Vector3.Distance(clickPosition, pointOnSegment);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestSegmentIndex = i;
            }
        }

        // 3. 분기점(Junction) 오브젝트를 생성합니다.
        junctionIndex++;
        var junctionObj = junctionPreview;
        junctionObj.name = "Junction_" + junctionIndex;
        junctionObj.transform.SetParent(content_2D, false);
        junctionObj.GetComponent<RectTransform>().anchoredPosition = snappedJunctionPos;
        var junctionComp = junctionObj.AddComponent<Junction>();
        var junctionPoint = junctionObj.AddComponent<ConnectionPoint>();
        junctionPoint.parentComponent = junctionComp;

        // 4. 기존 전선과 임시 전선을 파괴합니다.
        Destroy(targetWire.gameObject);
        Wire drawingWireInfo = drawingWire.GetComponent<Wire>();
        ConnectionPoint newPointC = drawingWireInfo.connectedPoints[0];
        Destroy(drawingWire.gameObject);
        junctionPreview = null;

        // 5. 복사해둔 경로를 분할하여 새 전선 3개를 생성합니다.

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
        // 경로의 마지막 지점(마우스 커서 위치)을 최종 분기점 위치로 교체합니다.
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

    /// <summary>
    /// 지정된 경로(points)를 사용하여 새로운 Wire를 생성하는 헬퍼 함수입니다.
    /// </summary>
    private void CreateWireWithPath(ConnectionPoint startPoint, ConnectionPoint endPoint, List<Vector3> pathPoints)
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
        lr.startColor = lineColor; lr.endColor = lineColor;
        lr.sortingOrder = 1;

        lr.positionCount = pathPoints.Count;
        lr.SetPositions(pathPoints.ToArray());

        // 3. Wire 컴포넌트 설정
        var newWire = wireObject.AddComponent<Wire>();
        newWire.connectedPoints.Add(startPoint);
        newWire.connectedPoints.Add(endPoint);

        // 4. EdgeCollider2D 설정
        var edgeCollider = wireObject.AddComponent<EdgeCollider2D>();
        Vector2[] colliderPoints = pathPoints.Select(p => new Vector2(p.x, p.y)).ToArray();
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
                RedrawWire(wire);
            }
        }
    }

    /// <summary>
    /// 주어진 Wire의 경로를 연결된 포트의 현재 위치에 맞춰 다시 계산하고 그립니다.
    /// (StraightenPath를 사용하도록 수정)
    /// </summary>
    public void RedrawWire(Wire wireToRedraw)
    {
        LineRenderer lr = wireToRedraw.GetComponent<LineRenderer>();
        if (lr == null) return;

        // 1. 현재 LineRenderer의 경로(사용자가 그린 모양)를 그대로 가져옵니다.
        Vector3[] currentPath = new Vector3[lr.positionCount];
        lr.GetPositions(currentPath);

        // 2. 시작점과 끝점의 현재 위치를 반영하여 경로를 업데이트합니다.
        if (wireToRedraw.connectedPoints.Count > 0)
        {
            currentPath[0] = WorldToLocal(wireToRedraw.connectedPoints.First().transform.position);
            currentPath[currentPath.Length - 1] = WorldToLocal(wireToRedraw.connectedPoints.Last().transform.position);
        }

        // 3. StraightenPath 함수로 경로를 보기 좋게 직선화합니다.
        List<Vector3> straightenedPath = StraightenPath(currentPath);

        // 4. 직선화된 새 경로를 LineRenderer와 EdgeCollider에 적용합니다.
        lr.positionCount = straightenedPath.Count;
        lr.SetPositions(straightenedPath.ToArray());

        currentWire = lr;
        ColliderSetting();
        currentWire = null;
    }

    /// <summary>
    /// 사용자가 그린 자유로운 경로를 직각/직선 경로로 변환(직선화)합니다.
    /// </summary>
    /// <param name="originalPath">LineRenderer에서 가져온 원본 경로</param>
    /// <returns>직선화된 새로운 경로</returns>
    private List<Vector3> StraightenPath(Vector3[] originalPath)
    {
        if (originalPath == null || originalPath.Length < 2)
        {
            return originalPath?.ToList() ?? new List<Vector3>();
        }

        List<Vector3> newPath = new List<Vector3>();
        newPath.Add(originalPath[0]); // 시작점은 그대로 추가

        // 경로의 각 점들을 순회하며 코너점 생성
        for (int i = 0; i < originalPath.Length - 1; i++)
        {
            Vector3 currentPoint = newPath.Last(); // 새 경로의 마지막 점
            Vector3 nextPoint = originalPath[i + 1];    // 원본 경로의 다음 목표점

            // 현재 점과 목표점의 x, y 차이 계산
            float deltaX = Mathf.Abs(currentPoint.x - nextPoint.x);
            float deltaY = Mathf.Abs(currentPoint.y - nextPoint.y);

            // 이전 경로의 방향을 확인하여 꺾이는 방향 결정 (옵션)
            Vector3 lastSegmentDir = (newPath.Count > 1) ? (newPath.Last() - newPath[newPath.Count - 2]).normalized : Vector3.zero;

            // 기본적으로 수평/수직 중 더 많이 움직인 쪽으로 먼저 이동
            // (더 자연스러운 경로를 위해 이전 경로 방향을 고려)
            if ((deltaX > deltaY && lastSegmentDir.y == 0) || (deltaY <= deltaX && lastSegmentDir.x != 0)) // 수평으로 먼저 이동
            {
                if (Mathf.Abs(currentPoint.x - nextPoint.x) > 0.01f)
                    newPath.Add(new Vector3(nextPoint.x, currentPoint.y, 0));
            }
            else // 수직으로 먼저 이동
            {
                if (Mathf.Abs(currentPoint.y - nextPoint.y) > 0.01f)
                    newPath.Add(new Vector3(currentPoint.x, nextPoint.y, 0));
            }

            newPath.Add(nextPoint); // 최종 목표점 추가
        }

        // 생성된 경로에서 불필요한 점들(일직선 위의 점)을 제거하여 최적화
        return OptimizePath(newPath);
    }

    /// <summary>
    /// 주어진 경로(path)에서 일직선상에 있는 불필요한 중간 점들을 제거합니다.
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
            // 이전 점에서 현재 점으로의 방향 벡터
            Vector3 prevDir = (path[i] - path[i - 1]).normalized;
            // 현재 점에서 다음 점으로의 방향 벡터
            Vector3 nextDir = (path[i + 1] - path[i]).normalized;

            // 두 방향 벡터의 거리를 계산하여 방향이 다른지(꺾이는 지점인지) 확인
            // 거리가 0에 가까우면 같은 방향(일직선)을 의미
            if (Vector3.Distance(prevDir, nextDir) > 0.01f)
            {
                // 방향이 다를 경우, 현재 점은 꺾이는 지점이므로 경로에 추가
                optimizedPath.Add(path[i]);
            }
        }

        optimizedPath.Add(path.Last()); // 마지막 점은 항상 포함

        return optimizedPath;
    }
}