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
        
        var rigidBody = ballController.GetComponent<Rigidbody2D>();
        if (rigidBody == null)
            return;

        if (ballController.Instance.life > 0)
        {
            ballController.Instance.life--;
            PinEffectManager.Instance?.OnBallDestroyed(ballController.Instance);
            ballController.transform.position = new Vector2(UnityEngine.Random.Range(-288, 288f), 500);
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        Destroy(other.gameObject);
    }
}