using UnityEngine;
using UnityEngine.UI;

public class Lamp : ElectricalComponent
{
    private Image mySprite;
    public Sprite[] state;

    void Awake()
    {
        mySprite = GetComponent<Image>();
    }

    // PowerOn �Լ��� �������Ͽ� ���� �Ѵ� Ư���� �ൿ �߰�
    public override void PowerOn()
    {
        base.PowerOn(); // �θ��� PowerOn ����(isPowered=true ��)�� �״�� ����

        if(state != null)
        {
            mySprite.sprite = state[1];
        }
        
    }

    // PowerOff �Լ��� �������Ͽ� ���� ���� Ư���� �ൿ �߰�
    public override void PowerOff()
    {
        base.PowerOff(); // �θ��� PowerOff ������ �״�� ����
        
        if (state != null)
        {
            mySprite.sprite = state[0];
        }
    }
}