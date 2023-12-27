using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{
    // Компоненты
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    // Метод для отображения текстуры
    public void DrawTexture(Texture2D texture)
    {
        // Установка текстуры для рендерера
        textureRender.sharedMaterial.mainTexture = texture;
        // Изменение размера объекта согласно размерам текстуры
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height); 
    }

    // Метод для отображения меша
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        // Установка сгенерированного меша в фильтр меша
        meshFilter.sharedMesh = meshData.CreateMesh();
        // Установка текстуры для рендерера меша
        meshRenderer.sharedMaterial.mainTexture = texture; 
    }

}
