using System;
using System.Collections.Generic;

public sealed class StaticDataSet
{
    public List<AdventurerDef> adventurerDefs = new();
    public List<MissionDef> missionDefs = new();
    public List<TraitDef> traitDefs = new();

    readonly Dictionary<string, AdventurerDef> adventurerDefById = new(StringComparer.Ordinal);
    readonly Dictionary<string, MissionDef> missionDefById = new(StringComparer.Ordinal);
    readonly Dictionary<string, TraitDef> traitDefById = new(StringComparer.Ordinal);

    public void BuildIndexes()
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
}
