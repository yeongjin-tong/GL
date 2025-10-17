using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [Header("메인 화면")]
    public Button selectBtn_2d;
    public Button selectBtn_3d;

    public GameObject content_3d;
    public GameObject modeScreen;

    [Header("2D 화면")]
    public Button initBtn;
    public Button playBtn;
    public Button pauseBtn;
    public Button stopBtn;
    public Button helpBtn;
    public Button helpCloseBtn;
    public Button backBtn_2d;
    public Button pinNum;


    private bool pinisOn;
    public Transform space_2d;
    public GameObject helpPanel;
    [Header("3D 화면")]
    public Button window_2dBtn;
    public Button backBtn_3d;
    public Button initBtn_3d;

    private RectTransform panel_2D;



    private void Awake()
    {
        initBtn.onClick.AddListener(ObjectInit_2d);
        selectBtn_3d.onClick.AddListener(() => ModeSelect(1) );
        selectBtn_2d.onClick.AddListener(() => ModeSelect(2) );
        window_2dBtn.onClick.AddListener(WindowScreen_2d);
        backBtn_3d.onClick.AddListener(() => ModeSelect(0));
        backBtn_2d.onClick.AddListener(() => ModeSelect(0));
        initBtn_3d.onClick.AddListener(ObjectInit);
        pinNum.onClick.AddListener(FindPinNumber);
        helpBtn.onClick.AddListener(() =>
        {
            helpPanel.SetActive(true);
        });
        helpCloseBtn.onClick.AddListener(() =>
        {
            helpPanel.SetActive(false);
        });

        SimulationBtnAdd();
    }

    private void Start()
    {
        ModeSelect(0);
        pinisOn = false;
    }

    private void ObjectInit_2d()
    {
        
        foreach(Transform chird in space_2d)
        {
            Destroy(chird.gameObject);
        }
    }


    private void ObjectInit()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if(obj.name.Contains("Clone"))
            {
                Destroy(obj);
            }
        }
    }

    private void SimulationBtnAdd()
    {
        stopBtn.onClick.AddListener(() =>
        {
            ModeBtnEvent(0);
        });
        playBtn.onClick.AddListener(() =>
        {
            ModeBtnEvent(1);
        });
        pauseBtn.onClick.AddListener(() =>
        {
            ModeBtnEvent(2);
        });
    }

    private void ModeBtnEvent(int num)
    {
        SimulationManager.ToggleSimulation(num);
        bool sim = SimulationManager.isSimulating;
        
        if (sim == false)
        {
            playBtn.interactable = true;
            pauseBtn.interactable = false;
            stopBtn.interactable = false;
        }
        else
        {
            playBtn.interactable = false;
            pauseBtn.interactable = true;
            stopBtn.interactable = true;
        }

    }

    private void ModeSelect(int index)                          // 0 : 메인 화면        1 : 3d 화면       2 : 2d 화면
    {
        content_3d.SetActive(false);

        for (int i = 0; i < modeScreen.transform.childCount; i++)
        {
            modeScreen.transform.GetChild(i).gameObject.SetActive(false);
        }

        modeScreen.transform.GetChild(index).gameObject.SetActive(true);

        if (index == 1)
        {
            content_3d.SetActive(true);
        }
        else if (index == 2)
        {
            panel_2D.localPosition = Vector3.zero;
            panel_2D.sizeDelta = new Vector2(1920f, 1080f);
        }

        if (modeScreen.transform.GetChild(2).GetComponent<RectTransform>() != null)
        {
            panel_2D = modeScreen.transform.GetChild(2).GetComponent<RectTransform>();
        }
    }

    private void WindowScreen_2d()
    {
        panel_2D.gameObject.SetActive(true);
        panel_2D.localPosition = new Vector3(410f, -20f, 0f);
        panel_2D.sizeDelta = new Vector2(1200f, 800f);
    }

    //private void Init_3D()
    //{
    //    List<LineRenderer> lines = WireManager_3d.Instance.wireList;
    //    foreach (LineRenderer lr in lines)
    //    {
    //        Destroy(lr.gameObject);
    //    }
    //    WireManager_3d.Instance.wireList.Clear();
    //}
    
    private void FindPinNumber()
    {
        

        ConnectionPoint[] allObject = space_2d.GetComponentsInChildren<ConnectionPoint>();
        if(!pinisOn)
        {
            foreach (ConnectionPoint obj in allObject)
            {
                Debug.Log("dddddd: " + obj.name);
                if (obj.transform.childCount != 0)
                {
                    obj.transform.GetChild(0).gameObject.SetActive(true);
                }
            }
            pinisOn = true;
        }
        else
        {
            foreach (ConnectionPoint obj in allObject)
            {
                Debug.Log("dddddd: " + obj.name);
                if (obj.transform.childCount != 0)
                {
                    obj.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
            pinisOn = false;
        }
        
    }

}
