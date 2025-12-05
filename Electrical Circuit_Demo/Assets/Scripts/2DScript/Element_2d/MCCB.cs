using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MCCB는 본질적으로 '스위치'이므로 Switch를 상속받습니다.
// 이렇게 하면 CircuitSolver가 자동으로 이 부품을 '스위치'로 인식하고 처리합니다.
public class MCCB : Switch
{
    // MCCB만의 고유한 기능이 필요하다면 여기에 추가합니다.
    // (예: 정격 전류 설정, 트립 기능 등)

    // 현재로서는 기본 Switch 기능만으로도 충분합니다.
    // Switch.cs의 OnEnable()에서 자동으로 switchGroups에 등록되어
    // 같은 이름(symbol_ID)을 가진 3개의 MCCB 극이 동시에 켜지고 꺼지게 됩니다.
}
