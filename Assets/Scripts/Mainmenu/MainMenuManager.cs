using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }
    
    [SerializeField] GameObject mainMenuBallPrefab;
    [SerializeField] GameObject mainMenuPinPrefab;
    [SerializeField] private GameObject pinsContainer;
    
    [SerializeField] int rowCount = 5;
    [SerializeField] int columnCount = 5;
    [SerializeField] float pinRadius = 64f;

    private float ballSpawnY = 600f;
    [SerializeField] private float cycle = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GeneratePins();
        StartCoroutine(SpawnBalls());
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }
    
    void GeneratePins()
    {
        if (mainMenuPinPrefab == null || pinsContainer == null)
        {
            Debug.LogWarning("[MainMenuManager] Pin prefab 또는 pinsContainer가 설정되지 않았습니다.");
            return;
        }

        for (int row = 0; row < rowCount; row++)
        {
            bool isOddRow = (row % 2) == 1;
            int colsInRow = isOddRow ? columnCount - 1 : columnCount;

            for (int col = 0; col < colsInRow; col++)
            {
                // 핀 로컬 좌표 계산 (pinsContainer 기준)
                Vector2 localPos2D = GetPinWorldPosition(row, col);
                Vector3 localPos = new Vector3(localPos2D.x, localPos2D.y, 0f);

                var pin = Instantiate(mainMenuPinPrefab, pinsContainer.transform);
                var t = pin.transform;
                t.localPosition = localPos;
                t.localRotation = Quaternion.identity;
            }
        }
    }
    
    public Vector2 GetPinWorldPosition(int row, int column)
    {
        float dx = pinRadius * 2f;
        float dy = pinRadius * Mathf.Sqrt(3f);

        int centerRow = rowCount / 2;
        int centerCol = columnCount / 2;

        bool isOddRow = (row % 2) == 1;

        float baseX = (column - centerCol) * dx;
        if (isOddRow)
            baseX += pinRadius;

        float baseY = (centerRow - row) * dy;

        return new Vector2(baseX, baseY);
    }
    
    IEnumerator SpawnBalls()
    {
        if (mainMenuBallPrefab == null || pinsContainer == null)
        {
            Debug.LogWarning("[MainMenuManager] Ball prefab 또는 pinsContainer가 설정되지 않았습니다.");
            yield break;
        }

        // columnCount와 pinRadius를 이용해서 핀 배열의 가로 폭 기준으로 랜덤 스폰
        float dx = pinRadius * 2f;
        float totalWidth = (columnCount - 1) * dx;
        float halfWidth = totalWidth * 0.5f;

        while (true)
        {
            float randomX = Random.Range(-halfWidth, halfWidth);
            Vector3 localPos = new Vector3(randomX, ballSpawnY, 0f);

            var ball = Instantiate(mainMenuBallPrefab, pinsContainer.transform);
            var t = ball.transform;
            t.localPosition = localPos;
            t.localRotation = Quaternion.identity;

            yield return new WaitForSeconds(cycle);
        }
    }
}
