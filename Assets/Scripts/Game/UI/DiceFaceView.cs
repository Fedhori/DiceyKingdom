using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DiceFaceView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI valueText;

    static readonly System.Random visualRng = new();
    Color defaultColor = Colors.White;
    Coroutine rollRoutine;

    public bool IsRolling => rollRoutine != null;

    void Awake()
    {
        EnsureBinding();
    }

    void OnDisable()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        if (valueText != null)
            valueText.color = defaultColor;
    }

    void OnValidate()
    {
        EnsureBinding();
    }

    public void SetLabel(string text)
    {
        EnsureBinding();
        if (valueText == null)
            return;
        if (IsRolling)
            return;

        valueText.text = text ?? string.Empty;
        valueText.color = defaultColor;
    }

    public void PlayRollEffect(int dieFace, int finalRoll, bool isSuccess)
    {
        EnsureBinding();
        if (valueText == null)
            return;

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);
        rollRoutine = StartCoroutine(PlayRollEffectRoutine(
            Mathf.Max(2, dieFace),
            Mathf.Max(1, finalRoll),
            isSuccess));
    }

    void EnsureBinding()
    {
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (valueText != null)
            defaultColor = valueText.color;
    }

    IEnumerator PlayRollEffectRoutine(int dieFace, int finalRoll, bool isSuccess)
    {
        float tickSeconds = Mathf.Max(0.01f, GameConfig.DuelRollSpinTickSeconds);
        float spinDuration = Mathf.Max(0f, GameConfig.DuelRollSpinDurationSeconds);
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            valueText.text = NextSpinValueText(dieFace);
            valueText.color = Colors.DuelRolling;
            yield return new WaitForSeconds(tickSeconds);
            elapsed += tickSeconds;
        }

        valueText.text = finalRoll.ToString();
        valueText.color = isSuccess ? Colors.DuelWin : Colors.DuelLose;

        float holdSeconds = Mathf.Max(0f, GameConfig.DuelRollResultHoldSeconds);
        if (holdSeconds > 0f)
            yield return new WaitForSeconds(holdSeconds);

        rollRoutine = null;
    }

    static string NextSpinValueText(int dieFace)
    {
        lock (visualRng)
        {
            int value = visualRng.Next(1, dieFace + 1);
            return value.ToString();
        }
    }
}
