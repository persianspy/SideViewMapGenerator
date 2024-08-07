using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RTProceduralGeneration : MonoBehaviour
{
    [Header("Terrain Gen")]
    [SerializeField] int width;
    [SerializeField] int height;
    [SerializeField] float smoothness;
    [SerializeField] int WaterLevel;
    int[] perlinHeightArray;

    [Header("Mineral Gen")]
    //Fill here with some info
    [Range(0, 1)]
    [SerializeField] float randomStoneFillPercent;

    [Header("Cave Gen")]
    [Range(0,1)]//  for perlin noise cave we need thses two lines of code
    [SerializeField] float modifier;
    [Range(0,100)]
    [SerializeField] int randomFillPercent;
    [SerializeField] int smoothAmount;

    [Header("Water Gen")]
    LinkedListStack<Coordinates> mapList = new LinkedListStack<Coordinates>();
    private int minWater, maxWater, randHeight;

    [Header("Surface environment Gen")]


    [Header("Tile")]
    [SerializeField] TileBase groundTile;
    [SerializeField] TileBase caveTile;
    [SerializeField] TileBase waterTile;
    [SerializeField] TileBase stoneTile;
    [SerializeField] TileBase topWaterTile;
    [SerializeField] TileBase TreeTile;
    [SerializeField] Tilemap groundTileMap;
    [SerializeField] Tilemap caveTileMap;
    [SerializeField] Tilemap waterTileMap;
    [SerializeField] Tilemap environmentTileMap;

    [Header("Tile")]
    [SerializeField] float seed;
    [SerializeField] float scale;
    [SerializeField] int octaves;
    [SerializeField] float persistance;
    [SerializeField] float lacunarity;
    float maxNoiseHeight, minNoiseHeight;

    protected int[,] map;
    private int tempGroundCheck;

    private void Start() {
        perlinHeightArray = new int[width];
        Generation();
        
    }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Generation();
        }
    }

    private void Generation() {
        //seed = Time.time;
        clearMap();
        map = GenerateArray(width, height, true);
        map = TerrainGeneration(map);
        SmoothMap(smoothAmount);
        map = stoneGeneration(map);
        map = caveAnalysis(map);
        map = surfaceWaterLevel(map);
        map = treeGeneration(map);
        RenderMap(map, groundTileMap, caveTileMap,waterTileMap, environmentTileMap, groundTile, caveTile, waterTile, stoneTile, TreeTile);
    }

    public int[,] GenerateArray(int width, int height, bool empty) {
        int[,] map = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                
                map[x, y] = (empty) ? 0 : 1;
            }
        }
        return map;
    }

    public int[,] TerrainGeneration(int[,] map) {
        System.Random pesudoRandom = new System.Random(seed.GetHashCode());
        System.Random pesudoStoneRandom = new System.Random((int) DateTime.Now.Ticks);
        int perlinHeight;
        //int maxHeight = 0;
        //int minHeight = height;
        for (int x = 0; x < width; x++) {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(x / smoothness, seed) * height/2);
            perlinHeight += height / 2;
            perlinHeightArray[x] = perlinHeight;
            for (int y = 0; y < perlinHeight; y++) {
                //map[x, y] = 1; we use this line for ground generation only
                //int caveValue = Mathf.RoundToInt(Mathf.PerlinNoise((x * modifier) + seed, (y * modifier) + seed)); we use these two lines for using perlin noise to generate caves.
                //map[x, y] = (caveValue == 1)? 2 : 1;
                float stoneValue = Mathf.PerlinNoise((pesudoStoneRandom.Next(1, 100) * modifier) + seed, (y * modifier) + seed);
                map[x, y] = (pesudoRandom.Next(1, 100) < randomFillPercent) ? 1 : 2;
                /*if (map[x,y] == 1) {
                    map[x, y] = ((stoneValue * 100) < randomStoneFillPercent) ? 6 : 1;
                }*/ //activate this part to return back to default stone production.
                //assist.GetSurroundingGround(x, y, minHeight, maxHeight);

            }
        }
        return map;
    }

    void SmoothMap( int smoothAmount) {
        for (int i = 0; i < smoothAmount; i++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < perlinHeightArray[x]; y++) {
                    if (y == 0) {
                        map[x, y] = 6;
                    }
                    else if (x == 0 || y == 0 || x == width - 1 || y == perlinHeightArray[x] - 1) {
                        map[x, y] = 1;
                    }
                    else {
                        int surroundingGroundCount = GetSurroundingGroundCount(x, y);
                        if (surroundingGroundCount > 4) {
                            map[x, y] = (tempGroundCheck == 1) ? 1 : 6;
                            tempGroundCheck = 0;
                        }
                        else if (surroundingGroundCount < 4) {
                            map[x, y] = 2;
                            tempGroundCheck = 0;
                        }
                    }
                }
            }
        }
        
    }

    int GetSurroundingGroundCount(int gridx, int gridy) {
        int groundCount = 0;
        int Ground = 0, Stone = 0;
        for (int nebx = gridx-1; nebx <= gridx+1; nebx++) {
            for (int neby = gridy-1; neby <= gridy+1; neby++) {
                if (nebx >= 0 && nebx < width && neby >= 0 && neby < height) { //shouldn't be out of neightbourhood zone
                    if (nebx != gridx || neby != gridy) { //shouldn't be the middle tile
                        if (map[nebx,neby] == 1) {
                            groundCount++;
                            Ground++;
                        }else if (map[nebx, neby] == 6) {
                            groundCount++;
                            Stone++;
                        }
                    }
                }
            }
        }
        tempGroundCheck = (Ground >= Stone) ? 1 : 6;
        return groundCount;
    }

    //private int[,] stoneGeneration(int[,] map) {

    //    for (int x = 0; x < width; x++) {
    //        for (int y = 0; y < height; y++) {
    //            stoneGenerationAssist(map, x, y);
    //        }
    //    }
    //    return map;
    //}

    private int[,] stoneGeneration(int[,] map) {
        float[,] noiseMap = new float[width, height];
        if (scale <= 0) scale = 0.0001f;
        maxNoiseHeight = float.MinValue;
        minNoiseHeight = float.MaxValue;
        float fillPercent = 60;
        int modifiedHeight, modifiedHeightStartPoint;
        float Scale;
        for (int j = 0; j < 3; j++) {
            for (int x = 0; x < width; x++) {
                if (j == 0) {
                    modifiedHeightStartPoint = 0;
                    modifiedHeight = perlinHeightArray[x] / 3;
                    Scale = 20;
                }
                else if (j == 1) {
                    modifiedHeightStartPoint = perlinHeightArray[x] / 3;
                    modifiedHeight = perlinHeightArray[x] * 2/3;
                    Scale = 15;
                }
                else if (j == 2) {
                    modifiedHeightStartPoint = perlinHeightArray[x] * 2/3;
                    modifiedHeight = perlinHeightArray[x];
                    Scale = 10;
                }
                else {
                    return null;
                }
                for (int y = modifiedHeightStartPoint; y < modifiedHeight; y++) {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    for (int i = 0; i < octaves; i++) {
                        float sampleX = x / Scale * frequency;
                        float sampleY = y / Scale * frequency;
                        float perlinValue = Mathf.PerlinNoise(sampleX + seed, sampleY + seed) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }
                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                    else if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                    noiseMap[x, y] = noiseHeight;
                }
            }
        }
        for (int j = 0; j < 3; j++) {
            if (j == 0) fillPercent = 0;
            else if (j == 1) fillPercent = 0.4f;
            else if (j == 2) fillPercent = 0.8f;
            for (int x = 0; x < width; x++) {
            if (j == 0) {
                modifiedHeightStartPoint = 0;
                modifiedHeight = perlinHeightArray[x] / 3;
            }
            else if (j == 1) {
                modifiedHeightStartPoint = perlinHeightArray[x] / 3;
                modifiedHeight = (perlinHeightArray[x] / 3) * 2;
            }
            else if (j == 2) {
                modifiedHeightStartPoint = (perlinHeightArray[x] / 3) * 2;
                modifiedHeight = perlinHeightArray[x];
            }
            else {
                    return null;
            }
            for (int y = modifiedHeightStartPoint; y < modifiedHeight; y++) {
                    //Debug.Log("noiseMap[" + X.ToString() + ", " + Y.ToString() + "] is equal to : " + noiseMap[X, Y]);
                    noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                    //Debug.Log("noiseMap[" + x.ToString() + ", " + y.ToString() + "] is equal to : " + noiseMap[x, y]);
                    if (noiseMap[x, y] > fillPercent && map[x, y] == 1) {
                        map[x, y] = 6;
                    }
                    //Debug.Log("noiseMap["+X.ToString()+", "+Y.ToString()+"] is equal to : " + noiseMap[X, Y]);
                }
            }
        }
        
        //Debug.Log("maxNoiseHeight is equal to : " + maxNoiseHeight);
        //Debug.Log("minNoiseHeight is equal to : " + minNoiseHeight);
        
    
        return map;
    }

    int[,] caveAnalysis(int[,] map) {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x, y] == 2) {
                    minWater = height - 1;
                    maxWater = 0;
                    caveAnalysisAssistant(map, x, y);
                    randHeight = 0;
                }
            }
        }
        return map;
    }
    void caveAnalysisAssistant(int[,] map, int x, int y) {
        if (y < minWater) minWater = y;
        if (y > maxWater) maxWater = y;
        if (map[x, y] == 2) {
            map[x, y] = 3;
            mapList.Push(new Coordinates(x, y));
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }else if (y > 0 && map[x,(y -1)] == 2) {
            mapList.Push(new Coordinates(x,y -1));
            map[x, y - 1] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }else if (y < map.GetLength(1) - 1 && map[x, (y + 1)] == 2) {
            mapList.Push(new Coordinates(x, y + 1));
            map[x, y + 1] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }else if (x > 0 && map[(x -1), y] == 2) {
            mapList.Push(new Coordinates((x -1), y));
            map[(x -1), y] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }else if (x < map.GetLength(0) - 1 && map[(x +1), y] == 2) {
            mapList.Push(new Coordinates((x +1), y));
            map[(x +1), y] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        } else {
            if (mapList.Count() > 0) {
                mapList.Pop();
                if (mapList.Count() > 0) {
                    Coordinates coord = mapList.Peek();
                    caveAnalysisAssistant(map, coord.X, coord.Y);
                }
            }
        }
        if(randHeight > 0) {
            if (map[x, y] == 3) {
                map[x, y] = (y <= randHeight) ?  4 : 5;
                mapList.Push(new Coordinates(x, y));
                Coordinates coord = mapList.Peek();
                caveAnalysisAssistant(map, coord.X, coord.Y);
            }
            else if (y > 0 && map[x, (y - 1)] == 3) {
                map[x, y -1] = (y -1 <= randHeight) ? 4 : 5;
                mapList.Push(new Coordinates(x, y - 1));
                Coordinates coord = mapList.Peek();
                caveAnalysisAssistant(map, coord.X, coord.Y);
            }
            else if (y < map.GetLength(1) - 1 && map[x, (y + 1)] == 3) {
                map[x, y +1] = (y +1 <= randHeight) ? 4 : 5;
                mapList.Push(new Coordinates(x, y + 1));
                Coordinates coord = mapList.Peek();
                caveAnalysisAssistant(map, coord.X, coord.Y);
            }
            else if (x > 0 && map[(x - 1), y] == 3) {
                map[x -1, y] = (y <= randHeight) ? 4 : 5;
                mapList.Push(new Coordinates((x - 1), y));
                Coordinates coord = mapList.Peek();
                caveAnalysisAssistant(map, coord.X, coord.Y);
            }
            else if (x < map.GetLength(0) - 1 && map[(x + 1), y] == 3) {
                map[x +1, y] = (y <= randHeight) ? 4 : 5;
                mapList.Push(new Coordinates((x + 1), y));
                Coordinates coord = mapList.Peek();
                caveAnalysisAssistant(map, coord.X, coord.Y);
            }
            else {
                if (mapList.Count() > 0) {
                    mapList.Pop();
                    if (mapList.Count() > 0) {
                        Coordinates coord = mapList.Peek();
                        caveAnalysisAssistant(map, coord.X, coord.Y);
                    }
                }
            }
        }else if (randHeight == 0) {
            randHeight = RandomWaterHeight(minWater, maxWater);
            mapList.Push(new Coordinates(x, y));
            Coordinates coordi = mapList.Peek();
            caveAnalysisAssistant(map, coordi.X, coordi.Y);
        }
    }

    int[,] surfaceWaterLevel(int[,] map) {
        int waterLevel = WaterLevelDecleration(perlinHeightArray);
        for (int x = 0; x < width; x++) {
            for (int y = perlinHeightArray[x]; y <= waterLevel; y++) {
                map[x, y] = 4;
            }
        }
        return map;
    }

    private int[,] treeGeneration(int[,] map) {
        bool collision = false;
        for (int x = 0; x < width; x++) {
            int y = perlinHeightArray[x];
            System.Random pesudoTreeRandom = new System.Random((int)DateTime.Now.Ticks);
            if (map[x,y] == 0 && map[x,y-1] == 1) {
                Debug.Log("working");
                for (int xmas = x - 5; xmas <= x + 5; xmas++) {
                    if (xmas >= 0 && xmas < width && y + 2 < height) { //shouldn't be out of neightbourhood zone
                        if (map[xmas, y + 2] != 0) collision = true;
                    }
                }
                if (collision == false) {
                    if (pesudoTreeRandom.Next(0, 101) < 30) map[x, y + 2] = 7;
                }
                collision = false;
            }
        }
        return map;
    }
   

    public void RenderMap(int[,] map, Tilemap groundTileMap, Tilemap caveTileMap,Tilemap waterTileMap, Tilemap environmentTileMap, TileBase groundTileBase, TileBase caveTileBase, TileBase waterTileBase, TileBase stoneTileBase, TileBase treeTileBase) {
        for (int x = 0; x< width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x, y] == 1) {
                    groundTileMap.SetTile(new Vector3Int(x, y, 0), groundTileBase);
                }else if (map[x, y] == 2 || map[x, y] == 5) {
                    caveTileMap.SetTile(new Vector3Int(x, y, 0), caveTileBase);
                }else if (map[x, y] == 4) {
                    waterTileMap.SetTile(new Vector3Int(x, y, 0), waterTileBase);
                }else if (map[x, y] == 6) {
                    groundTileMap.SetTile(new Vector3Int(x, y, 0), stoneTileBase);
                }else if (map[x,y] == 7) {
                    environmentTileMap.SetTile(new Vector3Int(x, y, 0), treeTileBase);
                }
            }
        }
    }

    private void clearMap() {
        groundTileMap.ClearAllTiles();
        caveTileMap.ClearAllTiles();
        waterTileMap.ClearAllTiles();
        environmentTileMap.ClearAllTiles();
    }

    int RandomWaterHeight(int min, int max) {
        System.Random pesudoRandom = new System.Random((int)DateTime.Now.Ticks);
        int randomHeight = pesudoRandom.Next(min - 1, max);
        return randomHeight;
    }

    private int WaterLevelDecleration(int[] perlinHeight) {
        int perlinHeightMax = 0;
        int perlinHeightMin = height;
        for (int x = 0; x < width; x++) {
            perlinHeightMax = (perlinHeight[x] >= perlinHeightMax) ? perlinHeight[x] : perlinHeightMax;
            perlinHeightMin = (perlinHeight[x] <= perlinHeightMin) ? perlinHeight[x] : perlinHeightMin;
        }
        WaterLevel = perlinHeightMin +( (perlinHeightMax - perlinHeightMin) / 3);
        //System.Random pesudoRandom = new System.Random((int)DateTime.Now.Ticks);
        //WaterLevel = pesudoRandom.Next(perlinHeightMin , perlinHeightMax);
        return WaterLevel;
    }
}

struct Coordinates {
    public int X, Y;
    public Coordinates(int x, int y) {
        X = x; Y = y;
    }

}

