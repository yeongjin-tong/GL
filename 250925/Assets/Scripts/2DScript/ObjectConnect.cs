using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ObjectConnect : MonoBehaviour
{
    public Transform[] connectPoints = new Transform[2]; // 0:상, 1:하

    void Awake()
    {
        if (transform.GetComponent<BoxCollider>() != null)
        {
            return;
        }
            // 연결 포인트 생성 및 위치 지정

        //CreateConnectPoint(0, new Vector2(0, 25f)); // 상
        //CreateConnectPoint(1, new Vector2(0, -25f)); // 하
    }

    void CreateConnectPoint(int idx, Vector2 localPos)
    {
        GameObject point = new GameObject("ConnectPoint_" + idx);
        var image = point.AddComponent<Image>();
        image.raycastTarget = false;
        point.transform.SetParent(this.transform);
        point.transform.localPosition = localPos;
        var collider = point.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(10f, 10f); // 작은 크기
        connectPoints[idx] = point.transform;
        RectTransform rect = point.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10f, 10f);
        point.transform.localScale = new Vector3(1f, 1f, 1f);
        point.AddComponent<ConnectionPoint>();  // ConnectionPoint 스크립트 추가
        
        switch(idx)
        {
            case 0:
                point.GetComponent<ConnectionPoint>().pointDirection = ConnectionPoint.Direction.Up;
                break;
            case 1:
                point.GetComponent<ConnectionPoint>().pointDirection = ConnectionPoint.Direction.Down;
                break;
        }

    }
}
