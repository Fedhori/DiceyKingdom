using Game.UI;
using Game.UI.Tooltip;




namespace Game.App
{
public sealed class UIService
{
    public TooltipService Tooltip { get; }
    public ModalService Modal { get; }
    public OptionService Option { get; }
    public FloatingTextService FloatingText { get; }
    public ToastService Toast { get; }

    public UIService(
        TooltipService tooltip,
        ModalService modal,
        OptionService option,
        FloatingTextService floatingText,
        ToastService toast)
    {
        Tooltip = tooltip;
        Modal = modal;
        Option = option;
        FloatingText = floatingText;
        Toast = toast;
    }
}


}
