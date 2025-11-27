using Data;

public sealed class PinInstance
{
    public PinDto BaseDto { get; }
    public string Id => BaseDto.id;

    public int HitCount { get; private set; }

    public int Row { get; }
    public int Column { get; }

    public PinInstance(PinDto dto, int row, int column)
    {
        BaseDto = dto;
        Row = row;
        Column = column;
        HitCount = 0;
    }

    public void OnHitByBall(BallInstance ball)
    {
        HitCount++;
        // 나중에 HitCount 기반 파괴/변형 로직 추가 가능
        // Future: add break/change logic based on HitCount
    }
}