using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
    // Масштаб для позиции обзора
    const float scale = 5f;

    // Минимальное расстояние, которое должен пройти обзорщик, чтобы обновить чанки
    const float viewerMoveThresholdForChunkUpdate = 25f;
    // Квадрат этого расстояния
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    // Уровни детализации
    public LODInfo[] detailLevels;
    // Максимальное расстояние обнаружения
    public static float maxViewDst;

    // Позиция обзорщика
    public Transform viewer;
    // Материал карты
    public Material mapMaterial;

    // Позиция обзорщика на плоскости
    public static Vector2 viewerPosition;
    // Предыдущая позиция обзорщика
    Vector2 viewerPositionOld;
    // Генератор карты
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    // Словарь чанков, обнаруженных на карте
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    // Список чанков, видимых при последнем обновлении
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>(); 

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        // Максимальное расстояние обнаружения - последний порог видимости из уровней детализации
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        // Обновляем видимые чанки
        UpdateVisibleChunks(); 
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale; // Текущая позиция обзорщика на плоскости

        // Если обзорщик переместился на достаточное расстояние, чтобы обновить чанки
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    // Обновляем видимые чанки
    void UpdateVisibleChunks()
    {
        // Скрываем все видимые чанки с предыдущего обновления
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Проходимся по всем чанкам, видимым в поле зрения
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // Если чанк уже был создан, обновляем его
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                // Иначе создаем новый чанк
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        // Игровой объект меша чанка
        GameObject meshObject;
        // Позиция чанка на плоскости
        Vector2 position;
        // Границы чанка
        Bounds bounds; 

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            // Рассчитываем позицию чанка на плоскости
            position = coord * size;
            // Рассчитываем границы чанка
            bounds = new Bounds(position, Vector2.one * size);
            // Рассчитываем позицию чанка в трехмерном пространстве
            Vector3 positionV3 = new Vector3(position.x, 0, position.y); 

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            // Устанавливаем позицию объекта меша с учетом масштаба
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            // Создаем меши для каждого уровня детализации
            lodMeshes = new LODMesh[detailLevels.Length]; 
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            // Запрашиваем данные карты
            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        // Когда данные карты получены
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }


        // Обновляем чанк
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        // Метод, вызываемый после получения данных о меше
        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // Вызываем обратный вызов
            updateCallback();
        }

        // Метод для запроса меша
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
    }


}
