using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JournalUI : MonoBehaviour
{
    [Header("Existing scene objects (optional)")]
    public GameObject coverPanel;     // "Journal_Cover" (optional; background fallback)
    public GameObject bookPanel;      // "Journal_Book"  (optional; background source)

    [Header("Controls (created at runtime)")]
    public Button btnClose;                   // built at runtime (top-right)
    public Sprite panelBackgroundSprite;      // uses Journal_Book sprite if present

    [Header("Runtime UI (built)")]
    public TMP_Text titleText;         // left page header
    public TMP_Text cluesText;         // left page (quiz)
    public TMP_Text factsText;         // left page (facts)
    public Button[] optionButtons = new Button[3]; // right page (quiz options)
    public Image previewImage;         // right page (animal image)
    public TMP_Text tipsText;          // right page (facts: “How to help”)

    RectTransform panel;
    CanvasGroup panelCg;
    Canvas panelCanvas;

    RectTransform leftArea;
    RectTransform rightArea;
    RectTransform previewRT;
    RectTransform optionsRT;

    public bool enableEscapeToClose = true;

    // Call once by GameFlow
    public void Init()
    {
        if (!coverPanel)
        {
            var c = transform.Find("Journal_Cover");
            if (c) coverPanel = c.gameObject;
        }
        if (!bookPanel)
        {
            var b = transform.Find("Journal_Book");
            if (b) bookPanel = b.gameObject;
        }

        // prefer the book sprite as background
        if (bookPanel && bookPanel.TryGetComponent<Image>(out var bookImg) && bookImg.sprite)
            panelBackgroundSprite = bookImg.sprite;
        else if (!panelBackgroundSprite && coverPanel && coverPanel.TryGetComponent<Image>(out var coverImg) && coverImg.sprite)
            panelBackgroundSprite = coverImg.sprite;

        if (coverPanel && coverPanel.TryGetComponent<Image>(out var cimg)) cimg.raycastTarget = false;

        BuildSimplePanel();
        CloseBook();
    }

    void Update()
    {
        if (!enableEscapeToClose || !panel) return;
        if (panel.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseBook();
    }

    // ============================== QUIZ ===============================
    public void ShowClues(string title, string[] clues, string[] options, Action<int> onPick, Sprite preview = null)
    {
        OpenPanel();

        if (titleText) titleText.text = ""; // hide animal name during quiz

        if (cluesText)
        {
            var lines = (clues ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s));
            cluesText.text = "• " + string.Join("\n• ", lines);
            cluesText.gameObject.SetActive(true);
        }

        if (factsText) { factsText.text = ""; factsText.gameObject.SetActive(false); }

        SetLayoutForQuiz();

        if (previewImage)
        {
            if (preview)
            {
                previewImage.sprite = preview;
                previewImage.enabled = true;
                previewImage.gameObject.SetActive(true);
            }
            else
            {
                previewImage.enabled = false;
                previewImage.gameObject.SetActive(false);
            }
        }

        if (tipsText) { tipsText.text = ""; tipsText.gameObject.SetActive(false); }

        EnsureButtonsArray();
        for (int i = 0; i < 3; i++)
        {
            var btn = optionButtons[i];
            if (!btn) continue;

            string label = (options != null && i < options.Length) ? options[i] : "";
            btn.gameObject.SetActive(!string.IsNullOrEmpty(label));

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.text = label;

            int ix = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onPick?.Invoke(ix));
        }

        Debug.Log($"[JournalUI] ShowClues • options=[{string.Join(", ", options ?? Array.Empty<string>())}]");
    }

    // ============================== FACTS ==============================
    public void ShowFacts(string title, string[] facts, string[] tips, Sprite preview = null)
    {
        OpenPanel();

        if (titleText) titleText.text = $"{title} — Facts";

        if (factsText)
        {
            var sb = new StringBuilder();
            if (facts != null)
            {
                foreach (var f in facts)
                    if (!string.IsNullOrWhiteSpace(f)) sb.AppendLine("• " + f);
            }
            factsText.text = sb.ToString();
            factsText.gameObject.SetActive(true);
        }

        if (cluesText) { cluesText.text = ""; cluesText.gameObject.SetActive(false); }

        SetLayoutForFacts();

        if (previewImage)
        {
            if (preview)
            {
                previewImage.sprite = preview;
                previewImage.enabled = true;
                previewImage.gameObject.SetActive(true);
            }
            else
            {
                previewImage.enabled = false;
                previewImage.gameObject.SetActive(false);
            }
        }

        if (tipsText)
        {
            var sb = new StringBuilder();
            if (tips != null && tips.Length > 0)
            {
                sb.AppendLine("<b>How to help</b>");
                foreach (var t in tips) if (!string.IsNullOrWhiteSpace(t)) sb.AppendLine("• " + t);
            }
            tipsText.text = sb.ToString();
            tipsText.gameObject.SetActive(true);
        }

        EnsureButtonsArray();
        for (int i = 0; i < 3; i++)
            if (optionButtons[i]) optionButtons[i].gameObject.SetActive(false);

        Debug.Log($"[JournalUI] ShowFacts • title='{title}'");
    }

    public void CloseBook()
    {
        if (panel)
        {
            panel.gameObject.SetActive(false);
            if (panelCg)
            {
                panelCg.alpha = 0f;
                panelCg.interactable = false;
                panelCg.blocksRaycasts = false;
            }
        }
        if (coverPanel) coverPanel.SetActive(true);
        if (bookPanel) bookPanel.SetActive(false);
    }

    // ============================ BUILD PANEL ==========================
    void OpenPanel()
    {
        if (coverPanel) coverPanel.SetActive(false);
        if (bookPanel) bookPanel.SetActive(false);

        if (!panel) BuildSimplePanel();

        if (!panelCanvas) panelCanvas = panel.GetComponent<Canvas>();
        panelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 5000;

        panel.gameObject.SetActive(true);
        if (panelCg)
        {
            panelCg.alpha = 1f;
            panelCg.interactable = true;
            panelCg.blocksRaycasts = true;
        }
    }

    void BuildSimplePanel()
    {
        var existing = GameObject.Find("_JournalPanel");
        if (existing) DestroyImmediate(existing);

        var go = new GameObject(
            "_JournalPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(CanvasScaler)
        );

        panel = go.GetComponent<RectTransform>();
        panel.SetParent(null, false);

        panel.anchorMin = new Vector2(0.10f, 0.075f);
        panel.anchorMax = new Vector2(0.90f, 0.925f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;

        var bg = go.GetComponent<Image>();
        if (panelBackgroundSprite)
        {
            bg.sprite = panelBackgroundSprite;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(1f, 1f, 1f, 0.92f);
        }
        bg.raycastTarget = false;

        panelCg = go.GetComponent<CanvasGroup>();

        panelCanvas = go.GetComponent<Canvas>();
        panelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 5000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Close button (use 'X' so any TMP font can render it)
        var close = CreateButton(panel, "Btn_ClosePanel");
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.972f, 0.945f);
        crt.anchorMax = new Vector2(0.992f, 0.985f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        var label = close.GetComponentInChildren<TextMeshProUGUI>();
        label.text = "X";
        label.fontSize = 28;
        close.onClick.AddListener(CloseBook);

        // Left & Right areas
        leftArea = CreateRect("Left", panel, new Vector2(0.05f, 0.06f), new Vector2(0.50f, 0.94f));
        rightArea = CreateRect("Right", panel, new Vector2(0.52f, 0.06f), new Vector2(0.95f, 0.94f));

        // Title (80–95% band)
        titleText = CreateTMP("Title", leftArea, 46, TextAlignmentOptions.MidlineLeft);
        {
            var rt = titleText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.80f);
            rt.anchorMax = new Vector2(0.98f, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Clues band (quiz)
        cluesText = CreateTMP("Clues", leftArea, 32, TextAlignmentOptions.TopLeft);
        {
            var rt = cluesText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.05f);
            rt.anchorMax = new Vector2(0.98f, 0.78f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            cluesText.textWrappingMode = TextWrappingModes.Normal;
        }

        // Facts band (facts)
        factsText = CreateTMP("Facts", leftArea, 30, TextAlignmentOptions.TopLeft);
        {
            var rt = factsText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.05f);
            rt.anchorMax = new Vector2(0.98f, 0.78f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            factsText.textWrappingMode = TextWrappingModes.Normal;
            factsText.gameObject.SetActive(false);
        }

        // Right page: preview (top)
        var prevContainer = CreateRect("Preview", rightArea, new Vector2(0.10f, 0.55f), new Vector2(0.90f, 0.95f));
        var prevGo = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        var prevImgRt = prevGo.GetComponent<RectTransform>();
        prevImgRt.SetParent(prevContainer, false);
        prevImgRt.anchorMin = new Vector2(0.15f, 0.15f);
        prevImgRt.anchorMax = new Vector2(0.85f, 0.85f);
        prevImgRt.offsetMin = prevImgRt.offsetMax = Vector2.zero;
        previewImage = prevGo.GetComponent<Image>();
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;
        previewRT = prevContainer;

        // Options (quiz)
        optionsRT = CreateRect("Options", rightArea, new Vector2(0.0f, 0.25f), new Vector2(1.0f, 0.52f));
        EnsureButtonsArray();
        for (int i = 0; i < 3; i++)
        {
            var btn = CreateButton(optionsRT, $"Option_{(char)('A' + i)}");
            var brt = btn.GetComponent<RectTransform>();
            float rowH = 1f / 3f;
            float yMin = 1f - (i + 1) * rowH;
            float yMax = 1f - i * rowH;
            brt.anchorMin = new Vector2(0.0f, yMin + 0.05f);
            brt.anchorMax = new Vector2(1.0f, yMax - 0.05f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.fontSize = 34;

            optionButtons[i] = btn;
        }

        // Tips (facts)
        tipsText = CreateTMP("Tips", rightArea, 28, TextAlignmentOptions.TopLeft);
        {
            var rt = tipsText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.45f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            tipsText.textWrappingMode = TextWrappingModes.Normal;
            tipsText.gameObject.SetActive(false);
        }
    }

    // ============================ LAYOUT MODES =========================
    void SetLayoutForQuiz()
    {
        if (optionsRT) optionsRT.gameObject.SetActive(true);
        if (previewRT) previewRT.gameObject.SetActive(true);
        if (tipsText) tipsText.gameObject.SetActive(false);
    }

    void SetLayoutForFacts()
    {
        if (optionsRT) optionsRT.gameObject.SetActive(false);
        if (previewRT) previewRT.gameObject.SetActive(true);
        if (tipsText) tipsText.gameObject.SetActive(true);
    }

    // =============================== HELPERS ===========================
    RectTransform CreateRect(string name, RectTransform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    TMP_Text CreateTMP(string name, RectTransform parent, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = Color.black;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    Button CreateButton(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.85f, 0.9f, 1f, 0.95f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lrt = label.GetComponent<RectTransform>();
        lrt.SetParent(go.transform, false);
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "Option";
        tmp.fontSize = 34;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    void EnsureButtonsArray()
    {
        if (optionButtons == null || optionButtons.Length != 3)
            optionButtons = new Button[3];
    }
    // Simple visual cue when the player picks a wrong option.
    public void NudgeWrong()
    {
        // If there’s a title area, show a quick “Try again” prompt.
        if (titleText != null)
            titleText.text = "Try again!";

        // Light emphasis on the clues block too (optional).
        if (cluesText != null && !string.IsNullOrEmpty(cluesText.text))
            cluesText.text = cluesText.text + "\n\n(That wasn’t it — look closely and try again.)";
    }

}
