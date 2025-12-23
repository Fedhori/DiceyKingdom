using UnityEngine;

public sealed class SellOverlayController : MonoBehaviour
{
    public static SellOverlayController Instance { get; private set; }

    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Canvas canvas;

    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public void Show()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    public void Hide()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public bool ContainsScreenPoint(Vector2 screenPos)
    {
        var rt = overlayRoot.transform as RectTransform;
        if (rt == null)
            return false;

        var cv = canvas != null ? canvas : rt.GetComponentInParent<Canvas>();
        if (cv == null)
            return false;

        Camera cam = null;
        if (cv.renderMode is RenderMode.ScreenSpaceCamera or RenderMode.WorldSpace)
            cam = cv.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }
}
