using UnityEngine;
using UnityEngine.UI;

public sealed class MissionOverlaySlotCellView : MonoBehaviour
{
    [SerializeField] Image frameImage;
    [SerializeField] Image portraitImage;
    [SerializeField] Graphic plusGraphic;
    [SerializeField] Graphic lockedOverlayGraphic;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    public void SetData(MissionOverlaySlotCellData data)
    {
        if (!setupValid || data == null)
            return;

        bool showPortrait = data.hasAssigned && data.portraitSprite != null;
        portraitImage.enabled = showPortrait;
        if (showPortrait)
        {
            portraitImage.sprite = data.portraitSprite;
            portraitImage.color = Color.white;
        }

        if (plusGraphic != null)
        {
            plusGraphic.gameObject.SetActive(!data.hasAssigned && data.isUsable);
            plusGraphic.color = data.isUsable ? Colors.Semantic.TextDisabled : Colors.Semantic.BorderSubtle;
        }

        if (frameImage != null)
            frameImage.color = data.isUsable ? Colors.Semantic.BorderSubtle : Colors.Semantic.TextDisabled;

        if (lockedOverlayGraphic != null)
        {
            lockedOverlayGraphic.gameObject.SetActive(!data.isUsable);
            lockedOverlayGraphic.color = Colors.Semantic.TextDisabled;
        }
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (frameImage == null)
        {
            Debug.LogError("[MissionOverlay] frameImage is not assigned.", this);
            valid = false;
        }

        if (portraitImage == null)
        {
            Debug.LogError("[MissionOverlay] portraitImage is not assigned.", this);
            valid = false;
        }

        if (plusGraphic == null)
        {
            Debug.LogError("[MissionOverlay] plusGraphic is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
