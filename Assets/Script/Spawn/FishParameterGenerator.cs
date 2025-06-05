// ====================
// FishParameterGenerator.cs - Parameter Generation
// ====================
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FishParameterGenerator : MonoBehaviour
{
    private SimpleFishSpawner2 spawner;
    private FishSpawnSettings settings;
    private float[] yPositions;
    private float[] radiusValues;
    private readonly float[] angleValues = { 0f, 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f, 300f, 330f };

    public void Initialize(SimpleFishSpawner2 fishSpawner)
    {
        spawner = fishSpawner;
        settings = spawner.spawnSettings;
        GenerateYPositions();
        GenerateRadiusValues();
    }

    public List<FishParameters> GenerateAllParameters()
    {
        GenerateYPositions();
        GenerateRadiusValues();

        List<FishParameters> combinations = new List<FishParameters>();

        foreach (float y in yPositions)
        {
            foreach (float radius in radiusValues)
            {
                foreach (float angle in angleValues)
                {
                    var parameters = GenerateSingleParameters(y, radius, angle);
                    combinations.Add(parameters);
                }
            }
        }

        if (settings.randomizeSpawnOrder || settings.randomizeDistribution)
        {
            ShuffleList(combinations);
        }

        if (settings.maxFishCount > 0 && settings.maxFishCount < combinations.Count)
        {
            combinations = combinations.GetRange(0, settings.maxFishCount);
        }

        return combinations;
    }

    FishParameters GenerateSingleParameters(float baseY, float baseRadius, float baseAngle)
    {
        Vector3 localCenter = new Vector3(0, baseY, 0);
        float finalRadius = baseRadius;
        float finalAngle = baseAngle;

        // Apply randomization
        if (settings.randomizeCenters)
        {
            localCenter.y += Random.Range(-settings.centerYRandomRange, settings.centerYRandomRange);
        }

        Vector3 worldCenter = spawner.transform.TransformPoint(localCenter);

        if (settings.randomizeRadii)
        {
            finalRadius += Random.Range(-settings.radiusRandomRange, settings.radiusRandomRange);
            finalRadius = Mathf.Max(0.5f, finalRadius);
        }

        if (settings.randomizeAngles)
        {
            finalAngle += Random.Range(-settings.angleRandomRange, settings.angleRandomRange);
            if (finalAngle < 0) finalAngle += 360f;
            if (finalAngle >= 360f) finalAngle -= 360f;
        }

        return new FishParameters
        {
            orbitCenter = worldCenter,
            orbitRadius = finalRadius,
            initialAngle = finalAngle,
            baseY = baseY,
            baseRadius = baseRadius,
            baseAngle = baseAngle
        };
    }

    void GenerateYPositions()
    {
        int layers = Mathf.Max(1, settings.yLayerCount);
        yPositions = new float[layers];
        
        if (layers == 1)
        {
            yPositions[0] = (settings.minCenterY + settings.maxCenterY) * 0.5f;
        }
        else
        {
            for (int i = 0; i < layers; i++)
            {
                float t = (float)i / (layers - 1);
                yPositions[i] = Mathf.Lerp(settings.minCenterY, settings.maxCenterY, t);
            }
        }
    }
    
    void GenerateRadiusValues()
    {
        int rings = Mathf.Max(1, settings.radiusRingCount);
        radiusValues = new float[rings];
        
        if (rings == 1)
        {
            radiusValues[0] = (settings.minRadius + settings.maxRadius) * 0.5f;
        }
        else
        {
            for (int i = 0; i < rings; i++)
            {
                float t = (float)i / (rings - 1);
                radiusValues[i] = Mathf.Lerp(settings.minRadius, settings.maxRadius, t);
            }
        }
    }

    public Vector3 CalculateOrbitPosition(Vector3 center, float radius, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * radius,
            0f,
            Mathf.Sin(radians) * radius
        );
        return center + offset;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public int GetTotalPossibleFish()
    {
        return yPositions.Length * radiusValues.Length * angleValues.Length;
    }

    public void DrawGizmos()
    {
        if (yPositions == null || radiusValues == null) return;

        foreach (float y in yPositions)
        {
            Vector3 localCenter = new Vector3(0, y, 0);
            Vector3 worldCenter = spawner.transform.TransformPoint(localCenter);

            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldCenter, 0.05f);

            // Draw orbit circles
            foreach (float radius in radiusValues)
            {
                float normalizedRadius = (radius - radiusValues[0]) / 
                    (radiusValues[radiusValues.Length - 1] - radiusValues[0]);
                Gizmos.color = Color.Lerp(Color.cyan, Color.blue, normalizedRadius);
                DrawWireCircle(worldCenter, radius);

                // Draw angle markers
                Gizmos.color = Color.yellow;
                foreach (float angle in angleValues)
                {
                    Vector3 anglePos = CalculateOrbitPosition(worldCenter, radius, angle);
                    Gizmos.DrawSphere(anglePos, 0.02f);
                }
            }
        }
    }

    void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}