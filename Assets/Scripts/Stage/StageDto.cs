using System;
using System.Collections.Generic;

[Serializable]
public class StageDto
{
    
}

public class StageInstance
{
    public readonly StageDto stageDto;

    public StageInstance(StageDto stageDto)
    {
        this.stageDto = stageDto;
    }
}