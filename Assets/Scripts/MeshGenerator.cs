using UnityEngine;

// Статический класс для генерации мешей
public static class MeshGenerator
{
    // Генерация меша для террейна на основе карты высот, множителя высоты, кривой высоты и уровня детализации
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        // Копирование кривой высот
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys); 

        int width = heightMap.GetLength(0); 
        int height = heightMap.GetLength(1); 
        float topLeftX = (width - 1) / -2f; 
        float topLeftZ = (height - 1) / 2f;

        // Шаг упрощения меша в зависимости от уровня детализации
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        // Количество вершин на одной линии меша
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        // Создание объекта с данными меша
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine); 
        int vertexIndex = 0; 

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                // Создание вершины меша
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    // Добавление треугольников для создания полигональной сетки
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

// Класс для хранения данных меша
public class MeshData
{
    // Массив вершин меша
    public Vector3[] vertices;
    // Массив индексов треугольников меша
    public int[] triangles;
    // Массив текстурных координат вершин
    public Vector2[] uvs; 

    int triangleIndex;

    // Конструктор класса
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    // Добавление треугольника в массив треугольников
    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    // Создание меша на основе данных
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

}
