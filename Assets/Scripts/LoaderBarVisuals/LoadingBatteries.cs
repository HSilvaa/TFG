using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingBatteries : MonoBehaviour
{
    public List<GameObject> batteries = new List<GameObject>();
    private int currentPila = -1;

    [Tooltip("Velocidad del parpadeo (en segundos)")]
    public float blinkInterval = 0.2f;

    public TMP_Text percentageText;
    private float currentPercent = 0f;

    public float totalChargeTime = 70f; // Tiempo total para encender todas las pilas

    public bool forceComplete = false;
    private bool isCharging = false;

    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject pila = transform.GetChild(i).gameObject;
            pila.SetActive(false);
            batteries.Add(pila);
        }

        isCharging = true;
        StartCoroutine(ShowNextBattery());
    }

    private IEnumerator ShowNextBattery()
    {
        currentPila++;

        if (currentPila >= batteries.Count || forceComplete)
            yield break;

        GameObject pila = batteries[currentPila];
        float duration = totalChargeTime / batteries.Count;
        float timer = 0f;
        bool visible = false;

        float percentToAdd = 100f / batteries.Count;
        float percentPerSecond = percentToAdd / duration;

        while (timer < duration && !forceComplete)
        {
            visible = !visible;
            pila.SetActive(visible);

            float deltaTime = blinkInterval;
            timer += deltaTime;
            currentPercent += percentPerSecond * deltaTime;

            percentageText.text = Mathf.Clamp(Mathf.RoundToInt(currentPercent), 0, 100).ToString();

            yield return new WaitForSeconds(deltaTime);
        }

        pila.SetActive(true);

        currentPercent = (currentPila + 1) * percentToAdd;
        percentageText.text = Mathf.Clamp(Mathf.RoundToInt(currentPercent), 0, 100).ToString();

        yield return new WaitForSeconds(0.3f);
        StartCoroutine(ShowNextBattery());
    }

    public void ActivarCargaCompleta()
    {
        if (!isCharging) return;

        forceComplete = true;
        StopAllCoroutines();
        StartCoroutine(CompletarCarga());
    }

    private IEnumerator CompletarCarga()
    {
        int total = batteries.Count;
        float delay = 0.05f; // Delay corto para animación progresiva
        float percentPerBattery = 100f / total;

        for (int i = 0; i < total; i++)
        {
            batteries[i].SetActive(true);
            currentPercent = (i + 1) * percentPerBattery;
            percentageText.text = Mathf.Clamp(Mathf.RoundToInt(currentPercent), 0, 100).ToString();
            yield return new WaitForSeconds(delay);
        }

        // Asegurar que está al 100% exacto
        currentPercent = 100f;
        percentageText.text = "100";

        yield return new WaitForSeconds(3f);

        // Gestionar Cambios
    }
}
