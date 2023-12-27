using UnityEngine;
using System.Collections;

// Класс для генерации шума
public static class Noise
{
    // Режимы нормализации
    public enum NormalizeMode { Local, Global };

    // Генерация карты шума на основе параметров
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        // Массив для хранения высот шума
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Порождающий элемент
        System.Random prng = new System.Random(seed);
        // Сдвиг октав, чтобы при наложении друг на друга получить более интересную картинку
        Vector2[] octaveOffsets = new Vector2[octaves]; 

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            // Учитываем внешний сдвиг положения
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            // Если scale меньше или равно 0, устанавливаем его на самое маленькое положительное значение
            scale = 0.0001f;
        }
        // Максимальная локальная высота шума
        float maxLocalNoiseHeight = float.MinValue;
        // Минимальная локальная высота шума
        float minLocalNoiseHeight = float.MaxValue; 

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Генерируем точки на карте высот
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // Обработка наложения октав
                for (int i = 0; i < octaves; i++)
                {
                    // Генерация высоты шума на каждой октаве с использованием Perlin Noise
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // Вместо Mathf.PerlinNoise() можно написать собственную реализацию генератора шума,
                    // Что позволит заменить бикубическую интерполяцию на биквадратную, что позволит избавиться от сложных стыков между полями 

                    // Получение высоты из ГСПЧ
                    // Нормализация значения Perlin Noise к диапазону [-1, 1] так как при наложении октав есть вероятность выхода за границы
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // Наложение октав
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    // Обновление максимальной локальной высоты шума
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    // Обновление минимальной локальной высоты шума
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    // Нормализация высоты шума в локальном диапазоне
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    // Глобальная нормализация высоты шума
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);

                }
            }
        }

        return noiseMap;
    }
}
