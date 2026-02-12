using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SituationController : MonoBehaviour
{
    [SerializeField] RectTransform rootRect;
    [SerializeField] Image backgroundImage;
    [SerializeField] EnemyDropTarget dropTarget;
    [SerializeField] TextMeshProUGUI slotText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI requirementText;
    [SerializeField] TextMeshProUGUI successText;
    [SerializeField] TextMeshProUGUI failureText;
    [SerializeField] TextMeshProUGUI deadlineText;
    [SerializeField] TextMeshProUGUI targetHintText;
    [SerializeField] RectTransform diceRowRoot;
    [SerializeField] GameObject dicePrefab;

    GameTurnOrchestrator orchestrator;
    string situationInstanceId = string.Empty;
    readonly List<DiceFaceWidgets> diceFaces = new();

    public RectTransform RootRect => rootRect;
    public string SituationInstanceId => situationInstanceId;

    public void SetDicePrefab(GameObject prefab)
    {
        dicePrefab = prefab;
    }

    public void BindOrchestrator(GameTurnOrchestrator value)
    {
        orchestrator = value;

        if (dropTarget == null)
        {
            Debug.LogWarning("[SituationController] dropTarget is not assigned.", this);
            return;
        }

        dropTarget.SetOrchestrator(value);
    }

    public void BindSituation(string value)
    {
        situationInstanceId = value ?? string.Empty;

        if (dropTarget == null)
        {
            Debug.LogWarning("[SituationController] dropTarget is not assigned.", this);
            return;
        }

        dropTarget.SetSituationInstanceId(situationInstanceId);
    }

    public void OnSituationDiePressed(int dieIndex)
    {
        if (orchestrator == null)
            return;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return;
        if (dieIndex < 0)
            return;

        orchestrator.TryTestAgainstSituationDie(situationInstanceId, dieIndex);
    }

    public void Render(
        string slotLabel,
        string nameLabel,
        string requirementLabel,
        string successLabel,
        string failureLabel,
        string deadlineLabel,
        string targetHintLabel,
        Color backgroundColor,
        Color requirementColor,
        Color successColor,
        Color failureColor,
        Color hintColor,
        IReadOnlyList<int> remainingDiceFaces)
    {
        if (slotText != null)
            slotText.text = slotLabel ?? string.Empty;
        if (nameText != null)
            nameText.text = nameLabel ?? string.Empty;
        if (requirementText != null)
        {
            requirementText.text = dicePrefab != null
                ? string.Empty
                : (requirementLabel ?? string.Empty);
            requirementText.color = requirementColor;
        }

        if (successText != null)
        {
            successText.text = successLabel ?? string.Empty;
            successText.color = successColor;
        }

        if (failureText != null)
        {
            failureText.text = failureLabel ?? string.Empty;
            failureText.color = failureColor;
        }

        if (deadlineText != null)
            deadlineText.text = deadlineLabel ?? string.Empty;
        if (targetHintText != null)
        {
            targetHintText.text = targetHintLabel ?? string.Empty;
            targetHintText.color = hintColor;
        }

        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        RefreshDiceFaces(remainingDiceFaces);
    }

    public void PlayDuelRollEffect(int dieIndex, int dieFace, int finalRoll, bool isDestroyedByAgent)
    {
        if (dieIndex < 0 || dieIndex >= diceFaces.Count)
            return;

        var face = diceFaces[dieIndex];
        if (face?.view == null)
            return;

        face.view.PlayRollEffect(
            Math.Max(2, dieFace),
            Mathf.Max(1, finalRoll),
            !isDestroyedByAgent);
    }

    void RefreshDiceFaces(IReadOnlyList<int> remainingDiceFaces)
    {
        EnsureDiceRowRoot();
        if (diceRowRoot == null)
            return;

        int targetCount = Mathf.Max(0, remainingDiceFaces?.Count ?? 0);
        EnsureDiceFaceCount(targetCount);

        for (int index = 0; index < diceFaces.Count; index++)
        {
            var face = diceFaces[index];
            if (face == null || face.view == null)
                continue;

            if (face.clickTarget != null)
                face.clickTarget.Bind(this, index);
            if (face.view.IsRolling)
                continue;

            int dieFace = remainingDiceFaces[index];
            face.view.SetLabel($"d{dieFace}");
        }
    }

    void EnsureDiceRowRoot()
    {
        if (diceRowRoot != null)
            return;
        if (requirementText == null)
            return;

        var requirementRect = requirementText.rectTransform;
        if (requirementRect == null || requirementRect.parent == null)
            return;

        var rowObject = new GameObject(
            "SituationDiceRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        rowObject.layer = LayerMask.NameToLayer("UI");

        diceRowRoot = rowObject.GetComponent<RectTransform>();
        diceRowRoot.SetParent(requirementRect.parent, false);
        diceRowRoot.anchorMin = requirementRect.anchorMin;
        diceRowRoot.anchorMax = requirementRect.anchorMax;
        diceRowRoot.pivot = requirementRect.pivot;
        diceRowRoot.anchoredPosition = requirementRect.anchoredPosition;
        diceRowRoot.sizeDelta = requirementRect.sizeDelta;

        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;
    }

    void EnsureDiceFaceCount(int targetCount)
    {
        while (diceFaces.Count > targetCount)
        {
            int lastIndex = diceFaces.Count - 1;
            var face = diceFaces[lastIndex];
            if (face.root != null)
            {
                if (Application.isPlaying)
                    Destroy(face.root.gameObject);
                else
                    DestroyImmediate(face.root.gameObject);
            }

            diceFaces.RemoveAt(lastIndex);
        }

        while (diceFaces.Count < targetCount)
        {
            var face = CreateDiceFace(diceFaces.Count);
            if (face == null)
                return;

            diceFaces.Add(face);
        }
    }

    DiceFaceWidgets CreateDiceFace(int index)
    {
        if (dicePrefab == null)
        {
            Debug.LogWarning("[SituationController] dicePrefab is not assigned.", this);
            return null;
        }
        if (diceRowRoot == null)
        {
            Debug.LogWarning("[SituationController] diceRowRoot is not assigned.", this);
            return null;
        }

        var root = Instantiate(dicePrefab, diceRowRoot, false);
        root.name = $"SituationDice_{index + 1}";

        var rootTransform = root.GetComponent<RectTransform>();
        if (rootTransform == null)
        {
            Debug.LogWarning("[SituationController] Dice prefab must include RectTransform.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        var view = root.GetComponent<DiceFaceView>();
        if (view == null)
        {
            Debug.LogWarning("[SituationController] Dice prefab requires DiceFaceView.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        var clickTarget = root.GetComponent<SituationDiceFaceClickTarget>();
        if (clickTarget == null)
            clickTarget = root.AddComponent<SituationDiceFaceClickTarget>();
        clickTarget.Bind(this, index);

        return new DiceFaceWidgets
        {
            root = rootTransform,
            view = view,
            clickTarget = clickTarget
        };
    }

    sealed class DiceFaceWidgets
    {
        public RectTransform root;
        public DiceFaceView view;
        public SituationDiceFaceClickTarget clickTarget;
    }
}
