using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // ✨ 이벤트(방송) 정의
    // 1. 2D 물리 오브젝트(부품, 전선) 클릭 방송
    public event Action<Collider2D> OnPhysicsObjectClicked;
    // 2. UI 오브젝트 클릭 방송
    public event Action<GameObject> OnUIObjectClicked;
    // 3. Delete 키 입력 방송
    public event Action OnDeleteKeyPressed;

    public event Action<GameObject> OnDoubleClick;
    

    // 인스펙터에서 Canvas의 GraphicRaycaster를 연결해줘야 함
    [SerializeField] private GraphicRaycaster uiRaycaster;
    private EventSystem eventSystem;

    private float interval = 0.25f;
    private float clickedTime;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        eventSystem = EventSystem.current;
    }

    void Update()
    {
        Vector3 mouse = GetMouseWorldPosition();

        // 시뮬레이션 모드가 아닐 때만 (편집 모드일 때만) 아래의 입력 감지를 실행합니다.
        if (!SimulationManager.isSimulating && PopupManager.Instance.currentPopup == null)
        {
            // --- 마우스 왼쪽 클릭 감지 ---
            if (Input.GetMouseButtonDown(0))
            {
                // 더블클릭 시!
                if (Time.time - clickedTime < interval)
                {
                    Vector3 mouseWorld = GetMouseWorldPosition();
                    Collider2D hit = Physics2D.OverlapCircle(mouseWorld, 0.01f);
                    if (hit != null)
                    {
                        // 2D_Space 아래에 있는 오브젝트만 더블클릭 기능 제공
                        Transform currentParent = hit.transform.parent;
                        while (currentParent != null)
                        {
                            if (currentParent.name.Contains("2D_Space"))
                            {
                                PopupManager.Instance.CreatePopup(hit.gameObject);
                                //OnDoubleClick?.Invoke(hit.gameObject);
                                return;
                            }

                            currentParent = currentParent.parent;
                        }
                    }
                }
                else
                {
                    clickedTime = Time.time;
                }

                // UI 클릭을 최우선으로 확인
                if (IsPointerOverUIObject(out GameObject clickedUI))
                {
                    OnUIObjectClicked?.Invoke(clickedUI);
                    Debug.Log("UI 이름: " + clickedUI.name);
                }
                // UI 클릭이 아니면, 2D 물리 오브젝트 확인
                else
                {
                    Vector3 mouseWorld = GetMouseWorldPosition();

                    Collider2D hit = SelectPriorityObject();

                    //Collider2D hit = Physics2D.OverlapCircle(mouseWorld, 0.01f);
                    
                    OnPhysicsObjectClicked?.Invoke(hit);

                    if (hit != null)
                    {
                        Debug.Log("오브젝트 이름: " + hit.name);
                    }
                }
            }

            // --- Delete 키 감지 ---
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                OnDeleteKeyPressed?.Invoke();
            }
        }
    }

    private Collider2D SelectPriorityObject()
    {
        Vector3 mousePos = GetMouseWorldPosition();

        // 클릭 위치의 모든 콜라이더를 배열로 가져옴
        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, 0.01f);

        Collider2D selectedObj = null;
        int highestPriority = -1;

        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            int currentPriority = 0;

            // 태그나 컴포넌트를 이용해 우선순위 점수 부여
            if (obj.GetComponent<ConnectionPoint>() != null) currentPriority = 100;      // 포트 (가장 높음)
            else if (obj.GetComponent<ElectricalComponent>() != null) currentPriority = 50;    // 부품

            // 현재까지 찾은 것보다 우선순위가 높으면 교체
            if (currentPriority > highestPriority)
            {
                highestPriority = currentPriority;
                selectedObj = hit;
            }
        }
        if (selectedObj != null)
        {
            Debug.Log("선택된 최우선 객체: " + selectedObj.name);
            // 여기서 선택 로직 수행
        }
        return selectedObj;
    }

    // 2D 물리 오브젝트를 찾는 헬퍼 함수 (GameObject를 반환하도록 수정)
    private bool IsPointerOverPhysicsObject(out GameObject clickedObject)
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        Collider2D hit = Physics2D.OverlapCircle(mouseWorld, 0.01f);
        if (hit != null)
        {
            clickedObject = hit.gameObject;
            return true;
        }
        clickedObject = null;
        return false;
    }

    // 마우스 포인터 아래에 UI가 있는지 확인하는 함수
    private bool IsPointerOverUIObject(out GameObject clickedObject)
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(eventData, results);

        if (results.Count > 0)
        {
            clickedObject = results[0].gameObject;
            return true;
        }
        clickedObject = null;
        return false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}