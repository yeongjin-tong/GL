using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

public class XRButtonManager : MonoBehaviour
{
    public Button setBtn;
    public GameObject settingScreen;
    public SelectorUnityEventWrapper dd;

    private void Awake()
    {
        setBtn.onClick.AddListener(ActiveScreen);
        //dd.WhenUnselected.AddListener(ActiveScreen);
    }

    public void ActiveScreen()
    {
        if(settingScreen.activeSelf)
        {
            settingScreen.SetActive(false);
        }
        else
        {
            settingScreen.SetActive(true);
        }
    }
}
