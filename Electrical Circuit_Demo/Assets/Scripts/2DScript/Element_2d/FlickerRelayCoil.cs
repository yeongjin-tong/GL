using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class FlickerRelayCoil : ElectricalComponent
{
    [SerializeField] private float time;
    [SerializeField] private float curTime;

    private TextMeshProUGUI countText;

    private bool isRunning = false;

    // 연동되어있는 스위치 리스트
    private List<RelaySwitch> relaySwitches = new List<RelaySwitch>();

    private bool currentFlickerState = false;

    public override void OnSimulationStart()
    {
        base.OnSimulationStart();
        StartSetting();
        time = float.Parse(countText.text);
    }

    public override void OnSimulationStop()
    {
        base.OnSimulationStop();
        relaySwitches.Clear();
    }

    public override void PowerOn()
    {
        base.PowerOn();
        if (!isRunning)
        {
            StartCoroutine(StartTimer());
        }
    }

    public override void PowerOff()
    {
        base.PowerOff();
        StopAllCoroutines();
        currentFlickerState = false;
        isRunning = false;
        if(countText!= null)
        {
            countText.text = time.ToString();
        }
        ControlLinkedSwitches(false, true);
    }

    private void StartSetting() // 시작 초기 세팅
    {
        RelaySwitch[] allSwitches = FindObjectsOfType<RelaySwitch>();

        foreach (RelaySwitch relay in allSwitches)
        {
            if(relay.symbol_Text == this.symbol_Text && (relay.switchType == Type.Relay || relay.switchType == Type.Flicker))
            {
                relaySwitches.Add(relay);
            }
        }

        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (tmp.name == "countNum_text")
            {
                countText = tmp;
                time = float.Parse(countText.text);
                break;
            }
        }
    }

    IEnumerator StartTimer()
    {
        isRunning = true;

        time = float.Parse(countText.text);

        curTime = time;

        while (curTime > 0)
        {
            yield return null;
            curTime -= Time.deltaTime;

            countText.text = curTime.ToString("N1");

            if (curTime <= 0)
            {
                currentFlickerState = !currentFlickerState;

                ControlLinkedSwitches(currentFlickerState);
                curTime = time;
            }
            if (!isRunning) yield break;
        }
    }

    private void ControlLinkedSwitches(bool state, bool init = false)
    {
        if (!init)
        {
            foreach (RelaySwitch relay in relaySwitches)
            {
                if(relay.isOn == state)
                {
                    relay.SetContactState(!state);
                }
                else
                {
                    relay.SetContactState(state);
                }
            }
        }
        else
        {
            foreach(RelaySwitch relay in relaySwitches)
            {
                bool initState = relay.GetInitState();

                relay.SetContactState(initState);
            }
        }
    }
}