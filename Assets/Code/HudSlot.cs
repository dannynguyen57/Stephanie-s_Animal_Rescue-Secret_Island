using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HudSlot : MonoBehaviour
{
    [Header("Match this to AnimalSpot.id (lowercase)")]
    public string id; // e.g. "kangaroo"

    [Header("Icon (auto-created as a child that fills the slot)")]
    public Image icon;                 // child Image we create/use

    [Tooltip("Optional cover/overlay above the icon (hidden on reveal).")]
    public Image overlay;

    private Button btn;
    public bool IsRevealed { get; private set; }

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }

        EnsureIconChild();     // makes an "Icon" child that STRETCHES to the slot
        SetIconVisible(false); // start hidden
        IsRevealed = false;
    }

    // Create/find a child named "Icon" that fills the slot (no aspect fitter; no padding)
    void EnsureIconChild()
    {
        var slotRT = transform as RectTransform;

        // find or create child
        RectTransform iconRT = null;
        var t = transform.Find("Icon");
        if (t) iconRT = t as RectTransform;
        if (!iconRT)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconRT = go.GetComponent<RectTransform>();
            iconRT.SetParent(slotRT, false);
        }

        // full-stretch to parent rect (no padding)
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        iconRT.pivot = new Vector2(0.5f, 0.5f);

        // if an AspectRatioFitter slipped in earlier, remove it so it can’t fight the layout
        var fitter = iconRT.GetComponent<AspectRatioFitter>();
        if (fitter) Destroy(fitter);

        icon = iconRT.GetComponent<Image>();
        icon.type = Image.Type.Simple;
        icon.preserveAspect = false;      // <-- IMPORTANT: fill the slot exactly
        icon.raycastTarget = false;      // Button handles clicks
        icon.enabled = true;
        icon.color = new Color(1, 1, 1, 0);
    }

    /// Preload the sprite (still hidden).
    public void Seed(Sprite s)
    {
        EnsureIconChild();
        icon.sprite = s;
        if (!IsRevealed) SetIconVisible(false);
        Debug.Log($"[HUD] Seeded '{id}' with sprite: {(s ? s.name : "NULL")}", this);
    }

    /// Reveal when the animal is found.
    public void Reveal(Sprite colouredIcon)
    {
        EnsureIconChild();

        if (colouredIcon) icon.sprite = colouredIcon;

        SetIconVisible(true);
        if (overlay) overlay.gameObject.SetActive(false);

        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;

        Canvas.ForceUpdateCanvases();
        IsRevealed = true;
        Debug.Log($"[HUD] Revealed '{id}' (sprite: {(icon.sprite ? icon.sprite.name : "NULL")}).", this);
    }

    private void SetIconVisible(bool on)
    {
        if (!icon) return;
        var c = icon.color;
        c.a = on ? 1f : 0f;
        icon.color = c;
    }

    private void OnClick()
    {
        if (IsRevealed && GameFlow.Instance != null)
            GameFlow.Instance.OnHudIconClicked(id);
        else
            Debug.Log($"[HUD] Click on '{id}' ignored (not revealed yet).", this);
    }
}
