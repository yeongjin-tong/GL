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

    // PowerOff �Լ��� �������Ͽ� ���� ���� Ư���� �ൿ �߰�
    public override void PowerOff()
    {
        myLight.material = lampColor[0];
        base.PowerOff(); // �θ��� PowerOff ������ �״�� ����
    }

    // PowerOn �Լ��� �������Ͽ� ���� �Ѵ� Ư���� �ൿ �߰�
    public override void PowerOn()
    {
        myLight.material = lampColor[1];
    }

   
}