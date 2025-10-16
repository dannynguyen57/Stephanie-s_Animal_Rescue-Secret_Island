using UnityEngine;
using UnityEngine.UI;
using System;

[System.Serializable]
public class OptionItem
{
    public string label;
    public Sprite sketch; // optional
}

public class AnimalSpot : MonoBehaviour
{
    [Header("Identity")]
    public string id;                // e.g. "kangaroo"
    public string displayName;       // e.g. "Kangaroo"

    [Header("HUD")]
    public Sprite hudIcon;           // shown on HUD when answered correctly

    [Header("Quiz")]
    [TextArea] public string[] clues;
    public OptionItem[] options = new OptionItem[3];
    [Range(0, 2)] public int correctIndex = 0;

    [Header("Facts")]
    [TextArea] public string[] funFacts;
    [TextArea] public string[] saveTips;

    void Awake()
    {
        // --- id ---
        if (string.IsNullOrWhiteSpace(id))
        {
            var nm = gameObject.name.Replace("Animal_", "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("Spr_", "", StringComparison.OrdinalIgnoreCase)
                                    .Trim();
            id = nm.Replace(" ", "").ToLowerInvariant();
        }

        // --- displayName (pretty) ---
        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.StartsWith("Animal_", StringComparison.OrdinalIgnoreCase) ||
            displayName.StartsWith("Spr_", StringComparison.OrdinalIgnoreCase))
        {
            displayName = ToPretty(id);
        }

        // --- hotspot ---
        var hotspot = GetComponentInChildren<Hotspot>(true);
        if (hotspot == null)
        {
            var go = new GameObject("Hotspot",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(Hotspot));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.3f);
            rt.anchorMax = new Vector2(0.7f, 0.7f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            hotspot = go.GetComponent<Hotspot>();
        }

        hotspot.animal = this;
        if (string.IsNullOrEmpty(hotspot.id)) hotspot.id = id;
    }

    // Simple pretty-name from id (with a small map for tricky names)
    static string ToPretty(string key)
    {
        key = (key ?? "").Trim().ToLowerInvariant();
        switch (key)
        {
            case "blackneckedstork": return "Black-necked Stork";
            case "greentreefrog": return "Green Tree Frog";
            case "frillneckedlizard": return "Frill-necked Lizard";
            case "treekangaroo": return "Tree Kangaroo";
            case "olivepython": return "Olive Python";
            case "kingfisher": return "Kingfisher";
            case "kangaroo": return "Kangaroo";
        }

        // fallback Title Case split by spaces/underscores/dashes
        var raw = key.Replace("_", " ").Replace("-", " ").Trim();
        if (string.IsNullOrEmpty(raw)) return key;
        var parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var w = parts[i];
            parts[i] = char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1) : "");
        }
        return string.Join(" ", parts);
    }
}
