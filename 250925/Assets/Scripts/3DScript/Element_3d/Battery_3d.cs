using UnityEngine;

[RequireComponent(typeof(Battery))]
public class Battery3D_Controller : MonoBehaviour
{
    private Battery selfBattery;
    public Transform powerSwitch;

    void Awake()
    {
        selfBattery = GetComponent<Battery>();
        if (selfBattery != null)
        {
            selfBattery.OnStateChanged += UpdateVisuals;
        }
    }

    void Start()
    {
        UpdateVisuals(selfBattery.isOn);
    }

    private void OnMouseDown()
    { 
        selfBattery.ToggleEvent(); 
    }

    private void UpdateVisuals(bool isOn)
    {

        if (isOn)
        {
            powerSwitch.rotation = Quaternion.Euler(0f, 90f, -55f);
            Debug.Log("on!!!");
        }
        else
        {
            powerSwitch.rotation = Quaternion.Euler(0f, 90f, -125f);
            Debug.Log("off..");
        }
    }

    private void OnDestroy()
    {
        if (selfBattery != null)
            selfBattery.OnStateChanged -= UpdateVisuals;
    }
}