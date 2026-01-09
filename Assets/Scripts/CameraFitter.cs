using UnityEngine;

[ExecuteAlways]
public sealed class CameraFitter : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform playArea;
    [SerializeField] float horizontalMargin = 0f; // extra padding in world units
    [SerializeField] float verticalMargin = 0f;

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
        if (playArea == null)
        {
            Debug.LogWarning("[CameraFitter] PlayArea is not assigned.");
            return;
        }

        var spriteRenderer = playArea.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[CameraFitter] PlayArea SpriteRenderer not found.");
            return;
        }

        float aspect = (float)Screen.width / Screen.height;
        var bounds = spriteRenderer.bounds;
        float halfHeight = bounds.size.y * 0.5f + verticalMargin;
        float halfWidth = bounds.size.x * 0.5f + horizontalMargin;

        float sizeByHeight = halfHeight;
        float sizeByWidth = halfWidth / Mathf.Max(0.0001f, aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
