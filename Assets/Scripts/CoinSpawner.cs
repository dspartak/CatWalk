using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public static CoinSpawner Instance;

    [Header("Что и сколько")]
    public GameObject coinPrefab;
    public int maxCoins = 10;

    [Header("Где спавнить")]
    public Vector3 areaCenter = Vector3.zero;
    public float spawnRadius = 18f;
    public Transform cat;
    public float minDistanceFromCat = 3f;

    [Header("Респавн")]
    public float respawnDelay = 0.5f;

    private readonly List<Coin> activeCoins = new List<Coin>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (cat == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cat = player.transform;
        }

        for (int i = 0; i < maxCoins; i++) SpawnOne();
    }

    public void OnCoinCollected(Coin coin)
    {
        activeCoins.Remove(coin);
        Invoke(nameof(SpawnOne), respawnDelay);
    }

    void SpawnOne()
    {
        if (coinPrefab == null) return;

        for (int tries = 0; tries < 25; tries++)
        {
            Vector2 rnd = Random.insideUnitCircle * spawnRadius;
            Vector3 rayStart = areaCenter + new Vector3(rnd.x, 50f, rnd.y);

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 120f))
                continue;

            if (cat != null && Vector3.Distance(hit.point, cat.position) < minDistanceFromCat)
                continue;

            bool tooClose = false;
            foreach (var c in activeCoins)
            {
                if (c == null) continue;
                if (Vector3.Distance(c.transform.position, hit.point) < 1.5f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            GameObject go = Instantiate(coinPrefab, hit.point + Vector3.up * 0.5f, coinPrefab.transform.rotation);
            Coin coin = go.GetComponent<Coin>();
            if (coin != null) activeCoins.Add(coin);
            return;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(areaCenter, spawnRadius);
    }
}
