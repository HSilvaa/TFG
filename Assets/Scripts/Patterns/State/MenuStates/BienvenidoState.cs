using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

public class BienvenidoState : AbstractMenuState
{
    public Button bienvenido;
    public List<Canvas> canvases;  // Lista de Canvas para el fade-in
    public float fadeDuration = 2f;  // Duración del fade-in

    private Dictionary<Canvas, Vector3> originalScales = new();

    public BienvenidoState(IMenuState menu) : base(menu)
    {
    }

    public override async void Enter()
    {
        bienvenido = GameObject.Find("BienvenidoThings").GetComponent<Button>();

        bienvenido.transform.SetAsLastSibling();

        bienvenido.interactable = false;
        //canvases = menu.GetCanvases();

        TransicionEnter();

        bienvenido.interactable = true;

        bienvenido.onClick.AddListener(() =>
        {
            bienvenido.interactable = false;
            
            TransicionExit();
        });
    }

    public override void Exit()
    {
        bienvenido.gameObject.SetActive(false);
    }

    public override void FixedUpdate()
    {
    }

    public override async void TransicionEnter()
    {
        //Dictionary<Canvas, Tween> scaleTweens = new Dictionary<Canvas, Tween>();

        //// Guardar escalas originales y poner a escala 0
        //foreach (Canvas canvas in canvases)
        //{
        //    originalScales[canvas] = canvas.transform.localScale;
        //    canvas.transform.localScale = Vector3.zero;
        //}

        //// Crear animaciones y guardarlas
        //foreach (Canvas canvas in canvases)
        //{
        //    Tween scaleTween = canvas.transform.DOScale(originalScales[canvas], fadeDuration)
        //        .SetEase(Ease.InOutSine);
        //    scaleTweens[canvas] = scaleTween;
        //}

        //// Esperar a que todas las animaciones terminen
        //foreach (var tween in scaleTweens.Values)
        //{
        //    await tween.AsyncWaitForCompletion();
        //}

        // Empezar la animación palpitante una vez todo esté animado
        bienvenido.transform.DOScale(0.70f, 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);


    }
    public override async void TransicionExit()
    {
        Transform bienvenidoTrans = bienvenido.transform;

        Sequence exitSequence = DOTween.Sequence();

        exitSequence.Append(bienvenidoTrans.DOScale(Vector3.zero, fadeDuration).SetEase(Ease.InOutSine));

        await exitSequence.AsyncWaitForCompletion();

        menu.SetState(new LoaderState(menu));
    }
    public override void Update()
    {

    }
}
