// UIRelayContactVisual.cs (새 스크립트 파일)
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TimerSwitch))]
public class TimerSwitch_2d : MonoBehaviour
{
    public Sprite[] state; // 0: Off (Open), 1: On (Closed)
    private TimerSwitch selfContact;
    private Image image;

    void Awake()
    {
        selfContact = GetComponent<TimerSwitch>();
        image = GetComponent<Image>();
        if (selfContact != null)
        {
            selfContact.OnStateChanged += UpdateVisual;
        }
    }
    void Start() { UpdateVisual(selfContact.isOn); }

    void OnDestroy() { if (selfContact != null) selfContact.OnStateChanged -= UpdateVisual; }

    private void UpdateVisual(bool isOn)
    {
        if (state == null || state.Length < 2) return;
        image.sprite = isOn ? state[1] : state[0];
    }
}