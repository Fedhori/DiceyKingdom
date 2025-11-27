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

    [Header("Screen Space")]
    [SerializeField] private float screenSpaceHeightOffset = 30f;

    readonly Queue<FloatingText> pool = new Queue<FloatingText>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void ShowText(
        string message,
        Color color,
        float fontSize,
        float lifeTime,
        Vector3 worldPosition)
    {
        if (textPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] textPrefab is not assigned.");
            return;
        }

        var floatingText = GetFromPool();
        if (floatingText == null)
            return;

        Vector3 startPosition =
            WorldToCanvasPosition(worldPosition) +
            new Vector3(0f, screenSpaceHeightOffset, 0f);
        
        floatingText.Bind(
            message,
            color,
            fontSize,
            lifeTime,
            startPosition,
            () => ReturnToPool(floatingText));
    }

    FloatingText GetFromPool()
    {
        FloatingText floatingText;

        if (pool.Count > 0)
        {
            floatingText = pool.Dequeue();
            if (floatingText != null)
                floatingText.gameObject.SetActive(true);
        }
        else
        {
            Transform parent = GetParent();
            GameObject instance = Instantiate(textPrefab, parent);
            floatingText = instance.GetComponent<FloatingText>();

            if (floatingText == null)
            {
                Debug.LogError("[FloatingTextManager] textPrefab has no FloatingText component.");
                Destroy(instance);
                return null;
            }
        }

        Transform targetParent = GetParent();
        if (targetParent != null)
            floatingText.transform.SetParent(targetParent, false);
        else
            floatingText.transform.SetParent(null, true);

        return floatingText;
    }

    void ReturnToPool(FloatingText text)
    {
        if (text == null)
            return;

        text.gameObject.SetActive(false);
        pool.Enqueue(text);
    }

    Transform GetParent()
    {
        if (container != null)
            return container.transform;

        if (parentCanvas != null)
            return parentCanvas.transform;

        return null;
    }

    Vector3 WorldToCanvasPosition(Vector3 worldPosition)
    {
        if (parentCanvas == null)
            return worldPosition;

        var canvasRect = parentCanvas.transform as RectTransform;
        if (canvasRect == null)
            return worldPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Camera.main.WorldToScreenPoint(worldPosition),
            parentCanvas.worldCamera,
            out Vector2 localPoint);

        return localPoint;
    }
}
