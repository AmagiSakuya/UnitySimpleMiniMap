using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MiniMap/BaseMap_Define")]
public class MiniMapBaseMap_Define : ScriptableObject
{
    [Header("世界地图底图定义")]
    public Sprite worldMapBaseImage;
    public Vector2 sizeOfImage;
    public Vector2 rectPosOfImage;
    public Vector2 rectScaleOfImage;

    [Header("小地图底图定义")]
    public LayerMask cameraCullingMask;
    public bool usePlaneMap;
    public Material planeMat;
    public Vector3 planePos;
    public Vector3 planeRot;
    public Vector3 planeScale;
}