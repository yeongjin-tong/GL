using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(SymbolData))]
public class SymbolCustom : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 타겟 스크립트의 모든 정보를 가져옴
        serializedObject.Update();

        // 2. 인스펙터에 표시할 속성(변수)들을 찾음
        SerializedProperty prefabToSpawn_2D = serializedObject.FindProperty("prefabToSpawn_2D");
        SerializedProperty useText = serializedObject.FindProperty("useText");
        SerializedProperty symbolName = serializedObject.FindProperty("symbolName");
        SerializedProperty namePosition = serializedObject.FindProperty("namePosition");


        // 3. 'enableExtraText' 체크박스를 인스펙터에 그림
        EditorGUILayout.PropertyField(prefabToSpawn_2D);
        EditorGUILayout.PropertyField(useText);

        // 4. ✨ 체크박스(boolValue)가 true일 때만 아래 코드를 실행
        if (useText.boolValue)
        {
            // 5. 'extraText' 텍스트 필드를 인스펙터에 그림
            EditorGUILayout.PropertyField(symbolName);
            EditorGUILayout.PropertyField(namePosition);
        }

        // 6. 변경된 모든 사항을 타겟 스크립트에 최종 적용
        serializedObject.ApplyModifiedProperties();
    }
}
