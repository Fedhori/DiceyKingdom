namespace Game.UI.Tooltip
{
    public interface ITooltipAnchorSource
    {
        bool TryBuildAnchor(out TooltipAnchor anchor);
    }
}
