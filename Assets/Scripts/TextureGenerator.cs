using UnityEngine;

// Статический класс для генерации текстур
public static class TextureGenerator
{
    // Генерация текстуры на основе массива цветов, ширины и высоты
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        // Создание объекта текстуры
        Texture2D texture = new Texture2D(width, height);
        // Установка режима фильтрации
        texture.filterMode = FilterMode.Point;
        // Установка режима обертывания
        texture.wrapMode = TextureWrapMode.Clamp;
        // Установка цветов для текстуры
        texture.SetPixels(colourMap);
        // Применение изменений к текстуре
        texture.Apply();
        // Возвращение сгенерированной текстуры
        return texture; 
    }

    // Генерация текстуры на основе карты высот
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0); 
        int height = heightMap.GetLength(1); 

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Определение цвета на основе значения высоты
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height); // Генерация текстуры из цветовой карты
    }

}
