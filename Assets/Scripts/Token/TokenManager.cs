using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class TokenManager : MonoBehaviour
{
    public static TokenManager Instance { get; private set; }

    [SerializeField] Transform slotContainer;
    [SerializeField] TokenController slotPrefab;
    TokenController[] slotControllers;

    TokenController draggingController;
    int draggingStartIndex = -1;
    int currentHighlightIndex = -1;
    bool overSellArea;

    [SerializeField] Color slotHighlightColor = Color.white;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildSlots();
    }

    void BuildSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[TokenManager] slotContainer or slotPrefab is not assigned.");
            slotControllers = System.Array.Empty<TokenController>();
            return;
        }

        // 기존 자식 슬롯 정리
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            var child = slotContainer.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }

        int slotCount = Mathf.Max(0, GameConfig.TokenSlotCount);
        slotControllers = new TokenController[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            var ctrl = Instantiate(slotPrefab, slotContainer);
            ctrl.SetSlotIndex(i);
            slotControllers[i] = ctrl;
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
            if (ctrl.dragHighlightMask != null)
                ctrl.dragHighlightMask.SetActive(empty);
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

            if (ctrl.dragHighlightMask != null)
                ctrl.dragHighlightMask.SetActive(false);
            ctrl.SetHighlight(false, slotHighlightColor);
        }

        currentHighlightIndex = -1;
    }

    public bool BeginDrag(TokenController controller, Vector2 screenPos)
    {
        if (controller == null)
            return false;

        var flow = FlowManager.Instance;
        if (flow != null && !flow.CanDragTokens)
            return false;

        int idx = controller.SlotIndex;
        if (controller.Instance == null)
            return false;

        draggingController = controller;
        draggingStartIndex = idx;
        GhostManager.Instance?.ShowGhost(controller.GetIconSprite(), screenPos, GhostKind.Token);
        controller.SetIconVisible(false);
        overSellArea = false;
        SellOverlayController.Instance?.Show();
        return true;
    }

    public void EndDrag(TokenController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
        {
            SellOverlayController.Instance?.Hide();
            ResetDrag();
            return;
        }

        var overlay = SellOverlayController.Instance;
        overSellArea = overlay != null && overlay.ContainsScreenPoint(screenPos);
        if (overSellArea)
        {
            GhostManager.Instance?.HideGhost(GhostKind.Token);
            ClearHighlights();
            draggingController?.SetIconVisible(true);
            overlay?.Hide();

            var toSell = draggingController;
            ResetDrag();
            RequestSellToken(toSell);
            return;
        }

        int targetIndex = FindSlotIndexAtScreenPos(screenPos);
        if (targetIndex >= 0 && targetIndex != draggingStartIndex)
            SwapControllers(draggingStartIndex, targetIndex);

        GhostManager.Instance?.HideGhost(GhostKind.Token);
        ClearHighlights();
        draggingController?.SetIconVisible(true);

        ResetDrag();
        overlay?.Hide();
    }

    int FindSlotIndexAtScreenPos(Vector2 screenPos)
    {
        if (slotControllers == null || slotControllers.Length == 0)
            return -1;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            var rt = ctrl.RectTransform;
            if (rt == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                return i;
        }

        return -1;
    }

    public void UpdateDrag(TokenController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
            return;

        GhostManager.Instance?.UpdateGhostPosition(screenPos);

        int targetIndex = FindSlotIndexAtScreenPos(screenPos);
        UpdateHighlight(targetIndex);

        var overlay = SellOverlayController.Instance;
        overSellArea = overlay != null && overlay.ContainsScreenPoint(screenPos);
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

        int siblingA = ctrlA.transform.GetSiblingIndex();
        int siblingB = ctrlB.transform.GetSiblingIndex();

        slotControllers[indexA] = ctrlB;
        slotControllers[indexB] = ctrlA;

        ctrlA.SetSlotIndex(indexB);
        ctrlB.SetSlotIndex(indexA);

        ctrlA.transform.SetSiblingIndex(siblingB);
        ctrlB.transform.SetSiblingIndex(siblingA);

    }

    void ResetDrag()
    {
        draggingController = null;
        draggingStartIndex = -1;
        overSellArea = false;
    }

    public void TriggerTokens(TokenTriggerType trigger)
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
            slotControllers[i]?.Instance?.HandleTrigger(trigger);
    }

    public void RequestSellToken(TokenController ctrl)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        int price = Mathf.FloorToInt(ctrl.Instance.Price / 2f);
        if (price <= 0)
            return;

        var args = new Dictionary<string, object>
        {
            ["pinName"] = LocalizationUtil.GetTokenName(ctrl.Instance.Id),
            ["value"] = price
        };

        ModalManager.Instance.ShowConfirmation(
            "modal",
            "modal.sellpin.title",
            "modal",
            "modal.sellpin.message",
            () => SellToken(ctrl, price),
            () => { },
            args);
    }

    void SellToken(TokenController ctrl, int price)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        CurrencyManager.Instance?.AddCurrency(price);
        RemoveToken(ctrl);
    }

    void RemoveToken(TokenController ctrl)
    {
        if (ctrl == null)
            return;

        ctrl.Bind(null);
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
        if (ctrl.dragHighlightMask != null)
            ctrl.dragHighlightMask.SetActive(true);
        currentHighlightIndex = targetIndex;
    }

    void ClearHighlight()
    {
        if (IsValidIndex(currentHighlightIndex))
        {
            var ctrl = slotControllers[currentHighlightIndex];
            if (ctrl != null)
            {
                ctrl.SetHighlight(false, slotHighlightColor);
                if (ctrl.dragHighlightMask != null)
                    ctrl.dragHighlightMask.SetActive(false);
            }
        }

        currentHighlightIndex = -1;
    }
}
