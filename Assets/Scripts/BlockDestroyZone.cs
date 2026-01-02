using UnityEngine;

public class BlockDestroyZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Block"))
        {
            GameManager.Instance?.HandleGameOver();
        }
    }
}
