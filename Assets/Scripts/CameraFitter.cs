using UnityEngine;

[ExecuteAlways]
public sealed class CameraFitter : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float boardSize = 1080f; // world units
    [SerializeField] float margin = 0f;       // extra padding in world units

    [SerializeField] float landscapeSize = 960f;

    float lastAspect = -1f;

    void Reset() => cam = GetComponent<Camera>();

    void Awake()
    {
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Apply();
    }
#endif

    void Apply()
    {
        if (cam == null || !cam.orthographic) return;

        float aspect = (float)Screen.width / Screen.height;
        if (Mathf.Approximately(aspect, lastAspect)) return;
        lastAspect = aspect;

        // Landscape: width > height => fixed 960
        if (aspect > 1f)
        {
            cam.orthographicSize = landscapeSize;
            return;
        }

        // Portrait (or square): fit board
        float half = boardSize * 0.5f + margin;
        float sizeByHeight = half;
        float sizeByWidth = half / Mathf.Max(0.0001f, aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}