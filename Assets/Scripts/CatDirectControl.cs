using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CatDirectControl : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public Transform body;
    public float bobHeight = 0.05f;
    public float bobSpeed = 9f;

    CharacterController cc;
    Vector2 input;
    float bobTimer;
    Vector3 initialBodyLocalPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (body != null) initialBodyLocalPos = body.localPosition;
    }

    public void SetInput(Vector2 v) => input = v;

    void Update()
    {
        if (input.sqrMagnitude < 0.01f)
        {
            if (body != null) body.localPosition = initialBodyLocalPos;
            return;
        }

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        cc.Move(dir * moveSpeed * Time.deltaTime);

        if (body != null)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            Vector3 p = initialBodyLocalPos;
            p.y += Mathf.Abs(Mathf.Sin(bobTimer)) * bobHeight;
            body.localPosition = p;
        }
    }
}
