using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class PersistentCanvas : MonoBehaviour
{
    [Header("Target Camera by Tag")]
    [SerializeField] private string targetCameraTag = "UICamera"; // ← 여기에 붙을 카메라 태그 지정
    [SerializeField] private bool retryNextFrameIfMissing = true; // 씬 로드시 늦게 생기는 카메라 대비

    private Canvas canvas;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        canvas = GetComponent<Canvas>();

        // 처음 씬에서도 시도
        AttachByTag();

        // 씬 변경/로드마다 다시 붙이기
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnActiveSceneChanged(Scene _, Scene __) => AttachByTag();
    private void OnSceneLoaded(Scene _, LoadSceneMode __) => AttachByTag();

    private void AttachByTag()
    {
        var cam = FindTaggedCamera(targetCameraTag);

        if (cam == null)
        {
            if (retryNextFrameIfMissing)
                // 씬 내 카메라 초기화 순서 보정
                StartCoroutine(AttachNextFrame());
            else
                Debug.LogWarning($"[PersistentCanvas] Tag '{targetCameraTag}' 카메라를 찾지 못했습니다.", this);
            return;
        }

        canvas.worldCamera = cam;
    }

    private IEnumerator AttachNextFrame()
    {
        yield return null;
        var cam = FindTaggedCamera(targetCameraTag) ?? Camera.main;
        if (cam != null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvas.worldCamera = cam;
    }

    private static Camera FindTaggedCamera(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Camera.main;

        // 태그가 정의 안되어 있으면 예외가 날 수 있으니 안전하게 전체 검색 후 Tag 비교
        var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
            if (c != null && c.CompareTag(tag) && c.isActiveAndEnabled)
                return c;
        return null;
    }
}
