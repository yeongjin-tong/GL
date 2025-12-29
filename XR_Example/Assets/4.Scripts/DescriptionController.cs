using UnityEngine;
using TMPro;

public class DiscriptionController : MonoBehaviour
{
    [Header("연동할 TMP_Text")]
    public TMP_Text targetText;

    [Header("각각 단계별 표시할 문자열들")]
    public string[] mainStepTitles;

    private int lastIndex = -1;

    void Update()
    {
        if (AssemblyManager.instance == null) return;

        int currentIndex = AssemblyManager.instance.currentMainIndex;

        // 인덱스가 변경되었을 때만 업데이트
        if (currentIndex != lastIndex)
        {
            UpdateText(currentIndex);
            lastIndex = currentIndex;
        }
    }

    void UpdateText(int index)
    {
        // 배열 범위 체크
        if (index < 0 || index >= mainStepTitles.Length)
        {
            targetText.text = "";
            Debug.LogWarning("MainStepTextBinder: index out of range!");
            return;
        }

        targetText.text = mainStepTitles[index];
    }
}
