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

    // PowerOn �Լ��� �������Ͽ� ���� �Ѵ� Ư���� �ൿ �߰�
    public override void PowerOn()
    {
        base.PowerOn(); // �θ��� PowerOn ����(isPowered=true ��)�� �״�� ����
        img.color = onColor;
    }

    // PowerOff �Լ��� �������Ͽ� ���� ���� Ư���� �ൿ �߰�
    public override void PowerOff()
    {
        base.PowerOff(); // �θ��� PowerOff ������ �״�� ����
        img.color = offColor;
    }
}