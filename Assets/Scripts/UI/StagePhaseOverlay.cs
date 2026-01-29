using UnityEngine;
using UnityEngine.UI;

public sealed class StagePhaseOverlay : MonoBehaviour
{
    [SerializeField] private bool hideInPlay = true;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private OverlayFader overlayFader;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
        if (overlayFader == null)
            overlayFader = GetComponent<OverlayFader>();
    }

    private void OnEnable()
    {
        var stage = StageManager.Instance;
        if (stage != null)
            stage.OnPhaseChanged += HandlePhaseChanged;

        Apply(stage != null ? stage.CurrentPhase : StagePhase.None);
    }

    private void OnDisable()
    {
        var stage = StageManager.Instance;
        if (stage != null)
            stage.OnPhaseChanged -= HandlePhaseChanged;
    }

    void HandlePhaseChanged(StagePhase phase)
    {
        Apply(phase);
    }

    void Apply(StagePhase phase)
    {
        bool shouldShow = !(hideInPlay && phase == StagePhase.Play);
        if (overlayFader != null)
        {
            if (shouldShow)
                overlayFader.Show();
            else
                overlayFader.Hide();
            return;
        }

        if (targetGraphic != null)
            targetGraphic.enabled = shouldShow;
    }
}
