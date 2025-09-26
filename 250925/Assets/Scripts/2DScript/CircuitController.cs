using System;
using System.Collections.Generic;
using NiceIO.Sysroot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CircuitController : MonoBehaviour
    , IBeginDragHandler
    , IEndDragHandler
    , IDragHandler
    , IPointerEnterHandler
    , IPointerExitHandler
{

    private GameObject clone;
    private RectTransform canvasRect;
    private RectTransform cloneRect;

    public RectTransform deleteZone;

    // ✨ GridManager에 접근하기 위한 참조 변수
    private GridManager gridManager;

    void Start()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        if(transform.parent.GetComponent<RectTransform>()!= null)
        {
            deleteZone = transform.parent.GetComponent<RectTransform>();
        }

        // ✨ 씬에 있는 GridManager 인스턴스를 찾아 할당
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("씬에 GridManager가 존재하지 않습니다!");
        }
    }


    public void OnBeginDrag(PointerEventData eventData)   // 드래그 시작
    {
        if (clone == null)
        {
            Debug.Log("생성될 오브젝트 이름: " + gameObject);
            clone = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
            clone.AddComponent<ObjectConnect>();
            clone.name = gameObject.name + "_Clone";
            cloneRect = clone.GetComponent<RectTransform>();
            cloneRect.localScale = GetComponent<RectTransform>().localScale;
            
            Outline line = clone.AddComponent<Outline>();
            line.effectColor = Color.yellow;
            line.effectDistance = new Vector2(3f, -3f);
            line.enabled = false;

            Image img = clone.GetComponent<Image>();
            img.raycastTarget = false;
            Destroy(clone.GetComponent<CircuitController>());

            ObjectManager.Instance.objects_2d.Add(clone);
        }
    }

    public void OnDrag(PointerEventData eventData)  // 드래그 중
    {
        if (clone != null && gridManager != null)
        {
            // ✨ --- 그리드 스냅 로직 시작 ---

            // 1. 현재 마우스 위치를 월드 좌표로 변환
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData);

            // 2. 월드 좌표를 그리드 좌표로 변환
            Vector2Int gridPos = gridManager.GetGridPosition(mouseWorldPos);

            // 3. 그리드 좌표를 다시 월드 좌표(칸의 중앙)로 변환
            Vector3 snappedWorldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);

            // 4. 클론의 위치를 스냅된 월드 좌표로 설정
            clone.transform.position = snappedWorldPos;

            // ✨ --- 그리드 스냅 로직 끝 ---
        }
    }

    public void OnEndDrag(PointerEventData eventData)   // 드래그 종료
    {
        if (clone != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(deleteZone, eventData.position, eventData.pressEventCamera))
            {
                Destroy(clone);
            }
            clone.GetComponent<Image>().color = Color.white;
            clone = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().color = Color.green;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().color = Color.white;
    }

    private Vector3 GetMouseWorldPosition(PointerEventData eventData)
    {
        // 1. 카메라에서 마우스 위치로 향하는 광선(Ray)을 생성합니다.
        Ray ray = eventData.pressEventCamera.ScreenPointToRay(eventData.position);

        // 2. 캔버스가 있는 위치를 기준으로, 카메라를 정면으로 바라보는 가상의 평면을 만듭니다.
        //    transform.forward는 Z축 방향을 의미하며, 카메라의 -transform.forward는 카메라가 바라보는 방향입니다.
        Plane plane = new Plane(-eventData.pressEventCamera.transform.forward, canvasRect.position);

        // 3. 1번에서 쏜 광선이 2번에서 만든 평면과 만나는 지점의 거리를 계산합니다.
        if (plane.Raycast(ray, out float distance))
        {
            // 4. 광선이 그 거리만큼 나아갔을 때의 정확한 3D 월드 좌표를 반환합니다.
            return ray.GetPoint(distance);
        }

        // 만약 평면과 만나지 않는 예외적인 경우, 기존의 Z값을 이용한 계산을 수행 (안전장치)
        Vector3 screenPoint = eventData.position;
        screenPoint.z = canvasRect.position.z - eventData.pressEventCamera.transform.position.z;
        return eventData.pressEventCamera.ScreenToWorldPoint(screenPoint);
    }
}