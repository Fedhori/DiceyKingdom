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

    public RectTransform RootRect => rootRect;

    public void BindOrchestrator(GameTurnOrchestrator orchestrator)
    {
        if (dropTarget == null)
        {
            Debug.LogWarning("[SituationController] dropTarget is not assigned.", this);
            return;
        }

        dropTarget.SetOrchestrator(orchestrator);
    }

    public void BindSituation(string situationInstanceId)
    {
        if (dropTarget == null)
        {
            Debug.LogWarning("[SituationController] dropTarget is not assigned.", this);
            return;
        }

        dropTarget.SetSituationInstanceId(situationInstanceId);
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
        Color hintColor)
    {
        if (slotText != null)
            slotText.text = slotLabel ?? string.Empty;
        if (nameText != null)
            nameText.text = nameLabel ?? string.Empty;
        if (requirementText != null)
        {
            requirementText.text = requirementLabel ?? string.Empty;
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
    }
}
