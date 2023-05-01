using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Tilemaps;


// Tile类表示一个地块，存储地块的坐标和类型
public class Tile
{
    public Vector2Int Coordinates { get; set; }
    public int type;
    public GameObject gameobject { get; set; }
    public Tile(Vector2Int coordinates, int type)
    {
        this.Coordinates = coordinates;
        this.type = type;
    }
}

// Chunk类表示地图的一个分块，包含一个二维Tile数组
public class Chunk
{
    public int width;
    public int height;
    public Dictionary<Vector2Int, Tile> tiles;

    public Chunk(int width, int height)
    {
        this.width = width;
        this.height = height;
        tiles = new Dictionary<Vector2Int, Tile>();

        // 生成随机地形作为示例
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                int randomType = Random.Range(0, 100) < 30 ? 1 : 0; // 0表示平地，1表示障碍物，这里设置30%的概率生成障碍物
                if (randomType == 1 && !HasEnoughEmptyNeighbors(coordinates, 5))
                {
                    randomType = 0; // 如果周围空地块数量不足，则将当前地块设为平地
                }
                tiles[coordinates] = new Tile(coordinates, randomType);
            }
        }
    }
    private bool HasEnoughEmptyNeighbors(Vector2Int coordinates, int requiredEmptyNeighbors)
    {
        int emptyNeighbors = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2Int neighborCoordinates = new Vector2Int(coordinates.x + x, coordinates.y + y);
                Tile neighbor;
                if (tiles.TryGetValue(neighborCoordinates, out neighbor))
                {
                    if (neighbor.type == 0) emptyNeighbors++;
                }
                else
                {
                    emptyNeighbors++;
                }
            }
        }

        return emptyNeighbors >= requiredEmptyNeighbors;
    }
}

[System.Serializable]
public class SerializableVector2Int
{
    public int x;
    public int y;

    public SerializableVector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }

    public static SerializableVector2Int FromVector2Int(Vector2Int vector)
    {
        return new SerializableVector2Int(vector.x, vector.y);
    }
}
[System.Serializable]
public class SavedMapData
{
    public List<SavedTileData> tiles;

    public SavedMapData(List<SavedTileData> tiles)
    {
        this.tiles = tiles;
    }
}

[System.Serializable]
public class SavedTileData
{
    public SerializableVector2Int coordinates;
    public int type;

    public SavedTileData(SerializableVector2Int coordinates, int type)
    {
        this.coordinates = coordinates;
        this.type = type;
    }
}

public class MapManager : MonoBehaviour
{
    public string saveFileName = "map_data.json";
    public int chunkWidth = 16;
    public int chunkHeight = 16;
    public int chunksX = 4;
    public int chunksY = 4;
    public GameObject[] tilePrefabs;
    private Dictionary<Vector2Int, Tile> tileMap = new Dictionary<Vector2Int, Tile>();
    private Chunk[,] chunks;

    void Start()
    {
        GenerateMap();
    }

    // 生成分块地图
    private void GenerateMap()
    {
        chunks = new Chunk[chunksX, chunksY];
        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                chunks[x, y] = new Chunk(chunkWidth, chunkHeight);
                GenerateChunkVisual(x, y);
            }
        }
    }

    // 生成分块的可视化部分
    private void GenerateChunkVisual(int chunkX, int chunkY)
    {
        Chunk chunk = chunks[chunkX, chunkY];
        foreach (KeyValuePair<Vector2Int, Tile> kvp in chunk.tiles)
        {
            Tile tile = kvp.Value;
            GameObject tilePrefab = tilePrefabs[tile.type];
            Vector3 tilePosition = new Vector3(chunkX * chunkWidth + tile.Coordinates.x, chunkY * chunkHeight + tile.Coordinates.y, 0);
            GameObject obj = Instantiate(tilePrefab, tilePosition, Quaternion.identity, transform);
            tile.gameobject = obj;

            // 添加 Tile 对象到 tileMap
            tileMap[tile.Coordinates] = tile;
        }
    }


    // 根据坐标获取Tile对象
    public Tile GetTileAt(Vector2Int coordinates)
    {
        Tile tile;
        if (tileMap.TryGetValue(coordinates, out tile))
        {
            return tile;
        }
        return null;
    }
    public void CreateTile(Vector2Int coordinates, int tileType)
    {
        int chunkX = coordinates.x / chunkWidth;
        int chunkY = coordinates.y / chunkHeight;

        if (chunks[chunkX, chunkY] == null)
        {
            chunks[chunkX, chunkY] = new Chunk(chunkWidth, chunkHeight);
        }

        Tile newTile = new Tile(coordinates, tileType);
        chunks[chunkX, chunkY].tiles[coordinates] = newTile;

        GameObject tilePrefab = tilePrefabs[tileType];
        Vector3 tilePosition = new Vector3(coordinates.x, coordinates.y, 0);
        GameObject obj = Instantiate(tilePrefab, tilePosition, Quaternion.identity, transform);
        newTile.gameobject = obj;

        // 更新 tileMap
        tileMap[coordinates] = newTile;
        //在给定坐标处创建一个新地块，地块类型由参数tileType指定。首先计算所属的区块，如果区块不存在则创建一个新区块。然后创建一个新的Tile对象并添加到区块的tiles字典中。最后，实例化地块预制体并设置其位置。
    }

    public void RemoveTile(Vector2Int coordinates)
    {
        int chunkX = coordinates.x / chunkWidth;
        int chunkY = coordinates.y / chunkHeight;

        if (chunks[chunkX, chunkY] != null && chunks[chunkX, chunkY].tiles.ContainsKey(coordinates))
        {
            chunks[chunkX, chunkY].tiles.Remove(coordinates);
            Tile tile=GetTileAt(coordinates);
            if(tile != null)
            {
                Destroy(tile.gameobject);
            }
            tileMap.Remove(coordinates);
        }
        //删除给定坐标处的地块。首先计算所属的区块。如果区块存在且包含给定坐标的地块，则从区块的tiles字典中删除该地块。接着，使用Physics2D.OverlapPointAll方法找到给定坐标处的所有碰撞器，并遍历它们。如果找到标签为“Tile”的游戏对象，就销毁它。
    }
    public void SaveMap()
    {
        List<SavedTileData> savedTiles = new List<SavedTileData>();

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                foreach (KeyValuePair<Vector2Int, Tile> kvp in chunks[x, y].tiles)
                {
                    Tile tile = kvp.Value;
                    // 将局部坐标转换为全局坐标
                    Vector2Int globalCoordinates = new Vector2Int(x * chunkWidth + tile.Coordinates.x, y * chunkHeight + tile.Coordinates.y);
                    SerializableVector2Int coordinates = SerializableVector2Int.FromVector2Int(globalCoordinates);
                    savedTiles.Add(new SavedTileData(coordinates, tile.type));
                }
            }
        }

        SavedMapData savedMapData = new SavedMapData(savedTiles);
        string json = JsonUtility.ToJson(savedMapData, true);
        File.WriteAllText(Path.Combine(Application.dataPath + "/Maps/", saveFileName), json);
    }



    public void LoadMap()
    {
        string path = Path.Combine(Application.dataPath + "/Maps/", saveFileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SavedMapData savedMapData = JsonUtility.FromJson<SavedMapData>(json);

            foreach (SavedTileData savedTile in savedMapData.tiles)
            {
                // 将全局坐标转换为局部坐标
                Vector2Int localCoordinates = new Vector2Int(savedTile.coordinates.x % chunkWidth, savedTile.coordinates.y % chunkHeight);
                int tileType = savedTile.type;

                if (GetTileAt(localCoordinates) != null)
                {
                    RemoveTile(localCoordinates);
                }

                CreateTile(localCoordinates, tileType);
            }
        }
        else
        {
            Debug.LogError("Save file not found!");
        }
    }





    [System.Serializable]
    public class TileData
    {
        public SerializableVector2Int coordinates;
        public int type;
    }


}
