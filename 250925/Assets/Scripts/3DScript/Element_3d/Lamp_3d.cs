using UnityEngine;
using UnityEngine.UI;

public class Lamp_3d : ElectricalComponent
{
    public GameObject lamp;
    public Material[] lampColor;
    private MeshRenderer myLight;

    private void Awake()
    {
        myLight = lamp.GetComponent<MeshRenderer>();
        myLight.material = lampColor[0];
    }

    // PowerOff 함수를 재정의하여 빛을 끄는 특별한 행동 추가
    public override void PowerOff()
    {
        myLight.material = lampColor[0];
        base.PowerOff(); // 부모의 PowerOff 로직은 그대로 실행
    }

    // PowerOn 함수를 재정의하여 빛을 켜는 특별한 행동 추가
    public override void PowerOn()
    {
        myLight.material = lampColor[1];
    }

   
}