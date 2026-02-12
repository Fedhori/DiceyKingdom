using System;

public enum AssignmentResult
{
    Success = 0,
    AgentNotFound = 1,
    SituationNotFound = 2,
    AgentUnavailable = 3
}

public static class GameAssignmentService
{
    // Legacy compatibility shim. The dice-duel loop no longer keeps assignment state.
    public static AssignmentResult AssignAgentToSituation(
        GameRunState runState,
        string agentInstanceId,
        string situationInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return AssignmentResult.AgentNotFound;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return AssignmentResult.SituationNotFound;

        var agent = FindAgent(runState, agentInstanceId);
        if (agent == null)
            return AssignmentResult.AgentNotFound;
        if (agent.actionConsumed || agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return AssignmentResult.AgentUnavailable;

        var situation = FindSituation(runState, situationInstanceId);
        if (situation == null)
            return AssignmentResult.SituationNotFound;

        return AssignmentResult.Success;
    }

    public static AssignmentResult ClearAgentAssignment(GameRunState runState, string agentInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return AssignmentResult.AgentNotFound;

        var agent = FindAgent(runState, agentInstanceId);
        return agent == null ? AssignmentResult.AgentNotFound : AssignmentResult.Success;
    }

    public static int CountUnassignedAgents(GameRunState runState)
    {
        return CountPendingAgents(runState);
    }

    public static int CountPendingAgents(GameRunState runState)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));

        int count = 0;
        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;
            if (agent.actionConsumed)
                continue;
            if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
                continue;

            count += 1;
        }

        return count;
    }

    static AgentState FindAgent(GameRunState runState, string agentInstanceId)
    {
        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;
            if (!string.Equals(agent.instanceId, agentInstanceId, StringComparison.Ordinal))
                continue;

            return agent;
        }

        return null;
    }

    static SituationState FindSituation(GameRunState runState, string situationInstanceId)
    {
        for (int i = 0; i < runState.situations.Count; i++)
        {
            var situation = runState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            return situation;
        }

        return null;
    }

    public static AssignmentResult AssignAgentToEnemy(
        GameRunState runState,
        string agentInstanceId,
        string enemyInstanceId)
    {
        return AssignAgentToSituation(runState, agentInstanceId, enemyInstanceId);
    }
}
