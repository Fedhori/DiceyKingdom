using System;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Prefab & Parents")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject container;
    [SerializeField] private Transform worldSpaceContainer;

    [Header("Defaults")]
    [SerializeField] private bool defaultWorldSpace = true;
    [SerializeField] private float worldSpaceHeightOffset = 1.5f;
    [SerializeField] private float screenSpaceHeightOffset = 30f;

    [Header("Scaling")]
    [SerializeField] private float minFontSize = 8f;
    [SerializeField] private float maxFontSize = 48f;
    [SerializeField] private float minValue = 1f;
    [SerializeField] private float maxValue = 1000f;

    private readonly Queue<FloatingText> screenPool = new Queue<FloatingText>();
    private readonly Queue<FloatingText> worldPool = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    public void ShowText(string message, Color color, float fontSize, float lifeTime, Vector3 worldPosition, bool? useWorldSpaceOverride = null)
    {
        bool useWorldSpace = useWorldSpaceOverride ?? defaultWorldSpace;
        if (textPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] textPrefab is not assigned.");
            return;
        }

        FloatingText floatingText = GetFromPool(useWorldSpace);
        if (floatingText == null) return;

        Vector3 startPosition = useWorldSpace
            ? worldPosition + Vector3.up * worldSpaceHeightOffset
            : WorldToCanvasPosition(worldPosition) + new Vector3(0f, screenSpaceHeightOffset, 0f);

        floatingText.Bind(message, color, fontSize, lifeTime, startPosition, useWorldSpace,
            () => ReturnToPool(floatingText, useWorldSpace));
    }

    public void ShowScreenSpaceText(string message, Color color, float fontSize, float lifeTime, Vector2 anchoredPosition)
    {
        if (textPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] textPrefab is not assigned.");
            return;
        }

        FloatingText floatingText = GetFromPool(false);
        if (floatingText == null) return;

        floatingText.Bind(message, color, fontSize, lifeTime, anchoredPosition, false,
            () => ReturnToPool(floatingText, false));
    }

    // public void ShowMoneyText(int amount)
    // {
    //     if (UIManager.Instance == null) return;
    //
    //     Vector3 worldPos = UIManager.Instance.moneyText.transform.position;
    //     ShowText($"+${amount}", Colors.Currency, 32f, 2f, worldPos, useWorldSpaceOverride: false);
    // }

    private FloatingText GetFromPool(bool useWorldSpace)
    {
        Queue<FloatingText> pool = useWorldSpace ? worldPool : screenPool;
        FloatingText floatingText;
        if (pool.Count > 0)
        {
            floatingText = pool.Dequeue();
            if (floatingText != null) floatingText.gameObject.SetActive(true);
        }
        else
        {
            Transform parent = GetParent(useWorldSpace);
            GameObject instance = Instantiate(textPrefab, parent);
            floatingText = instance.GetComponent<FloatingText>();
        }

        if (floatingText == null) return null;

        Transform targetParent = GetParent(useWorldSpace);
        if (targetParent != null)
            floatingText.transform.SetParent(targetParent, false);
        else
            floatingText.transform.SetParent(null, true);

        return floatingText;
    }

    private void ReturnToPool(FloatingText text, bool useWorldSpace)
    {
        if (text == null) return;

        text.gameObject.SetActive(false);
        Queue<FloatingText> pool = useWorldSpace ? worldPool : screenPool;
        pool.Enqueue(text);
    }

    private Transform GetParent(bool useWorldSpace)
    {
        if (useWorldSpace)
        {
            if (worldSpaceContainer != null) return worldSpaceContainer;
            return null;
        }

        if (container != null) return container.transform;

        if (parentCanvas != null) return parentCanvas.transform;

        return null;
    }

    private Vector3 WorldToCanvasPosition(Vector3 worldPosition)
    {
        if (parentCanvas == null) return worldPosition;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        if (canvasRect == null) return worldPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Camera.main.WorldToScreenPoint(worldPosition),
            parentCanvas.worldCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    public float CalculateFontSize(int value)
    {
        float clampedValue = Mathf.Max(minValue, value);
        if (maxValue <= minValue) return minFontSize;

        float logMin = Mathf.Log10(minValue);
        float logMax = Mathf.Log10(maxValue);
        float logCurrent = Mathf.Log10(clampedValue);
        float t = Mathf.InverseLerp(logMin, logMax, logCurrent);
        return Mathf.Lerp(minFontSize, maxFontSize, t);
    }
}

