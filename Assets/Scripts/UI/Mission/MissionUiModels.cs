using System.Collections.Generic;

public sealed class MissionWorldCardData
{
    public string missionUid = string.Empty;
    public string missionName = string.Empty;
    public int remainingDeadlineTurns;
    public int displayedPartyLimit = 2;
    public bool isSelected;
    public List<MissionWorldTestData> tests = new();
}

public sealed class MissionWorldTestData
{
    public int difficulty;
    public bool isCleared;
    public List<string> requiredAbilities = new();
}
