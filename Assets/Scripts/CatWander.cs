using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CatWander : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 6f;
    public float wanderRadius = 10f;
    public float arriveDistance = 0.5f;

    [Header("Паузы")]
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;

    [Header("Покачивание")]
    public Transform body;
    public float bobHeight = 0.05f;
    public float bobSpeed = 9f;

    private CharacterController controller;
    private Vector3 homePoint;
    private Vector3 targetPoint;
    private float idleTimer;
    private bool isWalking;
    private bool isManual;
    private float bobTimer;
    private Vector3 initialBodyLocalPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        homePoint = transform.position;
        if (body != null) initialBodyLocalPos = body.localPosition;
        PickNewTarget();
    }

    void Update()
    {
        if (isWalking) Walk();
        else Idle();
    }

    public void SetManualTarget(Vector3 point)
    {
        targetPoint = point;
        isWalking = true;
        isManual = true;
        idleTimer = 0f;
    }

    void Walk()
    {
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0f;

        if (direction.magnitude < arriveDistance)
        {
            isWalking = false;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            if (isManual) { homePoint = transform.position; isManual = false; }
            if (body != null) body.localPosition = initialBodyLocalPos;
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        controller.Move(direction.normalized * moveSpeed * Time.deltaTime);

        if (body != null)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            Vector3 p = initialBodyLocalPos;
            p.y += Mathf.Abs(Mathf.Sin(bobTimer)) * bobHeight;
            body.localPosition = p;
        }
    }

    void Idle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f) PickNewTarget();
    }

    void PickNewTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = homePoint + new Vector3(rnd.x, 0, rnd.y);
        if (Physics.Raycast(candidate + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            targetPoint = hit.point;
        else
            targetPoint = candidate;
        isWalking = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? homePoint : transform.position, wanderRadius);
    }
}
