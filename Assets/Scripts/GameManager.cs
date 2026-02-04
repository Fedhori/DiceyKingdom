using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public System.Random Rng { get; private set; } = new System.Random();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResetRng(int seed)
    {
        Rng = new System.Random(seed);
    }

    public bool TryLoad(out string payloadJson)
    {
        payloadJson = string.Empty;

        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.TryLoad(out payloadJson);
    }

    public bool Save(string payloadJson)
    {
        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.Save(payloadJson);
    }
}
