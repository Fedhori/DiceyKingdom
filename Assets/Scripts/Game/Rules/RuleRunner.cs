using System;
using System.Collections.Generic;

public static class RuleRunner
{
    public static RuleExecutionSummary RunTraitsThenMission(
        RunState runState,
        string missionUid,
        string trigger,
        RuleContext context,
        IRuleEffectApplier effectApplier = null)
    {
        var summary = new RuleExecutionSummary();
        if (runState == null || string.IsNullOrWhiteSpace(missionUid) || string.IsNullOrWhiteSpace(trigger))
            return summary;

        if (!TryGetMissionInstance(runState.missions, missionUid, out MissionInstance missionInstance))
            return summary;

        RuleContext baseContext = CreateContext(context, runState, missionUid, string.Empty);
        effectApplier ??= NoopRuleEffectApplier.Shared;

        // Global order (confirmed): Trait rules first, then Mission rules.
        summary.Add(RunTraitRulesForMissionParty(runState, missionInstance, trigger, baseContext, effectApplier));
        summary.Add(RunMissionRules(runState, missionInstance, trigger, baseContext, effectApplier));
        return summary;
    }

    public static RuleExecutionSummary RunTraitRulesByAdventurer(
        RunState runState,
        string adventurerUid,
        string trigger,
        RuleContext context,
        IRuleEffectApplier effectApplier = null)
    {
        var summary = new RuleExecutionSummary();
        if (runState == null || string.IsNullOrWhiteSpace(adventurerUid) || string.IsNullOrWhiteSpace(trigger))
            return summary;

        if (runState.traits == null || runState.traits.Count == 0)
            return summary;

        effectApplier ??= NoopRuleEffectApplier.Shared;
        for (int i = 0; i < runState.traits.Count; i++)
        {
            TraitInstance traitInstance = runState.traits[i];
            if (traitInstance == null)
                continue;

            if (!string.Equals(traitInstance.ownerAdventurerUid, adventurerUid, StringComparison.Ordinal))
                continue;

            if (!StaticDataLoader.Current.TryGetTraitDef(traitInstance.traitId, out TraitDef traitDef))
                continue;

            RuleContext perTraitContext = CreateContext(context, runState, context?.missionUid ?? string.Empty, adventurerUid);
            summary.Add(RulePipeline.Execute(
                traitDef.rules,
                trigger,
                perTraitContext,
                RuleSourceType.Trait,
                traitInstance.uid,
                perTraitContext.missionUid,
                adventurerUid,
                effectApplier));
        }

        return summary;
    }

    public static RuleExecutionSummary RunMissionRules(
        RunState runState,
        string missionUid,
        string trigger,
        RuleContext context,
        IRuleEffectApplier effectApplier = null)
    {
        var summary = new RuleExecutionSummary();
        if (runState == null || string.IsNullOrWhiteSpace(missionUid) || string.IsNullOrWhiteSpace(trigger))
            return summary;

        if (!TryGetMissionInstance(runState.missions, missionUid, out MissionInstance missionInstance))
            return summary;

        RuleContext normalizedContext = CreateContext(context, runState, missionUid, context?.adventurerUid ?? string.Empty);
        return RunMissionRules(runState, missionInstance, trigger, normalizedContext, effectApplier);
    }

    static RuleExecutionSummary RunTraitRulesForMissionParty(
        RunState runState,
        MissionInstance missionInstance,
        string trigger,
        RuleContext baseContext,
        IRuleEffectApplier effectApplier)
    {
        var summary = new RuleExecutionSummary();
        if (missionInstance.assignedAdventurerUids == null || missionInstance.assignedAdventurerUids.Count == 0)
            return summary;

        var partyUids = new HashSet<string>(missionInstance.assignedAdventurerUids, StringComparer.Ordinal);
        if (runState.traits == null || runState.traits.Count == 0)
            return summary;

        for (int i = 0; i < runState.traits.Count; i++)
        {
            TraitInstance traitInstance = runState.traits[i];
            if (traitInstance == null)
                continue;

            if (string.IsNullOrWhiteSpace(traitInstance.ownerAdventurerUid) || !partyUids.Contains(traitInstance.ownerAdventurerUid))
                continue;

            if (!StaticDataLoader.Current.TryGetTraitDef(traitInstance.traitId, out TraitDef traitDef))
                continue;

            RuleContext perTraitContext = CreateContext(baseContext, runState, missionInstance.uid, traitInstance.ownerAdventurerUid);
            summary.Add(RulePipeline.Execute(
                traitDef.rules,
                trigger,
                perTraitContext,
                RuleSourceType.Trait,
                traitInstance.uid,
                missionInstance.uid,
                traitInstance.ownerAdventurerUid,
                effectApplier));
        }

        return summary;
    }

    static RuleExecutionSummary RunMissionRules(
        RunState runState,
        MissionInstance missionInstance,
        string trigger,
        RuleContext baseContext,
        IRuleEffectApplier effectApplier)
    {
        if (!StaticDataLoader.Current.TryGetMissionDef(missionInstance.missionId, out MissionDef missionDef))
            return new RuleExecutionSummary();

        RuleContext missionContext = CreateContext(baseContext, runState, missionInstance.uid, baseContext?.adventurerUid ?? string.Empty);
        return RulePipeline.Execute(
            missionDef.rules,
            trigger,
            missionContext,
            RuleSourceType.Mission,
            missionInstance.uid,
            missionInstance.uid,
            missionContext.adventurerUid,
            effectApplier);
    }

    static RuleContext CreateContext(RuleContext source, RunState runState, string missionUid, string adventurerUid)
    {
        RuleContext context = source?.Clone() ?? new RuleContext();
        context.runState = runState;
        context.missionUid = missionUid ?? string.Empty;
        context.adventurerUid = adventurerUid ?? string.Empty;
        return context;
    }

    static bool TryGetMissionInstance(IReadOnlyList<MissionInstance> missions, string missionUid, out MissionInstance mission)
    {
        mission = null;
        if (missions == null || string.IsNullOrWhiteSpace(missionUid))
            return false;

        for (int i = 0; i < missions.Count; i++)
        {
            MissionInstance entry = missions[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, missionUid, StringComparison.Ordinal))
            {
                mission = entry;
                return true;
            }
        }

        return false;
    }
}
