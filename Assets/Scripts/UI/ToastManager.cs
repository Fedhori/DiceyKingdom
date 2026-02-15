using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace UI
{
    public class ToastManager : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject toastPrefab; // Assign this in the Inspector

        public bool ValidateConfiguration(Transform appRoot)
        {
            bool valid = true;

            if (canvas == null)
            {
                Debug.LogError("[ToastManager] canvas is not assigned.");
                valid = false;
            }
            else if (appRoot != null && !canvas.transform.IsChildOf(appRoot))
            {
                Debug.LogError("[ToastManager] canvas must be placed under GameApp in editor.");
                valid = false;
            }

            if (toastPrefab == null)
            {
                Debug.LogError("[ToastManager] toastPrefab is not assigned.");
                valid = false;
            }

            return valid;
        }

        public void ShowToastMessage(string tableKey, string entryKey, float waitSecond = 2.0f, float fadeDuration = 0.5f)
        {
            if (toastPrefab == null) return;

            GameObject toastInstance = Instantiate(toastPrefab, canvas.transform);
            var localizeEvent = toastInstance.GetComponent<LocalizeStringEvent>();
            localizeEvent.StringReference.SetReference(tableKey, entryKey);
            localizeEvent.StringReference.RefreshString();

            StartCoroutine(ToastLifecycle(toastInstance, waitSecond, fadeDuration));
        }

        private IEnumerator ToastLifecycle(GameObject toastInstance, float waitSecond, float fadeDuration)
        {
            var canvasGroup = toastInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                yield return new WaitForSeconds(waitSecond);
                Destroy(toastInstance);
                yield break;
            }

            // Wait
            yield return new WaitForSecondsRealtime(waitSecond);

            // Fade Out
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
                yield return null;
            }

            Destroy(toastInstance);
        }
    }
}
