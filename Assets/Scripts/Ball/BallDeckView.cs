using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallDeckView : MonoBehaviour
{
    [SerializeField] private Image ballIcon;
    [SerializeField] private TMP_Text ballCount;

    public void UpdateBallDeckView(string ballId, int count)
    {
        ballIcon.sprite = SpriteCache.GetBallSprite(ballId);
        ballCount.text = count.ToString();
    }
}
