using System;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null)
        {
            Destroy(other.gameObject);
            return;
        }

        var ballController = other.GetComponent<BallController>();
        if (ballController == null)
            return;

        BallManager.Instance?.QueueForRelaunch(ballController);
    }
}
