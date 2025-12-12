using System.Collections;
using Data;
using UnityEngine;

public class PinMainMenuController : MonoBehaviour
{
    PinDto pinDto;
    [SerializeField] SpriteRenderer pinIcon;
    Vector3 baseScale;
    Coroutine hitRoutine;
    float hitScaleMultiplier = 1.2f;
    [SerializeField] float growDuration = 0.06f;
    [SerializeField] float shrinkDuration = 0.08f;
    
    void Awake()
    {
        baseScale = transform.localScale;
        pinDto = PinRepository.GetRandomPin();
        pinIcon.sprite = SpriteCache.GetPinSprite(pinDto.id);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayHitEffect();
    }
    
    public void PlayHitEffect()
    {
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitScaleRoutine());
    }

    IEnumerator HitScaleRoutine()
    {
        var t = 0f;
        var start = baseScale;
        var target = baseScale * hitScaleMultiplier;

        while (t < growDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / growDuration);
            transform.localScale = Vector3.Lerp(start, target, u);
            yield return null;
        }

        t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / shrinkDuration);
            transform.localScale = Vector3.Lerp(target, baseScale, u);
            yield return null;
        }

        transform.localScale = baseScale;
        hitRoutine = null;
    }
}
