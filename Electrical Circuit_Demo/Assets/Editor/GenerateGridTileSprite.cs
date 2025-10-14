// Assets/Editor/GenerateGridTileSprite.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateGridTileSprite : EditorWindow
{
    int tileSize = 64;
    int lineThickness = 1;
    Color bgColor = Color.white;
    Color lineColor = new Color(0.8f, 0.8f, 0.8f, 1f); // #CCCCCC

    [MenuItem("Tools/Grid/Generate Grid Tile Sprite")]
    static void Open() => GetWindow<GenerateGridTileSprite>("Grid Tile Generator").Show();

    void OnGUI()
    {
        tileSize = EditorGUILayout.IntField("Tile Size (px)", tileSize);
        lineThickness = EditorGUILayout.IntField("Line Thickness (px)", lineThickness);
        bgColor = EditorGUILayout.ColorField("Background", bgColor);
        lineColor = EditorGUILayout.ColorField("Line Color", lineColor);

        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
    }

    void Generate()
    {
        var tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
        // 전체 배경색
        Color[] pixels = new Color[tileSize * tileSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;
        tex.SetPixels(pixels);

        // 아래/오른쪽 경계선(타일 반복 시 끊김 없이 보이도록 상단/좌측은 비움)
        for (int x = 0; x < tileSize; x++)
            for (int t = 0; t < lineThickness; t++)
                tex.SetPixel(x, t, lineColor); // bottom line

        for (int y = 0; y < tileSize; y++)
            for (int t = 0; t < lineThickness; t++)
                tex.SetPixel(tileSize - 1 - t, y, lineColor); // right line

        tex.Apply();

        // 폴더/PNG 저장
        string dir = "Assets/GridTiles";
        Directory.CreateDirectory(dir);
        string path = $"{dir}/grid_{tileSize}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        // 임포트 설정: Sprite, Repeat
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        Debug.Log($"Generated grid tile: {path}");
    }
}
