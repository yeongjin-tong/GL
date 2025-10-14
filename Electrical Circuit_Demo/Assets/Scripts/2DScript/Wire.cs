using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wire : MonoBehaviour
{
    public ConnectionPoint firstPoint;
    public ConnectionPoint lastPoint;

    public ElectricalComponent componentA;
    public ElectricalComponent componentB;

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

    // ✨ 외부에서 직접 색상을 설정하는 함수
    public void SetColor(Color newColor)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }

    // ✨ 원래 색상으로 되돌리는 함수
    public void ResetColor()
    {
        if (lineRenderer != null)
        {
            SetColor(defaultColor);
        }
    }
}
