using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class AgentDiceFaceClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] AgentController owner;
    [SerializeField] int dieIndex = -1;

    public void Bind(AgentController controller, int index)
    {
        owner = controller;
        dieIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (owner == null)
            return;

        owner.OnDiceFacePressed(dieIndex);
    }
}
