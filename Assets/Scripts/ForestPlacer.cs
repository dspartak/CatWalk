using UnityEngine;

public class ForestPlacer : MonoBehaviour
{
    public GameObject[] treePrefabs;
    public int treeCount = 100;
    public Vector2 areaSize = new Vector2(40f, 40f);
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);
    public LayerMask groundMask;
    public float maxY = 30f;

    void Start()
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;

        int placed = 0;
        int guard = 0;
        while (placed < treeCount && guard < treeCount * 20)
        {
            guard++;
            Vector2 p = new Vector2(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f));

            Vector3 rayStart = transform.position + new Vector3(p.x, maxY, p.y);
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, maxY * 2f, groundMask))
                continue;

            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, hit.point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            float s = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale = new Vector3(s, s, s);
            placed++;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}
