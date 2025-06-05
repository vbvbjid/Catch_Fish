// ====================
// FishParameters.cs - Data Structure
// ====================
using UnityEngine;
[System.Serializable]
public struct FishParameters
{
    public Vector3 orbitCenter;
    public float orbitRadius;
    public float initialAngle;
    public float baseY;
    public float baseRadius;
    public float baseAngle;

    public override string ToString()
    {
        return $"Base: Y{baseY:F1}_R{baseRadius:F1}_A{baseAngle:F0}° | Final: Center{orbitCenter}, Radius{orbitRadius:F1}, Angle{initialAngle:F0}°";
    }
}