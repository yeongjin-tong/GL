using UnityEngine;
using UnityEngine.UI;

// 이 스크립트는 UI 심볼 이미지에 붙여서, 자신이 어떤 부품인지에 대한 정보만 가집니다.
[RequireComponent(typeof(Image))]
public class SymbolData : MonoBehaviour
{
    [Tooltip("이 심볼을 드래그했을 때 생성될 2D 부품 프리팹")]
    public GameObject prefabToSpawn_2D;

    public bool useText;
    public string symbolName;
}