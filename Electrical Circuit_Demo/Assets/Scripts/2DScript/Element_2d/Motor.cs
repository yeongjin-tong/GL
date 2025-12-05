using UnityEngine;
using UnityEngine.UI;

public class Motor : ElectricalComponent
{
    [Header("Visuals")]
    public RectTransform rotatingPart; // 회전할 이미지 (모터 내부 원판 등)
    public float rotationSpeed = 300f; // 회전 속도

    private bool isRunning = false;

    

    void Update()
    {
        // 전원이 켜져있으면 이미지 회전
        if (isRunning && rotatingPart != null)
        {
            rotatingPart.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
    public override void Awake()
    {
        base.Awake();
        rotatingPart.gameObject.SetActive(false);
    }

    public override void PowerOn()
    {
        base.PowerOn();
        rotatingPart.gameObject.SetActive(true);
        isRunning = true;
        // (필요 시 소리 재생 등 추가)
    }

    public override void PowerOff()
    {
        base.PowerOff();
        rotatingPart.gameObject.SetActive(false);
        isRunning = false;
    }
}