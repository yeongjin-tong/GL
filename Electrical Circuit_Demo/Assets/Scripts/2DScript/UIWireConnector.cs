using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWireConnector : MonoBehaviour
{
    private GraphicRaycaster gr;
    private EventSystem es;

    private Transform firstPoint;
    private Transform secondPoint;

    void Awake()
    {
        gr = GetComponent<GraphicRaycaster>();
        es = EventSystem.current; // 반드시 씬에 EventSystem 있어야 함
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnUIObjectClicked += HandleUIClick;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnUIObjectClicked -= HandleUIClick;
    }

    // ✨ UI 클릭 방송을 수신하여 처리
    private void HandleUIClick(GameObject clickedUI)
    {
        if (clickedUI != null)
        {
            if (firstPoint == null)
            {
                firstPoint = clickedUI.transform;
            }
            else if (secondPoint == null && clickedUI.transform != firstPoint)
            {
                secondPoint = clickedUI.transform;
                ConnectWire(firstPoint, secondPoint);
                firstPoint = null;
                secondPoint = null;
            }
        }
    }

    private void ConnectWire(Transform a, Transform b)
    {
        // 1) 전선 오브젝트 만들기
        GameObject wire = new GameObject("Wire");
        wire.transform.SetParent(this.transform); // Canvas 밑에 생성

        LineRenderer lr = wire.AddComponent<LineRenderer>();
        lr.positionCount = 2;

        // 2) UI 좌표를 월드 좌표로 변환해서 연결
        Vector3 posA = a.position;
        Vector3 posB = b.position;

        lr.SetPosition(0, posA);
        lr.SetPosition(1, posB);

        // 3) 선 스타일 설정
        lr.startWidth = 3f;  // UI니까 좀 크게
        lr.endWidth = 3f;
        // 조명 없이도 잘 보이는 파티클 셰이더를 사용합니다.
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.sortingOrder = 1;
    }
}
