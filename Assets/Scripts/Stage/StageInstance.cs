// StageInstance.cs
using System;

public sealed class StageInstance
{
    public StageDto StageDto { get; }
    public int StageIndex { get; }

    public double NeedScore => StageDto.needScore;
    public int RoundCount => StageDto.roundCount > 0 ? StageDto.roundCount : 3;

    public int CurrentRoundIndex { get; private set; }  // 0-based

    public StageInstance(StageDto stageDto, int stageIndex)
    {
        StageDto = stageDto ?? throw new ArgumentNullException(nameof(stageDto));
        StageIndex = stageIndex;
        CurrentRoundIndex = 0;
    }

    public void SetCurrentRoundIndex(int index)
    {
        CurrentRoundIndex = index;
    }
}