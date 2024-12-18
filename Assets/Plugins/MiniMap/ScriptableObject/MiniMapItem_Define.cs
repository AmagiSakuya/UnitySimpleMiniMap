using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MiniMapItemDBSettings
{
    public string name;
    public Sprite img;
    public bool disableBG;
    public bool changeBGColor;
    public Color overrideBGColor;
}

[CreateAssetMenu(menuName = "MiniMap/MiniMapItem_Define")]
public class MiniMapItem_Define : ScriptableObject
{
    public MiniMapItemDBSettings[] itemDB;
}