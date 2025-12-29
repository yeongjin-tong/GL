using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoticeUI : MonoBehaviour
{
    [Header("SubNotice")]
    public GameObject subbox;
    public TextMeshProUGUI subintext;
    public Animator subani;

    // 코루틴 최적화
    private WaitForSeconds _UIDelay_1 = new WaitForSeconds(1.0f);
    private WaitForSeconds _UIDelay_2 = new WaitForSeconds(0.3f);

    private void Start()
    {
        subbox.SetActive(false);
    }

    // 짧은 메시지 >> string 값을 매개 변수로 받아와서 2초간 표시
    // 사용 : _notice.SUB("문자열");
    public void SUB(string message)
    {
        subintext.text = message;
        subbox.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(SUBDelay());
    }

    IEnumerator SUBDelay()
    {
        subbox.SetActive(true);
        subani.SetBool("isOn", true);
        yield return _UIDelay_1;
        subani.SetBool("isOn", false);
        yield return _UIDelay_2;
        subbox.SetActive(false);
    }
}
