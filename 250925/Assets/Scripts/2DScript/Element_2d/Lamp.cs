using UnityEngine;
using UnityEngine.UI;

public class Lamp : ElectricalComponent
{
    private Image img;
    public Color offColor = Color.white;
    public Color onColor = Color.yellow;

    void Awake()
    {
        img = GetComponent<Image>();
        img.color = offColor;
    }

    // PowerOn 함수를 재정의하여 빛을 켜는 특별한 행동 추가
    public override void PowerOn()
    {
        base.PowerOn(); // 부모의 PowerOn 로직(isPowered=true 등)은 그대로 실행
        img.color = onColor;
    }

    // PowerOff 함수를 재정의하여 빛을 끄는 특별한 행동 추가
    public override void PowerOff()
    {
        base.PowerOff(); // 부모의 PowerOff 로직은 그대로 실행
        img.color = offColor;
    }
}