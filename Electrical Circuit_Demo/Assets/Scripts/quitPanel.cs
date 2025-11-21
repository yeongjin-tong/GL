using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class quitPanel : MonoBehaviour
{
    public Button confirmBtn;
    public Button cancelBtn;

    private void Awake()
    {
        confirmBtn.onClick.AddListener(ConfirmEvent);
        cancelBtn.onClick.AddListener(CancelEvent);
    }

    private void ConfirmEvent()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CancelEvent()
    {
        Destroy(gameObject);
    }
}
