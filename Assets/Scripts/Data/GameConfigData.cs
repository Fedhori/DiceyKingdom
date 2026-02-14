using System;

[Serializable]
public sealed class GameConfigData
{
    public int candidateCountPerTurn;
    public int barracksCapacity;
    public int missionSpawnCountPerTurn;
    public int globalHpRegenPerTurn;
    public int restStaminaRegenPerTurn;
    public float traitPositiveOnSuccess;
    public float traitNegativeOnSuccess;
    public float traitPositiveOnFailure;
    public float traitNegativeOnFailure;
}
