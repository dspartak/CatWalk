using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Вращение и парение")]
    public float rotateSpeed = 90f;
    public float bobAmplitude = 0.12f;
    public float bobSpeed = 3f;

    [Header("Награда")]
    public int value = 1;

    [Header("Эффекты (опционально)")]
    public GameObject pickupEffectPrefab;
    public AudioClip pickupSound;

    private float baseY;
    private bool collected;

    void Start()
    {
        baseY = transform.position.y;
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        if (bobAmplitude > 0f)
        {
            Vector3 p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = p;
        }

        // Надёжная проверка дистанции (гарантирует сбор даже без Rigidbody)
        if (!collected)
        {
            GameObject cat = GameObject.FindGameObjectWithTag("Player");
            if (cat != null && Vector3.Distance(cat.transform.position, transform.position) < 0.9f)
            {
                Collect();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.Add(value);

        if (pickupEffectPrefab != null)
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (CoinSpawner.Instance != null)
            CoinSpawner.Instance.OnCoinCollected(this);

        Destroy(gameObject);
    }
}
