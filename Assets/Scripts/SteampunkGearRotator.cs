using UnityEngine;

public class SteampunkGearRotator : MonoBehaviour
{
    [Tooltip("Скорость вращения в градусах в секунду (плюс или минус для направления)")]
    public float rotationSpeed = 35f;

    [Tooltip("Ось вращения")]
    public Vector3 rotationAxis = Vector3.forward;

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }
}
