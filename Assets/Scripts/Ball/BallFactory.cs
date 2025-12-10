using UnityEngine;

public sealed class BallFactory : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;
    [SerializeField] private Transform ballParent;
    public static BallFactory Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnBallById(string ballId)
    {
        if (FlowManager.Instance.CurrentPhase != FlowPhase.Round)
        {
            Debug.LogError($"Invalid Phase Type. Phase: {FlowManager.Instance.CurrentPhase}");
            return;
        }

        var ball = Instantiate(ballPrefab, ballParent);
        var controller = ball.GetComponent<BallController>();
        controller.Initialize(ballId);
        // 하드코딩 - 어떻게 볼을 스폰할지는 고민 필요. 플레이어가 뭔가 조작이 가능하게 해도 괜찮을거 같은데.. 발사기처럼?
        // 아니면 타이밍만 맞추게? 발사기가 위에서 좌우로 왔다갔다 하는?
        // 아니면 그냥 룰렛처럼 누르면 좌르르륵~? << 이게 최선 아닐까?
        // 인게임에서는 사용 아이템이 조작을 가져갈 수 있으니, 시작 이후에는 조작이 없어야 함
        // 여기 수정할때, 부활쪽 로직도 대응 필요
        ball.transform.position = new Vector2(Random.Range(-288, 288f), 500);
    }
}