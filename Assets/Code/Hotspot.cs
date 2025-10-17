using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System.Text;

public class Hotspot : MonoBehaviour, IPointerClickHandler
{
    [Header("Optional: leave empty to copy from parent")]
    public string id;

    [HideInInspector] public AnimalSpot animal;

    void Reset()
    {
        var img = GetComponent<Image>();
        if (!img) img = gameObject.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = true;

        if (!GetComponent<Button>()) gameObject.AddComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!animal) animal = GetComponentInParent<AnimalSpot>(true);

        // fallbacks if mis-parented
        if (!animal)
        {
            var all = Object.FindObjectsByType<AnimalSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 0)
            {
                string key = Normalize(id);
                if (!string.IsNullOrEmpty(key))
                {
                    animal = all.FirstOrDefault(a => Normalize(a.id) == key || Normalize(a.displayName) == key) ??
                             all.FirstOrDefault(a => Normalize(a.id).Contains(key) || Normalize(a.displayName).Contains(key));
                }
                if (!animal)
                {
                    animal = all
                        .OrderBy(a => (a.transform.position - transform.position).sqrMagnitude)
                        .FirstOrDefault();
                }
            }
        }

        if (animal != null && GameFlow.Instance != null)
        {
            Debug.Log($"[Hotspot] Click → {animal.displayName} ({animal.id})", this);
            GameFlow.Instance.OnAnimalClicked(animal);
        }
        else
        {
            Debug.LogWarning("[Hotspot] Clicked, but no AnimalSpot on parent.", this);
        }
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s) if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
        return sb.ToString();
    }
}
