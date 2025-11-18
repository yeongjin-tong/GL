using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class Timer : ElectricalComponent
{
    [SerializeField] private float time;
    [SerializeField] private float curTime;

    private TextMeshProUGUI countText;

    bool isRunning = false;


    IEnumerator StartTimer()
    {
        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (tmp.name == "countNum_text")
            {
                countText = tmp;
                break;
            }
        }


        isRunning = true;

        time = float.Parse(countText.text);

        curTime = time; // 현재 시간을 초기 시간으로 설정

        while (curTime > 0)
        {
            yield return null; // 다음 프레임까지 대기
            curTime -= Time.deltaTime; // 시간 감소

            countText.text = curTime.ToString("N1");

            if (curTime <= 0)
            {
                Debug.Log("시간 종료");
                curTime = 0;

                // [수정] 텍스트가 0으로 확실히 표시되도록 함
                if (countText != null) countText.text = "0";

                ControlLinkedSwitches(true, Type.Timer); // 타이머 스위치 작동
                yield break; // 코루틴 종료
            }
        }
    }


    private void StateSetting(bool isOn)
    {
        if(isOn)
        {
            ControlLinkedSwitches(true, Type.Relay);
            if (!isRunning)
            {
                StartCoroutine(StartTimer());
            }
        }
        else
        {
            ControlLinkedSwitches(false, Type.Relay);
            ControlLinkedSwitches(false, Type.Timer);
            StopAllCoroutines();
            isRunning = false;
            if(countText != null)
            {
                countText.text = time.ToString();
            }
        }
    }

    public override void PowerOn()
    {
        Debug.Log("파워온 호출");
        base.PowerOn();
        StateSetting(true);
    }


    public override void PowerOff()
    {
        base.PowerOff();
        StateSetting(false);
    }

    /// <summary>
    /// 이 코일과 연결된 모든 릴레이 스위치를 찾아 상태를 변경합니다.
    /// </summary>
    private void ControlLinkedSwitches(bool newState, Type type)
    {
        // 씬에 있는 모든 릴레이 스위치를 찾습니다.
        RelaySwitch[] allSwitches = FindObjectsOfType<RelaySwitch>();

        foreach (var relay in allSwitches)
        {
            // ID가 일치하는 스위치만 제어합니다.
            if (relay.symbol_ID == this.symbol_ID && relay.switchType == type)
            {
                bool initState = relay.GetInitState();

                bool targetState;

                if (newState)
                {
                    targetState = !initState;
                }
                else
                {
                    targetState = initState;
                }

                relay.SetContactState(targetState);
            }
        }
    }
}