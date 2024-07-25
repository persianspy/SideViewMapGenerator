using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RTProceduralGeneration : MonoBehaviour
{
    [Header("Terrain Gen")]
    [SerializeField] int width;
    [SerializeField] int height;
    [SerializeField] float smoothness;
    int[] perlinHeightArray;

    [Header("Cave Gen")]
    //[Range(0,1)]  for perlin noise cave we need thses two lines of code
    //[SerializeField] float modifier;
    [Range(0,100)]
    [SerializeField] int randomFillPercent;
    [SerializeField] int smoothAmount;

    [Header("Water Gen")]
    //fill with something
    LinkedListStack<Coordinates> mapList = new LinkedListStack<Coordinates>();

    [Header("Tile")]
    [SerializeField] TileBase groundTile;
    [SerializeField] TileBase caveTile;
    [SerializeField] TileBase waterTile;
    [SerializeField] TileBase topWaterTile;
    [SerializeField] Tilemap groundTileMap;
    [SerializeField] Tilemap caveTileMap;
    [SerializeField] Tilemap waterTileMap;

    [SerializeField] float seed;

    protected int[,] map;

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
        RenderMap(map, groundTileMap, caveTileMap, groundTile, caveTile);
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
                map[x, y] = (pesudoRandom.Next(1, 100) < randomFillPercent) ? 1 : 2;
                //assist.GetSurroundingGround(x, y, minHeight, maxHeight);

            }
        }
        return map;
    }

    void SmoothMap( int smoothAmount) {
        for (int i = 0; i < smoothAmount; i++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < perlinHeightArray[x]; y++) {
                    if (x == 0 || y == 0 || x == width - 1 || y == perlinHeightArray[x] - 1) {
                        map[x, y] = 1;
                    }
                    else {
                        int surroundingGroundCount = GetSurroundingGroundCount(x, y);
                        if (surroundingGroundCount > 4) {
                            map[x, y] = 1;
                        }
                        else if (surroundingGroundCount < 4) {
                            map[x, y] = 2;
                        }
                    }
                }
            }
        }
        
    }

    int GetSurroundingGroundCount(int gridx, int gridy) {
        int groundCount = 0;
        for (int nebx = gridx-1; nebx <= gridx+1; nebx++) {
            for (int neby = gridy-1; neby <= gridy+1; neby++) {
                if (nebx >= 0 && nebx < width && neby >= 0 && neby < height) { //shouldn't be out of neightbourhood zone
                    if (nebx != gridx || neby != gridy) { //shouldn't be the middle tile
                        if (map[nebx,neby] == 1) {
                            groundCount++;
                        }
                    }
                }
            }
        }
        return groundCount;
    }

    void caveAnalysis(int[,] map) {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x, y] == 2) {

                }
            }
        }
    }
    void caveAnalysisAssistant(int[,] map, int x, int y) {
        if (map[x,(y -1)] == 2) {
            mapList.Push(new Coordinates(x,y -1));
            map[x, y - 1] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }
        else if (map[x, (y + 1)] == 2) {
            mapList.Push(new Coordinates(x, y + 1));
            map[x, y + 1] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }
        else if (map[(x -1), y] == 2) {
            mapList.Push(new Coordinates((x -1), y));
            map[(x -1), y] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }
        else if (map[(x +1), y] == 2) {
            mapList.Push(new Coordinates((x +1), y));
            map[(x +1), y] = 3;
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        } else {
            mapList.Pop();
            Coordinates coord = mapList.Peek();
            caveAnalysisAssistant(map, coord.X, coord.Y);
        }

    }
    public void RenderMap(int[,] map, Tilemap groundTileMap, Tilemap caveTileMap, TileBase groundTilebase, TileBase caveTile) {
        for (int x = 0; x< width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x,y] == 1) {
                    groundTileMap.SetTile(new Vector3Int(x, y, 0), groundTilebase);
                } else if (map[x,y] ==2) {
                    caveTileMap.SetTile(new Vector3Int(x, y, 0), caveTile);
                }
            }
        }
    }

    private void clearMap() {
        groundTileMap.ClearAllTiles();
        caveTileMap.ClearAllTiles();
    }
}

struct Coordinates {
    public int X, Y;
    public Coordinates(int x, int y) {
        X = x; Y = y;
    }

}

