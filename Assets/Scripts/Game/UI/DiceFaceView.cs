using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DiceFaceView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] Image backgroundImage;

    static readonly System.Random visualRng = new();
    Coroutine rollRoutine;

    public bool IsRolling => rollRoutine != null;

    void Awake()
    {
        EnsureBinding();
        ApplyActiveVisual();
    }

    void OnDisable()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        ApplyActiveVisual();
    }

    void OnValidate()
    {
        EnsureBinding();
        if (!Application.isPlaying)
            ApplyActiveVisual();
    }

    public void SetLabel(string text)
    {
        EnsureBinding();
        if (valueText == null)
            return;

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        valueText.text = text ?? string.Empty;
        ApplyActiveVisual();
    }

    public void SetDisabledLabel(string text)
    {
        EnsureBinding();
        if (valueText == null)
            return;

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        valueText.text = text ?? string.Empty;
        ApplyDisabledVisual();
    }

    public void PlayRollEffect(int dieFace, int finalRoll, bool isSuccess)
    {
        EnsureBinding();
        if (valueText == null)
            return;

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);
        ApplyActiveVisual();
        rollRoutine = StartCoroutine(PlayRollEffectRoutine(
            Mathf.Max(1, dieFace),
            Mathf.Max(1, finalRoll),
            isSuccess));
    }

    void EnsureBinding()
    {
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    void ApplyActiveVisual()
    {
        if (valueText == null)
            return;

        valueText.color = Colors.DuelDefault;
        if (backgroundImage != null)
            backgroundImage.color = Colors.DuelDieBackgroundActive;
    }

    void ApplyDisabledVisual()
    {
        if (valueText == null)
            return;

        valueText.color = Colors.DuelDisabledText;
        if (backgroundImage != null)
            backgroundImage.color = Colors.DuelDieBackgroundInactive;
    }

    IEnumerator PlayRollEffectRoutine(int dieFace, int finalRoll, bool isSuccess)
    {
        float tickSeconds = Mathf.Max(0.01f, GameConfig.DuelOverlaySpinTickSeconds);
        float spinDuration = Mathf.Max(0f, GameConfig.DuelOverlaySpinDurationSeconds);
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            valueText.text = NextSpinValueText(dieFace);
            valueText.color = Colors.DuelRolling;
            yield return new WaitForSeconds(tickSeconds);
            elapsed += tickSeconds;
        }

        valueText.text = finalRoll.ToString();
        valueText.color = isSuccess ? Colors.DuelSuccess : Colors.DuelFailure;

        float holdSeconds = Mathf.Max(0f, GameConfig.DuelOverlayResultHoldSeconds);
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
