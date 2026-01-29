using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverlayFader : MonoBehaviour
{
    [SerializeField] private float fadeSeconds = 0.5f;
    [SerializeField] private bool deactivateOnHide = true;
    [SerializeField] private bool setActiveOnShow = true;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private float targetAlpha = -1f;

    private Coroutine fadeRoutine;
    private float cachedTargetAlpha = 1f;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
        if (targetGraphic != null)
        {
            cachedTargetAlpha = targetAlpha >= 0f ? Mathf.Clamp01(targetAlpha) : targetGraphic.color.a;
        }
    }

    public void Show(Action onComplete = null)
    {
        if (setActiveOnShow && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            SetAlpha(0f);
        }

        StartFade(cachedTargetAlpha, onComplete);
    }

    public void Hide(Action onComplete = null)
    {
        StartFade(0f, () =>
        {
            if (deactivateOnHide)
                gameObject.SetActive(false);

            onComplete?.Invoke();
        });
    }

    public void SetVisibleInstant(bool visible)
    {
        if (visible)
        {
            if (setActiveOnShow && !gameObject.activeSelf)
                gameObject.SetActive(true);
            SetAlpha(cachedTargetAlpha);
            return;
        }

        SetAlpha(0f);
        if (deactivateOnHide)
            gameObject.SetActive(false);
    }

    void StartFade(float targetAlpha, Action onComplete)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        float startAlpha = GetAlpha();
        float duration = Mathf.Max(0.0f, fadeSeconds);
        if (duration <= 0.0f)
        {
            SetAlpha(targetAlpha);
            onComplete?.Invoke();
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        onComplete?.Invoke();
        fadeRoutine = null;
    }

    float GetAlpha()
    {
        if (targetGraphic == null)
            return 1f;
        return targetGraphic.color.a;
    }

    void SetAlpha(float alpha)
    {
        if (targetGraphic == null)
            return;
        Color color = targetGraphic.color;
        color.a = alpha;
        targetGraphic.color = color;
    }
}
