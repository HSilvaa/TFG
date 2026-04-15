using UnityEngine;

public class RotationLoader : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 300f;
    private bool isRotating = false;

    private void OnEnable()
    {
        isRotating = true;
    }

    private void OnDisable()
    {
        isRotating = false;
    }

    void Update()
    {
        if (isRotating)
        {
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }
}