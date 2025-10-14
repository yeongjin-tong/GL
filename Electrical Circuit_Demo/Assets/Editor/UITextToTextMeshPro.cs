using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Modules.Util
{
    public class UITextToTextMeshPro : MonoBehaviour
    {
        //기존 UI Text 정보를 저장할 데이터 객체
        public class TextData
        {
            public string text; //문자열 정보
            public Color color; //텍스트 컬러 정보
            public TMP_FontAsset font; //폰트 정보
            public int fontSize; //폰트 사이즈 정보
            public int fontMaxSize; //폰트 최대 사이즈 정보(AutoSize 시)
            public bool autoSize; //AutoSize 여부
            public TextAlignmentOptions alignOptions; //정렬 옵션
            public FontStyles fontStyles; //폰트 스타일 옵션

        }

        //폰트 경로 설정.
        public const string PATH_FONT_TEXTMESHPRO_JALNAN = "Assets/Fonts/Jalnan SDF.asset";

        [MenuItem("CustomMenu/Text/UITextToTextMeshProUGUI(UI 텍스트를 TextMeshProUGUI 텍스트로 교체함")]
        public static void ChangeTextMeshProUGUI()
        {
            GameObject[] rootObj = GetSceneRootObjects();

            for (int i = 0; i < rootObj.Length; i++)
            {
                GameObject gbj = (GameObject)rootObj[i] as GameObject;
                Component[] com = gbj.transform.GetComponentsInChildren(typeof(Text), true);
                foreach (Text txt in com)
                {

                    TextData data = ConvertTextData(txt);
                    GameObject obj = txt.gameObject;
                    //기존 Text 컴포넌트 삭제
                    DestroyImmediate(txt.gameObject.GetComponent<Text>());
                    if (!obj.GetComponent<TextMeshProUGUI>())
                    {
                        TextMeshProUGUI newGUIText = obj.AddComponent<TextMeshProUGUI>();
                        newGUIText.text = data.text;
                        newGUIText.font = data.font;
                        newGUIText.fontSize = data.fontSize;
                        newGUIText.color = data.color;
                        newGUIText.enableAutoSizing = data.autoSize;
                        newGUIText.fontSizeMax = data.fontMaxSize;
                        newGUIText.alignment = data.alignOptions;
                        newGUIText.fontStyle = data.fontStyles;
                    }
                }
            }
        }

        /// <summary>
        /// UGUI Text 정보를 TextMeshPro Text 정보로 Convert 한다.
        /// </summary>
        /// <param name="text">UGUI Text</param>
        /// <returns>TextMeshProUGUI TextData</returns>
        private static TextData ConvertTextData(Text text)
        {
            TextData data = new TextData();
            data.text = text.text;
            data.color = text.color;
            data.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PATH_FONT_TEXTMESHPRO_JALNAN);
            data.fontSize = text.fontSize;
            data.fontMaxSize = text.resizeTextMaxSize;
            data.autoSize = text.resizeTextForBestFit;
            switch (text.alignment)
            {
                case TextAnchor.LowerCenter:
                    data.alignOptions = TextAlignmentOptions.Bottom;
                    break;
                case TextAnchor.LowerLeft:
                    data.alignOptions = TextAlignmentOptions.BottomLeft;
                    break;
                case TextAnchor.LowerRight:
                    data.alignOptions = TextAlignmentOptions.BottomRight;
                    break;
                case TextAnchor.MiddleCenter:
                    data.alignOptions = TextAlignmentOptions.Midline;
                    break;
                case TextAnchor.MiddleLeft:
                    data.alignOptions = TextAlignmentOptions.Left;
                    break;
                case TextAnchor.MiddleRight:
                    data.alignOptions = TextAlignmentOptions.Right;
                    break;
                case TextAnchor.UpperCenter:
                    data.alignOptions = TextAlignmentOptions.Top;
                    break;
                case TextAnchor.UpperLeft:
                    data.alignOptions = TextAlignmentOptions.TopLeft;
                    break;
                case TextAnchor.UpperRight:
                    data.alignOptions = TextAlignmentOptions.TopRight;
                    break;
            }
            switch (text.fontStyle)
            {
                case FontStyle.Bold:
                    data.fontStyles = FontStyles.Bold;
                    break;
                case FontStyle.BoldAndItalic:
                case FontStyle.Italic:
                    data.fontStyles = FontStyles.Italic;
                    break;
                default:
                    data.fontStyles = FontStyles.Normal;
                    break;
            }

            return data;
        }

        /// <summary>
        /// 모든 최상위 Root의 GameObject를 받아옴.
        /// </summary>
        /// <returns></returns>
        private static GameObject[] GetSceneRootObjects()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            return currentScene.GetRootGameObjects();
        }
    }
}