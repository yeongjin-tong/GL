using System.Collections.Generic;
using UnityEngine;

public class WireManager_3d : MonoBehaviour
{
    public static WireManager_3d Instance { get; private set; } // 싱글톤 인스턴스
    [Tooltip("Wire생성 위치")]
    public Transform content_3D;
    public Color portHighlightColor = Color.green; // 하이라이트 색상
    [Tooltip("전선의 색상")]
    public Color wireColor = Color.blue;
    [Tooltip("전선의 두께")]
    public float wireWidth = 0.1f;
    [Tooltip("스냅 계수")]
    public float snapRadius = 0.3f;

    public List<LineRenderer> wireList = new List<LineRenderer>();

    // ✨ 전선 그리기 모드인지 확인하는 플래그
    private bool isDrawingWire = false;
    private LineRenderer currentWire;
    private ElectricalComponent firstComponent;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (SimulationManager.isSimulating) return;

        // --- 마우스 왼쪽 버튼 클릭 처리 ---
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 만약 선을 그리고 있고, 현재 스냅된 포트가 있다면
            //    (FindClosestPort가 찾은 포트는 UpdateWirePreview에서 사용되지만, 여기서 직접 접근하기 위해 한 번 더 찾아줍니다.)
            Vector3? currentMousePos = GetMouseWorldPositionOnPlane(isDrawingWire ? currentWire.GetPosition(currentWire.positionCount - 2) : Vector3.zero);
            Transform snappedPort = isDrawingWire ? FindClosestPort(currentMousePos.Value, snapRadius) : null;

            if (isDrawingWire && snappedPort != null)
            {
                // 2. Raycast를 쏘기 전에, 스냅된 포트에 바로 연결하고 로직을 종료
                ElectricalComponent secondComponent = snappedPort.GetComponentInParent<ElectricalComponent>();
                if (secondComponent != firstComponent)
                {
                    // ✨ 스냅된 포트의 콜라이더 중심을 최종 위치로 사용
                    FinalizeWire(currentWire, snappedPort.GetComponent<Collider>().bounds.center, firstComponent, secondComponent);
                    ResetState();
                }
                return; // ✨ 중요: 아래의 Raycast 로직을 실행하지 않도록 여기서 종료
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // ✨ 1. 그리기 모드가 아닐 때 (첫 클릭)
                if (!isDrawingWire)
                {
                    if (hit.collider.CompareTag("ConnectionPort"))
                    {
                        // 그리기 시작
                        isDrawingWire = true;
                        firstComponent = hit.collider.GetComponentInParent<ElectricalComponent>();
                        currentWire = CreateWire(hit.point);
                    }
                }
                // ✨ 2. 그리기 모드일 때 (마지막 클릭)
                else
                {
                    if (hit.collider.CompareTag("ConnectionPort"))
                    {
                        ElectricalComponent secondComponent = hit.collider.GetComponentInParent<ElectricalComponent>();
                        // 자기 자신에게 연결하는 것을 방지
                        if (secondComponent != firstComponent)
                        {
                            FinalizeWire(currentWire, hit.point, firstComponent, secondComponent);
                            ResetState(); // 그리기 완료 후 상태 초기화
                        }
                    }
                    // ✨ 3. 그리기 모드 중 허공 대신 다른 포트를 클릭했을 때 경유지 추가
                    else
                    {
                        AddPointToWire(currentWire, hit.point);
                    }
                }
            }
            // ✨ 4. 그리기 모드 중 허공을 클릭했을 때 경유지 추가
            else if (isDrawingWire)
            {
                Vector3? mouseWorldPos = GetMouseWorldPositionOnPlane(currentWire.GetPosition(currentWire.positionCount - 2));
                if (mouseWorldPos.HasValue)
                {
                    AddPointToWire(currentWire, mouseWorldPos.Value);
                }
            }
        }

        // --- 실시간 미리보기 업데이트 ---
        if (isDrawingWire && currentWire != null)
        {
            UpdateWirePreview(currentWire);
        }

        // ✨ 마우스 오른쪽 버튼으로 그리기 취소
        if (isDrawingWire && Input.GetMouseButtonDown(1))
        {
            Destroy(currentWire.gameObject);
            ResetState();
        }
    }

    // 전선 그리기를 시작하는 함수
    private LineRenderer CreateWire(Vector3 startPos)
    {
        GameObject wireObject = new GameObject("Wire_Drawing");
        wireObject.transform.parent = content_3D;
        LineRenderer lr = wireObject.AddComponent<LineRenderer>();

        // ✨ 점 2개로 시작 (시작점, 마우스를 따라다닐 끝점)
        lr.positionCount = 2;
        lr.SetPosition(0, startPos);

        SetupLineRenderer(lr);
        return lr;
    }

    // ✨ 전선에 경유지를 추가하는 함수
    private void AddPointToWire(LineRenderer lr, Vector3 newPoint)
    {
        lr.positionCount++; // 점 개수를 하나 늘림
        lr.SetPosition(lr.positionCount - 1, newPoint); // 새로 생긴 마지막 점의 위치를 지정
    }

    // 실시간 미리보기를 업데이트하는 함수

    private void UpdateWirePreview(LineRenderer lr)
    {
        Vector3? mouseWorldPos = GetMouseWorldPositionOnPlane(lr.GetPosition(lr.positionCount - 2));
        if (!mouseWorldPos.HasValue) return;

        Transform closestPortTransform = FindClosestPort(mouseWorldPos.Value, snapRadius);

        Vector3 endPoint;
        if (closestPortTransform != null)
        {
            // ✨ 포트의 콜라이더 경계(bounds)의 중심점을 사용
            Collider portCollider = closestPortTransform.GetComponent<Collider>();
            if (portCollider != null)
            {
                endPoint = portCollider.bounds.center;
            }
            else
            {
                endPoint = closestPortTransform.position;
            }
        }
        else
        {
            endPoint = mouseWorldPos.Value;
        }

        lr.SetPosition(lr.positionCount - 1, endPoint);
    }

    // WireManager_3d.cs 스크립트 내부에 아래 함수 추가

    // 특정 위치 주변에서 가장 가까운 ConnectionPort를 찾는 함수
    private Transform FindClosestPort(Vector3 mousePos, float radius)
    {
        // 1. 지정된 반경 안의 모든 콜라이더를 감지
        Collider[] hitColliders = Physics.OverlapSphere(mousePos, radius);
        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            // 2. 감지된 콜라이더가 ConnectionPort 태그를 가지고 있는지 확인
            if (hitCollider.CompareTag("ConnectionPort"))
            {
                // 3. 시작점과 다른 포트인지 확인 (자기 자신에게 스냅되는 것 방지)
                ElectricalComponent portComponent = hitCollider.GetComponentInParent<ElectricalComponent>();
                if (portComponent != null && portComponent != firstComponent)
                {
                    // 4. 거리를 계산하여 가장 가까운 포트를 찾음
                    float distance = Vector3.Distance(mousePos, hitCollider.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = hitCollider.transform;
                    }
                }
            }
        }
        return closest;
    }

    // 전선 연결을 완료하는 함수
    private void FinalizeWire(LineRenderer lr, Vector3 endPos, ElectricalComponent comp1, ElectricalComponent comp2)
    {
        // 마지막 점을 최종 도착지 위치로 고정
        lr.SetPosition(lr.positionCount - 1, endPos);
        lr.name = $"Wire_{comp1.name}_to_{comp2.name}";

        if (comp1 != null && comp2 != null)
        {
            comp1.AddConnection(comp2);
            comp2.AddConnection(comp1);
        }
        wireList.Add(lr);
    }

    private void ResetState()
    {
        isDrawingWire = false;
        currentWire = null;
        firstComponent = null;
    }

    // 마우스 위치를 3D 평면에 계산하는 함수
    private Vector3? GetMouseWorldPositionOnPlane(Vector3 referencePoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, referencePoint);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return null;
    }

    // LineRenderer의 공통 스타일을 설정하는 함수
    private void SetupLineRenderer(LineRenderer lr)
    {
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = wireColor;
    }

    public void HighlightPort(ConnectionPoint_3D port, bool highlight, Color myColor)
    {
        if (port == null) return;

        // 포트 오브젝트에 있는 MeshRenderer의 색상을 변경
        var renderer = port.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                renderer.material.color = portHighlightColor;
            }
            else
            {
                renderer.material.color = myColor; // 원래 색상으로 복구
            }
        }
    }
}