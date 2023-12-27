using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            // ќтображение стандартного инспектора и проверка, были ли изменены какие-либо пол€
            if (mapGen.autoUpdate)
            {
                // ≈сли включена автообновление, запускаем генерацию карты
                mapGen.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            // ќтображаем кнопку "Generate" и запускаем генерацию карты при нажатии
            mapGen.DrawMapInEditor();
        }
    }
}
