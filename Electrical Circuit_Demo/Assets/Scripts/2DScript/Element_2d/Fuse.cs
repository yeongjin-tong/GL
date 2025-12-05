using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fuse : ElectricalComponent
{
    // 퓨즈가 끊어졌는지 여부 (기본값: false = 연결됨)
    public bool isBlown = false;

    // 현재는 별다른 로직 없이, CircuitSolver가 이 컴포넌트를 'Fuse' 타입으로 인식하게 하는 용도로만 사용
}
