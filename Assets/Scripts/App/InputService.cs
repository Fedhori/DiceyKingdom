using UnityEngine;
using UnityEngine.InputSystem;




namespace Game.App
{
public class InputService : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    public InputAction GetAction(string actionName)
    {
        if (playerInput == null || string.IsNullOrEmpty(actionName))
            return null;

        return playerInput.actions.FindAction(actionName, throwIfNotFound: false);
    }

    void OnMenu()
    {
    }
}


}
