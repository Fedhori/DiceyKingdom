using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameRuntimeUiScaffoldBuilder
{
    const string menuPath = "Tools/DiceyKingdom/Build MainUi Runtime Scaffold";
    const string rootName = "GameRuntimeRoot";

    static readonly Color backgroundColor = new(0.06f, 0.07f, 0.1f, 0.96f);
    static readonly Color panelColor = new(0.11f, 0.13f, 0.17f, 0.96f);
    static readonly Color panelSoftColor = new(0.13f, 0.16f, 0.21f, 0.9f);
    static readonly Color buttonColor = new(0.23f, 0.27f, 0.34f, 1f);
    static readonly Color buttonDangerColor = new(0.45f, 0.22f, 0.24f, 1f);
    static readonly Color textPrimary = new(0.95f, 0.96f, 0.99f, 1f);
    static readonly Color textMuted = new(0.74f, 0.77f, 0.84f, 1f);
    static readonly Color textSuccess = new(0.58f, 0.9f, 0.66f, 1f);

    static Font uiFont;

    [MenuItem(menuPath)]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[GameRuntimeUiScaffoldBuilder] Active scene is invalid.");
            return;
        }

        var mainCanvasObject = GameObject.Find("MainUiCanvas");
        if (mainCanvasObject == null)
        {
            Debug.LogError("[GameRuntimeUiScaffoldBuilder] MainUiCanvas not found in active scene.");
            return;
        }

        var mainCanvasRect = mainCanvasObject.GetComponent<RectTransform>();
        if (mainCanvasRect == null)
        {
            Debug.LogError("[GameRuntimeUiScaffoldBuilder] MainUiCanvas does not have RectTransform.");
            return;
        }

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var existingRoot = mainCanvasRect.Find(rootName);
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot.gameObject);

        var root = CreatePanel(rootName, mainCanvasRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, backgroundColor);

        BuildTop(root);
        BuildBoard(root);
        BuildAction(root);
        BuildBottom(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameRuntimeUiScaffoldBuilder] MainUiCanvas scaffold rebuilt and scene saved.");
    }

    static void BuildTop(RectTransform root)
    {
        var topPanel = CreatePanel("TopPanel", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -112f), new Vector2(-16f, -16f), panelColor);

        var turnPhaseText = CreateText("TurnPhaseText", topPanel, "Turn 1 | Turn Start", 28, TextAnchor.MiddleLeft, FontStyle.Bold, textPrimary);
        SetStretch(turnPhaseText.rectTransform, new Vector2(18f, -56f), new Vector2(-18f, -10f));

        var resourceText = CreateText("ResourceText", topPanel, "Defense 5   Stability 5   Gold 0   Situations 0", 20, TextAnchor.MiddleLeft, FontStyle.Normal, textMuted);
        SetStretch(resourceText.rectTransform, new Vector2(18f, -98f), new Vector2(-18f, -54f));
    }

    static void BuildBoard(RectTransform root)
    {
        var boardPanel = CreatePanel("BoardPanel", root, new Vector2(0f, 0f), new Vector2(0.72f, 1f), new Vector2(16f, 180f), new Vector2(-8f, -120f), panelColor);
        var boardTitle = CreateText("BoardTitle", boardPanel, "Situations", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(boardTitle.rectTransform, new Vector2(14f, -14f), new Vector2(280f, 34f), new Vector2(0f, 1f));

        CreateVerticalListPanel(boardPanel, "BoardList", new Vector2(12f, 12f), new Vector2(-12f, -56f), 10f);
    }

    static void BuildAction(RectTransform root)
    {
        var actionPanel = CreatePanel("ActionPanel", root, new Vector2(0.72f, 0f), new Vector2(1f, 1f), new Vector2(8f, 180f), new Vector2(-16f, -120f), panelColor);

        var actionTitle = CreateText("ActionTitle", actionPanel, "Adjustment & Controls", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(actionTitle.rectTransform, new Vector2(14f, -14f), new Vector2(420f, 34f), new Vector2(0f, 1f));

        var selectionText = CreateText("SelectionText", actionPanel, "Selected: Die - / Situation -", 16, TextAnchor.UpperLeft, FontStyle.Normal, textMuted);
        SetRect(selectionText.rectTransform, new Vector2(14f, -50f), new Vector2(520f, 30f), new Vector2(0f, 1f));

        var phaseHelpText = CreateText("PhaseHelpText", actionPanel, string.Empty, 15, TextAnchor.UpperLeft, FontStyle.Italic, textMuted);
        phaseHelpText.horizontalOverflow = HorizontalWrapMode.Wrap;
        phaseHelpText.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(phaseHelpText.rectTransform, new Vector2(14f, -82f), new Vector2(520f, 42f), new Vector2(0f, 1f));

        var controlsPanel = CreatePanel("ControlsPanel", actionPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -202f), new Vector2(-12f, -132f), panelSoftColor);
        var nextPhaseButton = CreateButton("NextPhaseButton", controlsPanel, "Start Assignment [SPACE]", buttonColor);
        SetRect(nextPhaseButton.GetComponent<RectTransform>(), new Vector2(10f, -10f), new Vector2(214f, 48f), new Vector2(0f, 1f));
        var restartButton = CreateButton("RestartButton", controlsPanel, "Restart [R]", buttonDangerColor);
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(232f, -10f), new Vector2(214f, 48f), new Vector2(0f, 1f));
        var unassignButton = CreateButton("UnassignButton", controlsPanel, "Unassign Die", buttonColor);
        SetRect(unassignButton.GetComponent<RectTransform>(), new Vector2(10f, -64f), new Vector2(214f, 44f), new Vector2(0f, 1f));
        var clearSelectionButton = CreateButton("ClearSelectionButton", controlsPanel, "Clear Selection", buttonColor);
        SetRect(clearSelectionButton.GetComponent<RectTransform>(), new Vector2(232f, -64f), new Vector2(214f, 44f), new Vector2(0f, 1f));

        var advisorTitle = CreateText("AdvisorTitle", actionPanel, "Advisors", 20, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(advisorTitle.rectTransform, new Vector2(14f, -212f), new Vector2(220f, 28f), new Vector2(0f, 1f));
        CreateVerticalListPanel(actionPanel, "AdvisorList", new Vector2(12f, 338f), new Vector2(-12f, -246f), 6f);

        var decreeTitle = CreateText("DecreeTitle", actionPanel, "Decrees", 20, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(decreeTitle.rectTransform, new Vector2(14f, 298f), new Vector2(220f, 28f), new Vector2(0f, 0f));
        CreateVerticalListPanel(actionPanel, "DecreeList", new Vector2(12f, 78f), new Vector2(-12f, 294f), 6f);

        var toastText = CreateText("ToastText", actionPanel, string.Empty, 15, TextAnchor.LowerCenter, FontStyle.Bold, textSuccess);
        SetRect(toastText.rectTransform, new Vector2(0f, 12f), new Vector2(520f, 26f), new Vector2(0.5f, 0f));
    }

    static void BuildBottom(RectTransform root)
    {
        var bottomPanel = CreatePanel("BottomPanel", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 164f), panelColor);

        var diceTitle = CreateText("DiceTitle", bottomPanel, "Dice Tray", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(diceTitle.rectTransform, new Vector2(14f, -14f), new Vector2(260f, 34f), new Vector2(0f, 1f));

        var diceHint = CreateText("DiceHint", bottomPanel, "Select die: 1-0 | Assignment: die 선택 후 Situation 클릭 | Advance: SPACE", 15, TextAnchor.UpperLeft, FontStyle.Normal, textMuted);
        SetRect(diceHint.rectTransform, new Vector2(280f, -18f), new Vector2(1200f, 28f), new Vector2(0f, 1f));

        CreateHorizontalListPanel(bottomPanel, "DiceList", new Vector2(12f, 12f), new Vector2(-12f, -52f), 8f);
    }

    static RectTransform CreateVerticalListPanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, float spacing)
    {
        var panel = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax, panelSoftColor);
        panel.gameObject.AddComponent<RectMask2D>();

        var scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = panel;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(panel, false);
        var content = contentObject.GetComponent<RectTransform>();
        SetStretch(content, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = content;
        return content;
    }

    static RectTransform CreateHorizontalListPanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, float spacing)
    {
        var panel = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax, panelSoftColor);
        panel.gameObject.AddComponent<RectMask2D>();

        var scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = panel;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(panel, false);
        var content = contentObject.GetComponent<RectTransform>();
        SetStretch(content, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = content;
        return content;
    }

    static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        panel.GetComponent<Image>().color = color;
        return rect;
    }

    static Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor, FontStyle style, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var textComp = textObject.GetComponent<Text>();
        textComp.font = uiFont;
        textComp.fontSize = size;
        textComp.alignment = anchor;
        textComp.fontStyle = style;
        textComp.color = color;
        textComp.text = text;
        textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComp.verticalOverflow = VerticalWrapMode.Truncate;
        return textComp;
    }

    static Button CreateButton(string name, Transform parent, string labelText, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.GetComponent<Image>();
        image.color = color;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var label = CreateText("Label", buttonObject.transform, labelText, 15, TextAnchor.MiddleCenter, FontStyle.Bold, textPrimary);
        SetStretch(label.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));
        return button;
    }

    static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
