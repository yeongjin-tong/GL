using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 나뉘어진 배경과 UI를 한번에 사용하기 위한 스크립트
public class PageManager : MonoBehaviour
{
    public static PageManager instance;

    public Transform panelSpace;
    public Transform uiSpace;
    private int screenIndex = 0;

    private class PanelPair
    {
        public GameObject panel;
        public GameObject ui;
    }

    private PanelPair[] pairs;

    private void Awake()
    {
        if (instance == null) instance = this;

        UIMatching();
        ShowPanel(screenIndex);
    }

    private void UIMatching()
    {
        int count = panelSpace.childCount;
        pairs = new PanelPair[count];

        // 🌟🌟🌟 2단계: 배열의 각 요소에 PanelPair 객체를 생성 (인스턴스 생성) 🌟🌟🌟
        for (int i = 0; i < count; i++)
        {
            pairs[i] = new PanelPair(); // PanelPair 객체 생성 후 할당
        }

        for (int i = 0; i < panelSpace.childCount; i++)
        {
            pairs[i].panel = panelSpace.GetChild(i).gameObject;
            pairs[i].ui = uiSpace.GetChild(i).gameObject;
        }
    }

    public void ShowPanel(int index)
    {
        // 모두 끄기
        foreach(var pair in pairs)
        {
            pair.panel.SetActive(false);
            pair.ui.SetActive(false);
        }

        pairs[index].panel.SetActive(true);
        pairs[index].ui.SetActive(true);
    }
}
