// StageInstance.cs
using System;

public sealed class StageInstance
{
    public StageDto StageDto { get; }
    public int StageIndex => StageDto.index;

    public double NeedScore => StageDto.needScore;
    public int RoundCount => StageDto.roundCount > 0 ? StageDto.roundCount : 3;

    public int CurrentRoundIndex { get; private set; }  // 0-based

    public StageInstance(StageDto stageDto)
    {
        StageDto = stageDto ?? throw new ArgumentNullException(nameof(stageDto));
        CurrentRoundIndex = 0;
    }

    public void SetCurrentRoundIndex(int index)
    {
        CurrentRoundIndex = index;
    }
}
