using Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class TokenManager : MonoBehaviour
{
    public static TokenManager Instance { get; private set; }

    [SerializeField] Transform slotContainer;
    TokenController[] slotControllers;

    TokenController draggingController;
    int draggingStartIndex = -1;
    int currentHighlightIndex = -1;

    [SerializeField] RectTransform dragGhostRect;
    [SerializeField] Image dragGhostImage;
    [SerializeField] Color slotHighlightColor = Color.white;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheSlotControllers();
        InitializeSlots();
    }

    void CacheSlotControllers()
    {
        if (slotContainer == null)
        {
            slotControllers = GetComponentsInChildren<TokenController>(true);
            return;
        }

        slotControllers = slotContainer.GetComponentsInChildren<TokenController>(true);
    }

    void InitializeSlots()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            ctrl.SetSlotIndex(i);
        }
    }

    public bool TryAddTokenAt(string tokenId, int slotIndex, out TokenInstance instance)
    {
        instance = null;

        if (!IsValidIndex(slotIndex))
        {
            Debug.LogWarning($"[TokenManager] Invalid slot index: {slotIndex}");
            return false;
        }

        var controller = slotControllers[slotIndex];
        if (controller == null)
        {
            Debug.LogWarning($"[TokenManager] Slot {slotIndex} has no controller.");
            return false;
        }

        if (!TokenRepository.IsInitialized)
        {
            Debug.LogWarning("[TokenManager] TokenRepository not initialized.");
            return false;
        }

        if (!TokenRepository.TryGet(tokenId, out var dto) || dto == null)
        {
            Debug.LogWarning($"[TokenManager] Token id not found: {tokenId}");
            return false;
        }

        if (controller.Instance != null)
        {
            Debug.LogWarning($"[TokenManager] Slot {slotIndex} is not empty.");
            return false;
        }

        instance = new TokenInstance(dto);
        controller.Bind(instance);
        ClearHighlights();
        return true;
    }

    public int SlotCount => slotControllers != null ? slotControllers.Length : 0;

    public bool TryGetFirstEmptySlot(out int index)
    {
        index = -1;
        if (slotControllers == null)
            return false;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            if (slotControllers[i] != null && slotControllers[i].Instance == null)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
            return false;

        return slotControllers[slotIndex]?.Instance == null;
    }
    
    public bool TryGetSlotFromScreenPos(Vector2 screenPos, out int slotIndex)
    {
        slotIndex = -1;
        if (slotControllers == null)
            return false;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null || ctrl.RectTransform == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(ctrl.RectTransform, screenPos))
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    public void HighlightEmptySlots()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            bool empty = ctrl.Instance == null;
            ctrl.SetHighlight(empty, slotHighlightColor);
        }
    }

    public void ClearHighlights()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            ctrl.SetHighlight(false, slotHighlightColor);
        }

        currentHighlightIndex = -1;
    }

    public bool BeginDrag(TokenController controller)
    {
        if (controller == null)
            return false;

        int idx = controller.SlotIndex;
        if (controller.Instance == null)
            return false;

        draggingController = controller;
        draggingStartIndex = idx;
        ShowDragGhost(controller.GetIconSprite(), controller.RectTransform.position);
        controller.SetIconVisible(false);
        return true;
    }

    public void EndDrag(TokenController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
        {
            ResetDrag();
            return;
        }

        int targetIndex = FindNearestSlotIndex(screenPos);
        if (targetIndex >= 0 && targetIndex != draggingStartIndex)
            SwapControllers(draggingStartIndex, targetIndex);

        HideDragGhost();
        ClearHighlight();
        draggingController?.SetIconVisible(true);

        ResetDrag();
    }

    int FindNearestSlotIndex(Vector2 screenPos)
    {
        if (slotControllers == null || slotControllers.Length == 0)
            return -1;

        float bestDist = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            var rt = ctrl.RectTransform;
            if (rt == null)
                continue;

            Vector3 worldPos = rt.position;
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            float dist = Vector2.SqrMagnitude(slotScreenPos - screenPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public void UpdateDrag(TokenController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
            return;

        if (dragGhostRect != null)
            dragGhostRect.position = screenPos;

        int targetIndex = FindNearestSlotIndex(screenPos);
        UpdateHighlight(targetIndex);
    }

    void SwapControllers(int indexA, int indexB)
    {
        if (slotControllers == null)
            return;

        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            return;

        var ctrlA = slotControllers[indexA];
        var ctrlB = slotControllers[indexB];

        if (ctrlA == null || ctrlB == null)
            return;

        var temp = ctrlA.Instance;
        ctrlA.Bind(ctrlB.Instance);
        ctrlB.Bind(temp);
    }

    void ResetDrag()
    {
        draggingController = null;
        draggingStartIndex = -1;
    }

    public void TriggerTokens(TokenTriggerType trigger)
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
            slotControllers[i]?.Instance?.HandleTrigger(trigger);
    }

    public bool HasToken(string tokenId)
    {
        if (string.IsNullOrEmpty(tokenId) || slotControllers == null)
            return false;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var inst = slotControllers[i]?.Instance;
            if (inst != null && inst.Id == tokenId)
                return true;
        }

        return false;
    }

    public void CollectOwnedTokenIds(System.Collections.Generic.HashSet<string> set)
    {
        if (set == null)
            return;

        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var inst = slotControllers[i]?.Instance;
            if (inst == null || string.IsNullOrEmpty(inst.Id))
                continue;

            set.Add(inst.Id);
        }
    }

    bool IsValidIndex(int index)
    {
        return slotControllers != null && index >= 0 && index < slotControllers.Length;
    }

    void ShowDragGhost(Sprite sprite, Vector2 screenPos)
    {
        if (dragGhostRect == null || dragGhostImage == null)
            return;

        dragGhostImage.sprite = sprite;
        dragGhostImage.enabled = sprite != null;
        dragGhostRect.gameObject.SetActive(true);
        dragGhostRect.position = screenPos;
    }

    void HideDragGhost()
    {
        if (dragGhostRect == null)
            return;

        dragGhostRect.gameObject.SetActive(false);
    }

    void UpdateHighlight(int targetIndex)
    {
        if (currentHighlightIndex == targetIndex)
            return;

        ClearHighlight();

        if (!IsValidIndex(targetIndex))
            return;

        var ctrl = slotControllers[targetIndex];
        if (ctrl == null)
            return;

        ctrl.SetHighlight(true, slotHighlightColor);
        currentHighlightIndex = targetIndex;
    }

    void ClearHighlight()
    {
        if (IsValidIndex(currentHighlightIndex))
        {
            var ctrl = slotControllers[currentHighlightIndex];
            if (ctrl != null)
                ctrl.SetHighlight(false, slotHighlightColor);
        }

        currentHighlightIndex = -1;
    }
}
