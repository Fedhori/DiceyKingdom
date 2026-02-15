using UI;

public sealed class UIService
{
    public TooltipManager Tooltip { get; }
    public ModalManager Modal { get; }
    public OptionManager Option { get; }
    public FloatingTextManager FloatingText { get; }
    public ToastManager Toast { get; }

    public UIService(
        TooltipManager tooltip,
        ModalManager modal,
        OptionManager option,
        FloatingTextManager floatingText,
        ToastManager toast)
    {
        Tooltip = tooltip;
        Modal = modal;
        Option = option;
        FloatingText = floatingText;
        Toast = toast;
    }
}
