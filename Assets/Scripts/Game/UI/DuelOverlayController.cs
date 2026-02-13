using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class DuelOverlayController : MonoBehaviour
{
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

    void Start()
    {
        DuelManager.Instance.DuelRollStarted -= OnDuelRollStarted;
        DuelManager.Instance.DuelRollStarted += OnDuelRollStarted;
    }

    void OnDestroy()
    {
        if (DuelManager.Instance != null)
            DuelManager.Instance.DuelRollStarted -= OnDuelRollStarted;

        if (duelRoutine != null)
        {
            StopCoroutine(duelRoutine);
            duelRoutine = null;
        }
    }

    void OnDuelRollStarted(DuelManager.DuelRollPresentation presentation)
    {
        if (duelRoutine != null)
            StopCoroutine(duelRoutine);

        SetOverlayVisible(false);
        duelRoutine = StartCoroutine(PlayDuelRoutine(presentation));
    }

    IEnumerator PlayDuelRoutine(DuelManager.DuelRollPresentation presentation)
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

        DuelManager.Instance.NotifyDuelPresentationFinished();

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
