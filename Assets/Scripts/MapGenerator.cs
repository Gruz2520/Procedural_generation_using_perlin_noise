using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{

    public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
    // Режим отображения
    public DrawMode drawMode;

    // Режим нормализации шума
    public Noise.NormalizeMode normalizeMode;

    // Размер чанка карты
    public const int mapChunkSize = 241;
    // Уровень детализации для предварительного просмотра в редакторе
    [Range(0, 6)]
    public int editorPreviewLOD; 
    public float noiseScale;

    // Количество октав шума
    public int octaves; 
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed; 
    public Vector2 offset; 

    public bool useFalloff;

    public float meshHeightMultiplier; 
    public AnimationCurve meshHeightCurve; 

    public bool autoUpdate;

    // Регионы для окрашивания карты
    public TerrainType[] regions; 

    float[,] falloffMap;

    // Очередь с информацией о расчете карты
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    // Очередь с информацией о расчете меша
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>(); 

    void Awake()
    {
        // Генерация карты затухания при запуске
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize); 
    }


    public void DrawMapInEditor()
    {
        // Генерация данных карты с использованием нулевого центра
        MapData mapData = GenerateMapData(Vector2.zero);

        // Получение ссылки на объект отображения карты
        MapDisplay display = FindObjectOfType<MapDisplay>(); 
        if (drawMode == DrawMode.NoiseMap)
        {
            // Отображение шумовой карты
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            // Отображение цветовой карты
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            // Отображение меша
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            // Отображение карты затухания
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        // Запрос данных карты в отдельном потоке
        ThreadStart threadStart = delegate {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    // Генерация данных карты для заданного центра и добавление их в очередь
    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // Запрос данных меша в отдельном потоке
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    // Генерация данных меша для заданных данных карты и уровня детализации и добавление их в очередь
    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        // Обновление каждый кадр
        if (mapDataThreadInfoQueue.Count > 0)
        {
            // Если есть задачи на генерацию данных карты
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                // Извлекаем информацию о задаче из очереди и вызываем обратный вызов
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            // Если есть задачи на генерацию данных меша
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                // Извлекаем информацию о задаче из очереди и вызываем обратный вызов
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // Генерация данных карты на основе параметров и заданного центра
    MapData GenerateMapData(Vector2 centre)
    {
        // Генерация шумовой карты
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

        // Создание цветовой карты на основе шумовой карты
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloff)
                {
                    // Применение затухания (falloff) к шумовой карте
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        // Окрашивание в соответствии с регионом
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    // Валидация параметров
    void OnValidate()
    {
        if (lacunarity < 1)
        {
            // Если лакунарность меньше 1, устанавливаем ее в 1
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            // Если количество октав отрицательное, устанавливаем его в 0
            octaves = 0;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    // Структура для хранения информации о задаче в очереди
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback; 
        public readonly T parameter; 

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

    }

}

[System.Serializable]
// Структура для хранения информации о типе местности
public struct TerrainType
{
    // Название типа местности
    public string name;
    // Высота типа местности
    public float height;
    // Цвет типа местности
    public Color colour; 
}

// Структура для хранения данных карты
public struct MapData
{
    // Карта высот
    public readonly float[,] heightMap;
    // Цветовая карта
    public readonly Color[] colourMap; 

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
