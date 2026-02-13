using System.Collections;
using UnityEngine;

public sealed class DuelOverlayController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] GameObject overlayRoot;
    [SerializeField] DiceFaceView situationDieView;
    [SerializeField] DiceFaceView agentDieView;

    Coroutine duelRoutine;

    void Awake()
    {
        HideOverlay();
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

        HideOverlay();
    }

    void OnDuelRollStarted(GameTurnOrchestrator.DuelRollPresentation presentation)
    {
        if (duelRoutine != null)
            StopCoroutine(duelRoutine);

        duelRoutine = StartCoroutine(PlayDuelRoutine(presentation));
    }

    IEnumerator PlayDuelRoutine(GameTurnOrchestrator.DuelRollPresentation presentation)
    {
        ShowOverlay();

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

        HideOverlay();
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

    void ShowOverlay()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    void HideOverlay()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }
}
