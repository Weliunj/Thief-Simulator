using UnityEngine;
using UnityEngine.AI;

public static class NavMeshHelper
{
    public static Vector3 RandomNavmeshLocation(float radius, Vector3 origin)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position; // vị trí hợp lệ nằm trên NavMesh
        }

        return origin; // fallback nếu không tìm được vị trí
    }
}
