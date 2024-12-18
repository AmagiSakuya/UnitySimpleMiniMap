using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MiniMap/WorldMpaCoordinateReference_Define")]
public class WorldMpaCoordinateReference_Define : ScriptableObject
{
    public Vector3 basePoint;
    public Vector3 upPoint;
    public Vector3 rightPoint;
    public Vector3 basePoint_ui;
    public Vector3 upPoint_ui;
}
