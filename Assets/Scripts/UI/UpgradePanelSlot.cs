using System;
using UnityEngine;

public sealed class UpgradePanelSlot : MonoBehaviour
{
    [SerializeField] TooltipView tooltipView;

    public UpgradeInstance Upgrade { get; private set; }
    public TooltipView TooltipView => tooltipView;

    void Awake()
    {
        if (tooltipView == null)
            tooltipView = GetComponentInChildren<TooltipView>(true);
    }

    public void Bind(UpgradeInstance upgrade)
    {
        Upgrade = upgrade;

        if (tooltipView == null)
            return;

        if (upgrade == null)
            tooltipView.Hide();
        else
            tooltipView.Show(UpgradeTooltipUtil.BuildModel(upgrade));
    }

    public void SetToggleButton(bool visible, string labelKey, Color backgroundColor, bool interactable, Action onClick)
    {
        if (tooltipView == null)
            return;

        tooltipView.SetToggleButton(visible, labelKey, backgroundColor, interactable, onClick);
    }
}
