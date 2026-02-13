using UnityEngine;
using UnityEngine.UI;

public sealed class AgentRollButton : MonoBehaviour
{
    [SerializeField] string agentInstanceId = string.Empty;
    [SerializeField] Button button;

    public void SetAgentInstanceId(string instanceId)
    {
        agentInstanceId = instanceId ?? string.Empty;
        RefreshInteractable();
    }

    public void SetButton(Button value)
    {
        button = value;
    }

    public void OnRollPressed()
    {
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return;

        AgentManager.Instance.TryRollAgent(agentInstanceId);
    }

    void Reset()
    {
        if (button == null)
            button = GetComponent<Button>();
        RefreshInteractable();
    }

    void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    void Start()
    {
        SubscribeEvents();
        RefreshInteractable();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    void OnRunStarted(GameRunState _)
    {
        RefreshInteractable();
    }

    void OnRunEnded(GameRunState _)
    {
        RefreshInteractable();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshInteractable();
    }

    void SubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunStarted -= OnRunStarted;
            GameManager.Instance.RunEnded -= OnRunEnded;
            GameManager.Instance.RunStarted += OnRunStarted;
            GameManager.Instance.RunEnded += OnRunEnded;
        }

        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
            PhaseManager.Instance.PhaseChanged += OnPhaseChanged;
        }
    }

    void UnsubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunStarted -= OnRunStarted;
            GameManager.Instance.RunEnded -= OnRunEnded;
        }

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
    }

    void RefreshInteractable()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button == null)
            return;

        button.interactable = AgentManager.Instance != null && AgentManager.Instance.CanRollAgent(agentInstanceId);
    }
}
