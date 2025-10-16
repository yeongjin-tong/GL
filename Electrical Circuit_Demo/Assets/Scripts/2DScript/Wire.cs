// Wire.cs (최종 수정안)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Wire : MonoBehaviour
{
    // 이 전선(네트워크)에 연결된 모든 연결점의 목록이 유일한 데이터 소스입니다.
    public List<ConnectionPoint> connectedPoints = new List<ConnectionPoint>();

    // 연결된 부품 목록을 쉽게 가져오기 위한 프로퍼티
    public List<ElectricalComponent> ConnectedComponents
    {
        get
        {
            // 중복을 제거하고 부모 컴포넌트 리스트를 반환합니다.
            return connectedPoints.Select(p => p.parentComponent).Distinct().ToList();
        }
    }

    private LineRenderer lineRenderer;
    private Color defaultColor;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            defaultColor = lineRenderer.startColor;
        }
    }

    // 색상 관련 함수는 그대로 유지
    public void SetColor(Color newColor)
    {
        // TODO: 이 Wire에 속한 모든 시각적 선(자식 LineRenderer 포함)의 색을 변경해야 함
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }

    public void ResetColor()
    {
        if (lineRenderer != null)
        {
            SetColor(defaultColor);
        }
    }
}