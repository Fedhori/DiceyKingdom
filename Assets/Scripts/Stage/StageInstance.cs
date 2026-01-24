// StageInstance.cs
using System;

public sealed class StageInstance
{
    public StageDto StageDto { get; }
    public int StageIndex => StageDto.index;
    public double SpawnBudgetScale => StageDto.difficulty;
    public float SpawnSecond => StageDto.spawnSecond;
    public StageInstance(StageDto stageDto)
    {
        StageDto = stageDto ?? throw new ArgumentNullException(nameof(stageDto));
    }
}
