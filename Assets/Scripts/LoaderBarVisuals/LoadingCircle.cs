using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
    //REAL
    //[Tooltip("Radio del círculo")]
    //public float radius = 100f;

    //[Tooltip("Centro del círculo (opcional). Si está vacío, se usa el transform actual.")]
    //public Transform centerPoint;

    //[Tooltip("Rotar cada pila hacia afuera del círculo")]
    //public bool rotateToFaceOutwards = false;

    //void Start()
    //{
    //    ArrangeInCircle();
    //}

    //public void ArrangeInCircle()
    //{
    //    int count = transform.childCount;
    //    if (count == 0) return;

    //    Vector3 center = centerPoint != null ? centerPoint.position : transform.position;

    //    for (int i = 0; i < count; i++)
    //    {
    //        Transform pila = transform.GetChild(i);

    //        // Calcular ángulo
    //        float angle = (360f / count) * i;
    //        float rad = angle * Mathf.Deg2Rad;

    //        // Calcular nueva posición
    //        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
    //        pila.localPosition = offset;

    //        // Rotar si se desea
    //        if (rotateToFaceOutwards)
    //        {
    //            pila.right = offset.normalized; // Ajusta esto si prefieres que miren hacia dentro
    //        }
    //    }
    //}

    //PRODUCCIÓN
    [Header("Configuración del círculo")]
    [Tooltip("Radio del círculo")]
    public float radius = 100f;

    [Tooltip("Centro del círculo (opcional). Si está vacío, se usa el transform actual.")]
    public Transform centerPoint;

    [Tooltip("¿Rotar cada pila para que su eje Y apunte al centro?")]
    public bool rotateYToFaceCenter = true;

    [ContextMenu("Actualizar distribución")]
    public void ArrangeInCircle()
    {
        int count = transform.childCount;
        if (count == 0) return;

        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            Transform pila = transform.GetChild(i);

            // Calcular posición circular
            float angle = (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;

            pila.localPosition = offset;

            // Rotar eje Y hacia el centro
            if (rotateYToFaceCenter)
            {
                Vector3 dirToCenter = (center - pila.position).normalized;
                float angleToCenter = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg;

                // Ajuste: para que sea el eje Y el que apunte (no el eje X)
                pila.localRotation = Quaternion.Euler(0, 0, angleToCenter + 90f);
            }
        }
    }

    void OnValidate()
    {
        ArrangeInCircle();
    }
}
