using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class DuelOverlayController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] GameObject overlayRoot;
    [SerializeField] Image overlayBlockerImage;
    [SerializeField] DiceFaceView situationDieView;
    [SerializeField] DiceFaceView agentDieView;

    Coroutine duelRoutine;

    void Awake()
    {
        if (overlayRoot == null)
            overlayRoot = gameObject;
        if (overlayBlockerImage == null && overlayRoot != null)
            overlayBlockerImage = overlayRoot.GetComponent<Image>();

        SetOverlayVisible(false);
    }

    void OnEnable()
    {
        if (orchestrator != null)
        {
            orchestrator.DuelRollStarted -= OnDuelRollStarted;
            orchestrator.DuelRollStarted += OnDuelRollStarted;
        }
    }

    void OnDisable()
    {
        if (orchestrator != null)
            orchestrator.DuelRollStarted -= OnDuelRollStarted;

        if (duelRoutine != null)
        {
            StopCoroutine(duelRoutine);
            duelRoutine = null;
        }
    }

    void OnDuelRollStarted(GameTurnOrchestrator.DuelRollPresentation presentation)
    {
        if (duelRoutine != null)
            StopCoroutine(duelRoutine);

        SetOverlayVisible(false);
        duelRoutine = StartCoroutine(PlayDuelRoutine(presentation));
    }

    IEnumerator PlayDuelRoutine(GameTurnOrchestrator.DuelRollPresentation presentation)
    {
        SetOverlayVisible(true);

        if (situationDieView != null)
        {
            situationDieView.SetLabel(Mathf.Max(1, presentation.situationDieFace).ToString());
            situationDieView.PlayRollEffect(
                Mathf.Max(1, presentation.situationDieFace),
                Mathf.Max(1, presentation.situationRoll),
                !presentation.success);
        }

        if (agentDieView != null)
        {
            agentDieView.SetLabel(Mathf.Max(1, presentation.agentDieFace).ToString());
            agentDieView.PlayRollEffect(
                Mathf.Max(1, presentation.agentDieFace),
                Mathf.Max(1, presentation.agentRoll),
                presentation.success);
        }

        while (IsAnyDieRolling())
            yield return null;

        SetOverlayVisible(false);
        yield return null;

        if (orchestrator != null)
            orchestrator.NotifyDuelPresentationFinished();

        duelRoutine = null;
    }

    bool IsAnyDieRolling()
    {
        if (situationDieView != null && situationDieView.IsRolling)
            return true;
        if (agentDieView != null && agentDieView.IsRolling)
            return true;

        return false;
    }

    void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (overlayBlockerImage != null)
        {
            overlayBlockerImage.enabled = visible;
            overlayBlockerImage.raycastTarget = visible;
        }

        if (situationDieView != null)
            situationDieView.gameObject.SetActive(visible);
        if (agentDieView != null)
            agentDieView.gameObject.SetActive(visible);
    }
}
