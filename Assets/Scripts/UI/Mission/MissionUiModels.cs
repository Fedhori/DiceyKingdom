using System.Collections.Generic;
using UnityEngine;

public sealed class MissionWorldCardData
{
    public string missionUid = string.Empty;
    public string missionName = string.Empty;
    public int remainingDeadlineTurns;
    public int displayedPartyLimit = 1;
    public bool isSelected;
    public List<MissionWorldTestData> tests = new();
}

public sealed class MissionWorldTestData
{
    public int difficulty;
    public bool isCleared;
    public List<string> requiredAbilities = new();
}

public sealed class MissionOverlayData
{
    public string missionUid = string.Empty;
    public string missionName = string.Empty;
    public int remainingDeadlineTurns;
    public int partyLimit = 1;
    public bool isSelected;
    public bool isLocked;
    public string successSummary = string.Empty;
    public string deadlineFailSummary = string.Empty;
    public List<string> tags = new();
    public List<MissionOverlayTestData> tests = new();
}

public sealed class MissionOverlayTestData
{
    public int difficulty;
    public bool isCleared;
    public List<string> requiredAbilities = new();
}

public sealed class MissionPartyTotalsData
{
    public int strength;
    public int agility;
    public int intelligence;
}

public sealed class MissionAdventurerRowData
{
    public string adventurerUid = string.Empty;
    public string displayName = string.Empty;
    public int level = 1;
    public int hp;
    public int maxHp;
    public int stamina;
    public int maxStamina;
    public int strength;
    public int agility;
    public int intelligence;
    public bool isAssignable;
    public Sprite portraitSprite;
}

public sealed class MissionDraftSlotData
{
    public int slotIndex;
    public string assignedAdventurerUid = string.Empty;
    public string assignedDisplayName = string.Empty;
    public bool canInteract;
    public bool hasAssigned;
}

public sealed class MissionOverlayTestDiceData
{
    public int value;
    public bool isCleared;
    public List<string> requiredAbilities = new();
}

public sealed class MissionOverlayStatDiceData
{
    public string abilityId = string.Empty;
    public int value;
}

public sealed class MissionOverlaySlotCellData
{
    public int slotIndex;
    public bool isUsable;
    public bool hasAssigned;
    public string assignedAdventurerUid = string.Empty;
    public Sprite portraitSprite;
}
