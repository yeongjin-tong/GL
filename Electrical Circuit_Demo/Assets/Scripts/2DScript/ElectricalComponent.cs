using System.Collections.Generic;
using UnityEngine;


// abstract: 이 클래스는 직접 사용할 수 없고, 다른 클래스가 상속해야만 함
public abstract class ElectricalComponent : MonoBehaviour
{
    [Tooltip("이 부품의 고유 ID")]
    public string uniqueID;

    // ✨ 두 강물(신호)이 닿았는지 확인하는 변수
    public bool isLive = false;
    public bool isGrounded = false;

    // ✨ 핵심 상태 변수
    public bool isPowered = false;

    // 자신이 전력원인지 표시하는 변수
    public bool isPowerSource = false;



    // 전원을 켜는 함수 (이제 시각적 효과만 담당)
    public virtual void PowerOn()
    {
        isPowered = true;
    }

    // 전원을 끄는 함수 (이제 시각적 효과만 담당)
    public virtual void PowerOff()
    {
        isPowered = false;
    }

    // 이 컴포넌트의 상태를 초기화하는 함수
    public virtual void Reset()
    {
        isLive = false;
        isGrounded = false;
        // isPowered는 PowerOn/Off를 통해 제어되므로 여기서 바꾸지 않음
    }

    // ✨ 시뮬레이션이 시작될 때 호출될 함수
    public virtual void OnSimulationStart()
    {
        // 배터리 같은 특별한 부품이 이 함수를 재정의해서 사용하게 됨
    }

    // ✨ 시뮬레이션이 중지될 때 호출될 함수 (모든 부품 리셋)
    public virtual void OnSimulationStop()
    {
        // 모든 부품은 시뮬레이션이 끝나면 꺼지는 것이 기본 규칙
        PowerOff();
    }
}