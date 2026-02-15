using System;

[Serializable]
/// <summary>
/// Data model for game config values.
/// </summary>
public sealed class GameConfigData
{
    // Recruitment & capacity
    public int candidateCountPerTurn;
    public int barracksCapacity;
    public int missionSpawnCountPerTurn;

    // Turn settlement
    public int globalHpRegenPerTurn;
    public int restStaminaRegenPerTurn;

    // Trait slots
    public int traitSlotCount;

    // Trait roll (expedition success)
    public float traitNoChangeOnSuccess;
    public float traitPositiveOnSuccess;
    public float traitNegativeOnSuccess;

    // Trait roll (expedition failure)
    public float traitNoChangeOnFailure;
    public float traitPositiveOnFailure;
    public float traitNegativeOnFailure;
}

