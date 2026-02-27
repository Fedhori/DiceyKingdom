using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Tooltip
{
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] TMP_Text typeText;
        [SerializeField] Transform keywordRoot;
        [SerializeField] TooltipKeywordRow keywordRowPrefab;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image nameImage;
        [SerializeField] Image typeImage;
        [SerializeField] TMP_Text descriptionText;

        public RectTransform rectTransform;
        readonly List<TooltipKeywordRow> keywordRows = new();
        Color nameImageDefaultColor;
        bool hasNameImageDefaultColor;

        void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (rectTransform != null)
            {
                rectTransform.pivot = new Vector2(0f, 1f);
            }

            if (nameImage != null)
            {
                nameImageDefaultColor = nameImage.color;
                hasNameImageDefaultColor = true;
            }

            gameObject.SetActive(false);
        }

        public void Show(TooltipModel model)
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.text = model.title ?? string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = model.body ?? string.Empty;
            }

            BuildKeywordRows(model.keywordEntries);
            ApplyTypeHidden();
            ResetNameBackground();
        }

        public void Hide()
        {
            ClearKeywordRows();
            gameObject.SetActive(false);
        }

        void ApplyTypeHidden()
        {
            if (typeImage != null)
            {
                typeImage.gameObject.SetActive(false);
            }

            if (typeText != null)
            {
                typeText.gameObject.SetActive(false);
                typeText.text = string.Empty;
            }
        }

        void ResetNameBackground()
        {
            if (nameImage == null)
            {
                return;
            }

            if (hasNameImageDefaultColor)
            {
                nameImage.color = nameImageDefaultColor;
            }
        }

        void BuildKeywordRows(IReadOnlyList<TooltipKeywordEntry> entries)
        {
            if (keywordRoot == null || keywordRowPrefab == null)
            {
                return;
            }

            ClearKeywordRows();

            if (entries == null || entries.Count == 0)
            {
                keywordRoot.gameObject.SetActive(false);
                return;
            }

            keywordRoot.gameObject.SetActive(true);
            for (int i = 0; i < entries.Count; i++)
            {
                TooltipKeywordRow row = Instantiate(keywordRowPrefab, keywordRoot);
                row.Bind(entries[i]);
                keywordRows.Add(row);
            }
        }

        void ClearKeywordRows()
        {
            if (keywordRows.Count == 0)
            {
                if (keywordRoot != null)
                {
                    keywordRoot.gameObject.SetActive(false);
                }

                return;
            }

            for (int i = keywordRows.Count - 1; i >= 0; i--)
            {
                TooltipKeywordRow row = keywordRows[i];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            keywordRows.Clear();
            if (keywordRoot != null)
            {
                keywordRoot.gameObject.SetActive(false);
            }
        }
    }
}
