using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UiBackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        UiSelectionEvents.RaiseSelectionCleared();
    }
}
