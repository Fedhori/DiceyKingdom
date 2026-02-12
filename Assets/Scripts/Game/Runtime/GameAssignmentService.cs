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
        if (agent.actionConsumed)
            return AssignmentResult.AgentUnavailable;

        var situation = FindSituation(runState, situationInstanceId);
        if (situation == null)
            return AssignmentResult.SituationNotFound;

        RemoveFromAssignedSituation(runState, agent.instanceId, agent.assignedSituationInstanceId);

        if (!ContainsAssignedAgent(situation, agent.instanceId))
            situation.assignedAgentIds.Add(agent.instanceId);

        agent.assignedSituationInstanceId = situation.instanceId;
        return AssignmentResult.Success;
    }

    public static AssignmentResult ClearAgentAssignment(GameRunState runState, string agentInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return AssignmentResult.AgentNotFound;

        var agent = FindAgent(runState, agentInstanceId);
        if (agent == null)
            return AssignmentResult.AgentNotFound;

        RemoveFromAssignedSituation(runState, agent.instanceId, agent.assignedSituationInstanceId);
        agent.assignedSituationInstanceId = null;
        return AssignmentResult.Success;
    }

    public static int CountUnassignedAgents(GameRunState runState)
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
            if (!string.IsNullOrWhiteSpace(agent.assignedSituationInstanceId))
                continue;

            count += 1;
        }

        return count;
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

    static bool ContainsAssignedAgent(SituationState situation, string agentInstanceId)
    {
        if (situation?.assignedAgentIds == null)
            return false;

        for (int i = 0; i < situation.assignedAgentIds.Count; i++)
        {
            if (string.Equals(situation.assignedAgentIds[i], agentInstanceId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static void RemoveFromAssignedSituation(
        GameRunState runState,
        string agentInstanceId,
        string assignedSituationInstanceId)
    {
        if (string.IsNullOrWhiteSpace(assignedSituationInstanceId))
            return;

        var situation = FindSituation(runState, assignedSituationInstanceId);
        if (situation?.assignedAgentIds == null)
            return;

        for (int i = situation.assignedAgentIds.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(situation.assignedAgentIds[i], agentInstanceId, StringComparison.Ordinal))
                continue;

            situation.assignedAgentIds.RemoveAt(i);
        }
    }

    public static AssignmentResult AssignAgentToEnemy(
        GameRunState runState,
        string agentInstanceId,
        string enemyInstanceId)
    {
        return AssignAgentToSituation(runState, agentInstanceId, enemyInstanceId);
    }
}

