namespace Game.UI.Tooltip
{
    public interface ITooltipContentProvider
    {
        bool TryBuildTooltipModel(out TooltipModel model);
    }
}
