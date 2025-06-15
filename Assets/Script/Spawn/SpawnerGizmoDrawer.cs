using UnityEngine;

public class SpawnerGizmoDrawer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public bool showSpawnGizmos = true;
    public bool showDebugInfo = true;

    private SpawnerParameterGenerator parameterGenerator;
    private SpawnerPoolManager poolManager;
    private SpawnerBatchController batchController;

    void Start()
    {
        // Get component references
        parameterGenerator = GetComponent<SpawnerParameterGenerator>();
        poolManager = GetComponent<SpawnerPoolManager>();
        batchController = GetComponent<SpawnerBatchController>();
    }

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        // Get parameter generator if not already cached
        if (parameterGenerator == null)
            parameterGenerator = GetComponent<SpawnerParameterGenerator>();

        if (parameterGenerator == null) return;

        // Ensure parameters are generated
        if (parameterGenerator.YPositions == null || parameterGenerator.RadiusValues == null)
        {
            parameterGenerator.Initialize();
        }

        DrawOrbitVisualization();
        DrawDebugInfo();
    }

    private void DrawOrbitVisualization()
    {
        // Draw orbit centers and radii for visualization (now in local space)
        foreach (float y in parameterGenerator.YPositions)
        {
            // Convert local position to world space for gizmo drawing
            Vector3 localCenter = new Vector3(0, y, 0);
            Vector3 worldCenter = transform.TransformPoint(localCenter);

            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldCenter, 0.05f);

            // Draw orbit circles for each radius at this height
            foreach (float radius in parameterGenerator.RadiusValues)
            {
                // Use different colors for different radii
                float normalizedRadius = (radius - parameterGenerator.RadiusValues[0]) / 
                    (parameterGenerator.RadiusValues[parameterGenerator.RadiusValues.Length - 1] - parameterGenerator.RadiusValues[0]);
                Gizmos.color = Color.Lerp(Color.cyan, Color.blue, normalizedRadius);
                DrawWireCircle(worldCenter, radius);

                // Draw angle markers
                Gizmos.color = Color.yellow;
                foreach (float angle in parameterGenerator.AngleValues)
                {
                    Vector3 anglePos = CalculateOrbitPosition(worldCenter, radius, angle);
                    Gizmos.DrawSphere(anglePos, 0.02f);
                }
            }
        }
    }

    private void DrawDebugInfo()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        // Draw debug info for pool system
        if (poolManager != null && poolManager.debugPooling)
        {
#if UNITY_EDITOR
            string statusText = $"Active Fish: {poolManager.ActiveFishCount}\nPooled Fish: {poolManager.PooledFishCount}\nTotal: {poolManager.TotalFishCount}";
            
            if (batchController != null && batchController.IsSpawning)
            {
                statusText += $"\nSpawning: {batchController.PendingSpawnCount} pending";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, statusText);
#endif
        }
    }

    private Vector3 CalculateOrbitPosition(Vector3 center, float radius, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * radius,
            0f,
            Mathf.Sin(radians) * radius
        );
        return center + offset;
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 32)
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