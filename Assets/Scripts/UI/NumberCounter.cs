// NumberCounter.cs
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class NumberCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    
    [Min(0.01f)]
    [SerializeField] private float settleSeconds = 0.6f; // "현재 → 목표"까지 항상 이 시간에 도달

    public int Target { get; private set; }

    float current;     // 내부 진행(부동소수)
    float start, end; // 구간
    float t0, dur;    // 시작시간, 구간시간
    bool  playing;

    void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label != null && int.TryParse(label.text, out var parsed))
        {
            current = Target = parsed;
            UpdateLabel();
        }
        else if (label != null)
        {
            current = Target = 0;
            UpdateLabel();
        }
    }

    void Update()
    {
        if (!playing) return;

        float now = Time.unscaledTime;
        float t = (now - t0) / dur;

        if (t >= 1f)
        {
            current = end;
            playing = false;
        }
        else
            current = Mathf.Lerp(start, end, t);

        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (label != null) 
            label.text = Mathf.RoundToInt(current).ToString("N0");
    }
    
    public void SetTarget(int value)
    {
        Target = value;
        start = current;                 
        end   = value;
        dur   = Mathf.Max(0.0001f, settleSeconds);
        t0    = Time.unscaledTime;
        playing = true;
    }

    public void SetColor(Color color)
    {
        label.color = color;
    }
    
    public void SnapTo(int value)
    {
        Target = value;
        current = value;
        playing = false;
        UpdateLabel();
    }
}
