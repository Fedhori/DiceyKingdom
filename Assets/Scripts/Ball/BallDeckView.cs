using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallDeckView : MonoBehaviour
{
    [SerializeField] private Image ballIcon;
    [SerializeField] private TMP_Text ballCount;
    [SerializeField] private BallUITooltipTarget ballUITooltipTarget;

    public void UpdateBallDeckView(string ballId, int count)
    {
        ballIcon.sprite = SpriteCache.GetBallSprite(ballId);
        ballCount.text = count.ToString();

        BallRepository.TryGet(ballId, out BallDto dto);
        if (dto == null)
            return;

        var ballInstance = new BallInstance(dto);
        ballUITooltipTarget.Bind(ballInstance);
    }
}