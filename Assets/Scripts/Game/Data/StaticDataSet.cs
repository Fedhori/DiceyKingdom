using System;
using System.Collections.Generic;

public sealed class StaticDataSet
{
    readonly List<AdventurerDef> adventurerDefs = new();
    readonly List<MissionDef> missionDefs = new();
    readonly List<TraitDef> traitDefs = new();

    public IReadOnlyList<AdventurerDef> AdventurerDefs => adventurerDefs;
    public IReadOnlyList<MissionDef> MissionDefs => missionDefs;
    public IReadOnlyList<TraitDef> TraitDefs => traitDefs;

    readonly Dictionary<string, AdventurerDef> adventurerDefById = new(StringComparer.Ordinal);
    readonly Dictionary<string, MissionDef> missionDefById = new(StringComparer.Ordinal);
    readonly Dictionary<string, TraitDef> traitDefById = new(StringComparer.Ordinal);

    public StaticDataSet(
        IReadOnlyList<AdventurerDef> adventurers,
        IReadOnlyList<MissionDef> missions,
        IReadOnlyList<TraitDef> traits)
    {
        if (adventurers != null)
            adventurerDefs.AddRange(adventurers);
        if (missions != null)
            missionDefs.AddRange(missions);
        if (traits != null)
            traitDefs.AddRange(traits);

        BuildIndexes();
    }

    void BuildIndexes()
    {
        adventurerDefById.Clear();
        missionDefById.Clear();
        traitDefById.Clear();

        for (int i = 0; i < adventurerDefs.Count; i++)
        {
            AdventurerDef def = adventurerDefs[i];
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                continue;

            adventurerDefById[def.id] = def;
        }

        for (int i = 0; i < missionDefs.Count; i++)
        {
            MissionDef def = missionDefs[i];
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                continue;

            missionDefById[def.id] = def;
        }

        for (int i = 0; i < traitDefs.Count; i++)
        {
            TraitDef def = traitDefs[i];
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                continue;

            traitDefById[def.id] = def;
        }
    }

    public bool TryGetAdventurerDef(string id, out AdventurerDef def)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            def = null;
            return false;
        }

        return adventurerDefById.TryGetValue(id, out def);
    }

    public bool TryGetMissionDef(string id, out MissionDef def)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            def = null;
            return false;
        }

        return missionDefById.TryGetValue(id, out def);
    }

    public bool TryGetTraitDef(string id, out TraitDef def)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            def = null;
            return false;
        }

        return traitDefById.TryGetValue(id, out def);
    }

    public AdventurerDef GetAdventurerDefOrThrow(string id)
    {
        if (TryGetAdventurerDef(id, out AdventurerDef def))
            return def;

        throw new KeyNotFoundException($"[StaticDataSet] AdventurerDef not found: {id}");
    }

    public MissionDef GetMissionDefOrThrow(string id)
    {
        if (TryGetMissionDef(id, out MissionDef def))
            return def;

        throw new KeyNotFoundException($"[StaticDataSet] MissionDef not found: {id}");
    }

    public TraitDef GetTraitDefOrThrow(string id)
    {
        if (TryGetTraitDef(id, out TraitDef def))
            return def;

        throw new KeyNotFoundException($"[StaticDataSet] TraitDef not found: {id}");
    }
}
