using System;

[Serializable]



public sealed class GameConfigData
{
    
    public int candidateCountPerTurn;
    public int barracksCapacity;
    public int startingAdventurerCount;
    public int missionSpawnCountPerTurn;

    
    public int globalHpRegenPerTurn;
    public int restStaminaRegenPerTurn;

    
    public int traitSlotCount;

    
    public float traitNoChangeOnSuccess;
    public float traitPositiveOnSuccess;
    public float traitNegativeOnSuccess;

    
    public float traitNoChangeOnFailure;
    public float traitPositiveOnFailure;
    public float traitNegativeOnFailure;
}

