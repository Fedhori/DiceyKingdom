using UnityEngine;

public class ProjectileDestroyZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Projectile"))
        {
            Destroy(other.gameObject);
        }
    }
}