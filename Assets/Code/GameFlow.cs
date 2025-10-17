using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    [Header("UI")]
    public JournalUI journal;

    [Header("Legacy/Optional")]
    public Transform hudSlotsParent;   // drag HUD_slots (optional)

    private readonly Dictionary<string, AnimalSpot> animals = new();
    private readonly Dictionary<string, HudSlot> slots = new();
    private readonly HashSet<string> found = new();

    // ---------- Alias map so different names resolve to the same key ----------
    private static readonly Dictionary<string, string> keyAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        { "stork", "blackneckedstork" },
        { "black-necked stork", "blackneckedstork" },
        { "black necked stork", "blackneckedstork" },
        { "blacknecked stork", "blackneckedstork" },

        // (These are no-ops but keep normalization consistent if your IDs ever vary)
        { "tree kangaroo", "treekangaroo" },
        { "tree_kangaroo", "treekangaroo" },
        { "green tree frog", "greentreefrog" },
        { "green_tree_frog", "greentreefrog" }
    };

    private static string ResolveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        key = key.Trim().ToLowerInvariant();
        if (keyAlias.TryGetValue(key, out var alias)) return alias;
        return key;
    }

    // ---------- Extra clues (merged at runtime) ----------
    private static readonly Dictionary<string, string[]> extraClues = new()
    {
        ["kangaroo"] = new[] {
            "I move by hopping on powerful back legs",
            "I carry my young in a pouch",
            "Large ears help me listen for danger",
            "I rest in shade during hot daytime hours",
            "You’ll spot me at dawn and dusk near open ground",
            "I am found in Australian grasslands and open woodlands",
            "I graze on grasses",
            "easily spotted for hopping locomotion and social mobs."
        },
        ["treekangaroo"] = new[] {
            "I’m a marsupial that climbs trees",
            "I have strong forelimbs and a long balancing tail",
            "My paws grip branches like a climber",
            "I live in dense rainforest",
            "You won’t see me much in open grassland",
            "I am found tropical rainforests of Queensland and New Guinea",
            "I feed on leaves and fruits",
            "I can move both in trees and on ground."
        },
        ["olivepython"] = new[] {
            "I’m a large non-venomous snake",
            "My scales look glossy olive-brown",
            "I often hunt near waterholes",
            "I’m most active at dusk or dawn",
            "I’m a constrictor, not a biter",
            "I am found in Northern Australia rocky areas and savannas",
            "I am non-venomous and I eat birds and mammals",
            "I am one of Australia’s largest snakes, olive-brown in color."
        },
        ["stork"] = new[] {
            "I’m a tall wetland bird with a heavy bill",
            "My neck shimmers blue-green in sunlight",
            "I wade slowly and spear fish",
            "I nest near healthy wetlands",
            "You might hear me bill-clatter",
            "I am found in wetlands and rivers worldwide (several species)",
            "I feed on fish, frogs, and small animals",
            "I am a large wading bird with long legs and beak."
        },
        ["greentreefrog"] = new[] {
            "I’m bright green with big toe pads for climbing",
            "I call loudly after rain",
            "I shelter in letterboxes and bathrooms!",
            "I like moist hideouts near houses",
            "I’m most vocal on warm nights",
            "I am found in wet forests and near swamps/urban areas in Australia",
            "I eat insects and I am recognizable by its bright green skin and croaking at night."
        },
        ["frillneckedlizard"] = new[] {
            "I open a big neck frill when startled",
            "I can run upright on my back legs",
            "I bask on branches to warm up",
            "I snap up insects with speed",
            "I prefer warm, dry woodland",
            "I am found in Northern Australia woodlands" 
        },
        ["kingfisher"] = new[] {
            "I dive for fish from a perch",
            "I have a short tail and a big straight bill",
            "I watch patiently, then strike fast",
            "I sit near creeks and rivers",
            "Some of my cousins eat insects too",
            "I am found near rivers, lakes, and coastal regions",
            "I eat fish and insects",
            "I am very colorful plumage and known for diving into water"
        }
    };

    // --------- default facts & tips (rich) ----------
    private static readonly Dictionary<string, string[]> defaultFacts = new()
    {
        ["kangaroo"] = new[] {
            "Red kangaroos are the largest living marsupials.",
            "Elastic leg tendons store energy for efficient hopping.",
            "A strong tail acts like a fifth leg when walking slowly.",
            "Females can pause embryo development during drought.",
            "Joeys shelter in the pouch for many months.",
            "Ears swivel to locate sounds.",
            "Most active at dawn and dusk (crepuscular).",
            "Prefer open country with scattered shade trees.",
            "Grazers that shape grassland vegetation.",
            "‘Mobs’ help detect predators."
        },
        ["treekangaroo"] = new[] {
            "Tree-kangaroos evolved from ground-dwelling roos.",
            "Powerful forelimbs and curved claws for climbing.",
            "Long tails help with balance on branches.",
            "Can leap several metres down from trees.",
            "Walk slowly on the ground compared with wallabies.",
            "Many species occupy small rainforest ranges.",
            "Mostly solitary and secretive in dense canopy.",
            "Browse leaves, fruit and flowers in the trees.",
            "Habitat fragmentation is a major threat.",
            "Cultural significance in parts of New Guinea."
        },
        ["olivepython"] = new[] {
            "One of Australia’s largest pythons; non-venomous constrictor.",
            "Adults commonly exceed 3 m; some pass 4 m.",
            "Glossy olive-brown scales with a pale belly.",
            "Often hunts near rock gorges and waterholes.",
            "Good swimmer that may take fish as well as mammals.",
            "Most active at dusk and night in hot weather.",
            "Constricts prey with coils, not venom.",
            "Egg-laying; females guard clutches until hatching.",
            "Tolerant of arid inland habitats if water is nearby.",
            "Important predator controlling rodent numbers."
        },
        ["blackneckedstork"] = new[] {
            "Also called the jabiru in Australia.",
            "Iridescent blue-green head and neck; heavy black bill.",
            "Tall wader that spears fish, frogs and eels.",
            "Pairs defend large wetland territories.",
            "Nests are big stick platforms high in trees.",
            "Slow, deliberate foraging along shallow edges.",
            "Sensitive to wetland drainage and pollution.",
            "Often seen alone or as a territorial pair.",
            "Takes crustaceans where mangroves occur.",
            "Requires intact floodplain wetlands to breed."
        },
        // duplicate entry so the simple key “stork” works too
        ["stork"] = new[] {
            "Also called the jabiru in Australia.",
            "Iridescent blue-green head and neck; heavy black bill.",
            "Tall wader that spears fish, frogs and eels.",
            "Pairs defend large wetland territories.",
            "Nests are big stick platforms high in trees.",
            "Slow, deliberate foraging along shallow edges.",
            "Sensitive to wetland drainage and pollution.",
            "Often seen alone or as a territorial pair.",
            "Takes crustaceans where mangroves occur.",
            "Requires intact floodplain wetlands to breed."
        },
        ["greentreefrog"] = new[] {
            "A familiar Australian frog often near houses.",
            "Large toe pads help climb glass and walls.",
            "Loud calls after summer rain carry long distances.",
            "Shelters in pipes, letterboxes and bathrooms.",
            "Can live many years; some appear bluish due to pigment shifts.",
            "Feeds on insects and other invertebrates at night.",
            "Needs clean water and moist refuges to thrive.",
            "Often coexists with people where chemicals are limited.",
            "Active on warm, humid nights; hides by day.",
            "An indicator of local water quality."
        },
        ["frillneckedlizard"] = new[] {
            "Famous for the large neck frill used to startle predators.",
            "When threatened it opens the mouth, raises the frill and hisses.",
            "Can sprint short distances upright on hind legs.",
            "Mostly arboreal but forages for insects on the ground.",
            "Basks on trunks and branches in open woodland.",
            "Relies on camouflage when motionless on bark.",
            "The frill is supported by long flexible cartilage spines.",
            "Active in the warmer months; shelters in trees or burrows.",
            "Feeds mainly on beetles, ants and small vertebrates.",
            "Loss of large trees reduces suitable habitat."
        },
        ["kingfisher"] = new[] {
            "Kingfishers watch patiently then dive head-first to catch prey.",
            "Not all kingfishers eat only fish; many take insects and lizards.",
            "Short tails and long straight bills are characteristic.",
            "Often nest in burrows excavated into riverbanks.",
            "Excellent eyesight compensates for water refraction.",
            "Use exposed perches overlooking water or open ground.",
            "Territorial calls are common at dawn.",
            "Some species live far from water, hunting insects.",
            "Bank erosion can destroy active nest burrows.",
            "Dead branches (‘snags’) provide perfect perches."
        }
    };

    private static readonly Dictionary<string, string[]> defaultTips = new()
    {
        ["kangaroo"] = new[] {
            "Drive carefully at dawn/dusk; never swerve blindly.",
            "Keep dogs on leads in wildlife habitat.",
            "Provide wildlife-friendly fencing or ground-level gaps.",
            "Do not feed wild kangaroos; human food can harm.",
            "Report injured wildlife to local carers.",
            "Slow down on rural roads at night; watch verge movement.",
            "Support habitat restoration of native grasslands.",
            "Place shallow water trays with escape sticks in heatwaves."
        },
        ["treekangaroo"] = new[] {
            "Protect and reconnect rainforest corridors.",
            "Keep dogs and cats away from forest edges.",
            "Support sustainable forestry and anti-poaching.",
            "Plant native rainforest trees in suitable regions.",
            "Avoid disturbing hollow-bearing trees.",
            "Back Indigenous-led land management projects."
        },
        ["olivepython"] = new[] {
            "Give snakes space—call a licensed remover if needed.",
            "Secure chicken coops and pet enclosures at night.",
            "Seal shed gaps and keep yards tidy to reduce rodents.",
            "Avoid glue traps; they injure non-target wildlife.",
            "Educate neighbours about non-venomous species.",
            "Leave water access during extreme heat."
        },
        ["blackneckedstork"] = new[] {
            "Protect and restore wetlands; prevent pollution.",
            "Dispose of fishing line and hooks responsibly.",
            "Keep a respectful distance from nesting trees.",
            "Support floodplain re-watering where feasible.",
            "Plant native sedges and rushes along banks."
        },
        // duplicate for “stork”
        ["stork"] = new[] {
            "Protect and restore wetlands; prevent pollution.",
            "Dispose of fishing line and hooks responsibly.",
            "Keep a respectful distance from nesting trees.",
            "Support floodplain re-watering where feasible.",
            "Plant native sedges and rushes along banks."
        },
        ["greentreefrog"] = new[] {
            "Avoid pesticides and snail pellets near water.",
            "Provide clean water and native plants for shelter.",
            "Keep cats indoors at night; add ‘escape ramps’ to ponds.",
            "Build simple ‘frog hotels’ with upright pipes.",
            "Do not move frogs between areas (disease risk)."
        },
        ["frillneckedlizard"] = new[] {
            "Retain standing trees and fallen logs for refuge.",
            "Drive slowly on warm days—lizards bask on tracks.",
            "Keep cats and dogs under control near bushland.",
            "Avoid broad-spectrum insecticides that reduce prey.",
            "Do not collect wild lizards as pets."
        },
        ["kingfisher"] = new[] {
            "Maintain native vegetation along streams.",
            "Keep waterways clean—no litter or chemical runoff.",
            "Protect riverbanks with active nest burrows.",
            "Leave natural perches and snags near water.",
            "Use barbless hooks where legal; retrieve lost tackle."
        }
    };

    // ----------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!journal)
            journal = UnityEngine.Object.FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
        if (journal) journal.Init();

        if (!hudSlotsParent)
        {
            var go = GameObject.Find("HUD_slots");
            if (go) hudSlotsParent = go.transform;
        }

        // Animals + hotspots
        foreach (var a in UnityEngine.Object.FindObjectsByType<AnimalSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!a) continue;

            if (string.IsNullOrWhiteSpace(a.id))
                a.id = a.gameObject.name.Replace("Animal_", "").Replace("Spr_", "").Trim().ToLowerInvariant();

            var key = ResolveKey(a.id);
            if (!animals.ContainsKey(key)) animals.Add(key, a);

            var hs = a.GetComponentInChildren<Hotspot>(true);
            if (!hs)
            {
                var hot = new GameObject("Hotspot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Hotspot));
                var rt = hot.GetComponent<RectTransform>();
                rt.SetParent(a.transform, false);
                rt.anchorMin = new Vector2(0.3f, 0.3f);
                rt.anchorMax = new Vector2(0.7f, 0.7f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var img = hot.GetComponent<Image>();
                img.color = new Color(1, 0, 0, 0f);
                img.raycastTarget = true;

                hs = hot.GetComponent<Hotspot>();
            }
            hs.animal = a;
            if (string.IsNullOrEmpty(hs.id)) hs.id = a.id;
        }

        // HUD slots
        HudSlot[] foundSlots = hudSlotsParent
            ? hudSlotsParent.GetComponentsInChildren<HudSlot>(true)
            : UnityEngine.Object.FindObjectsByType<HudSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (foundSlots != null && foundSlots.Length > 0)
        {
            foreach (var s in foundSlots)
            {
                if (!s || string.IsNullOrWhiteSpace(s.id)) continue;
                var key = ResolveKey(s.id);
                if (!slots.ContainsKey(key)) slots.Add(key, s);
            }

            // Fill blank slots automatically with any unmapped animals
            var blanks = foundSlots.Where(s => s && string.IsNullOrWhiteSpace(s.id)).ToList();
            if (blanks.Count > 0)
            {
                var keys = animals.Keys.ToList();
                int i = 0;
                foreach (var k in keys)
                {
                    if (slots.ContainsKey(k)) continue;
                    if (i >= blanks.Count) break;
                    blanks[i].id = k;
                    slots[k] = blanks[i];
                    i++;
                }
            }
        }

        Debug.Log("[GameFlow] Animals: " + string.Join(", ", animals.Keys.ToArray()));
        Debug.Log("[GameFlow] HUD slots: " + string.Join(", ", slots.Keys.ToArray()));
    }

    // Called by Hotspot
    public void OnAnimalClicked(AnimalSpot a)
    {
        if (!a || !journal) return;

        var key = ResolveKey(a.id);

        // already found? open facts
        if (found.Contains(key) || (slots.TryGetValue(key, out var s) && s != null && s.IsRevealed))
        {
            OpenFacts(a);
            return;
        }

        var mergedClues = GetCluesFor(a, key);
        var built = BuildOptions(a);
        var optionLabels = built.labels;
        a.correctIndex = built.correctIndex;

        var sprite = GetAnimalSprite(a);

        journal.ShowClues(
            title: "",                     // title hidden per your latest layout
            clues: mergedClues,
            options: optionLabels,
            onPick: ix => OnAnswered(a, ix),
            preview: sprite
        );
    }

    private void OpenFacts(AnimalSpot a)
    {
        if (!a || !journal) return;
        var key = ResolveKey(a.id);

        string[] facts = (a.funFacts != null && a.funFacts.Length > 0)
                         ? a.funFacts
                         : GetFromDict(defaultFacts, key);

        string[] tips = (a.saveTips != null && a.saveTips.Length > 0)
                         ? a.saveTips
                         : GetFromDict(defaultTips, key);

        // Always ensure something shows
        if (facts == null || facts.Length == 0)
            facts = new[] { "This animal is special to local ecosystems.", "Look for its shape, colours and behaviour!" };
        if (tips == null || tips.Length == 0)
            tips = new[] { "Protect habitats and keep areas clean.", "Observe quietly and never disturb wildlife." };

        var sprite = GetAnimalSprite(a);
        journal.ShowFacts(a.displayName, facts, tips, sprite);
        Debug.Log($"[GameFlow] ShowFacts → {a.displayName} ({key})");
    }

    // Safe dictionary fetch with alias
    private static string[] GetFromDict(Dictionary<string, string[]> dict, string key)
    {
        key = ResolveKey(key);
        if (dict.TryGetValue(key, out var arr)) return arr;
        return Array.Empty<string>();
    }

    private Sprite GetAnimalSprite(AnimalSpot a)
    {
        if (a.hudIcon) return a.hudIcon;

        var img = a.GetComponentsInChildren<Image>(true)
                   .FirstOrDefault(i => i && i.sprite && i.gameObject.name.StartsWith("Spr_", StringComparison.OrdinalIgnoreCase));
        if (img) return img.sprite;

        img = a.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i && i.sprite);
        return img ? img.sprite : null;
    }

    private void OnAnswered(AnimalSpot a, int picked)
    {
        if (picked == a.correctIndex)
        {
            var key = ResolveKey(a.id);
            found.Add(key);

            if (slots.TryGetValue(key, out var slot) && slot != null)
                slot.Reveal(GetAnimalSprite(a));
            else
                Debug.LogWarning($"[GameFlow] No HUD slot for '{a.id}'.");

            journal.CloseBook();

        }
        else
        {
            journal.NudgeWrong();
        }
    }

    // Called by HUD icon click
    public void OnHudIconClicked(string id)
    {
        var key = ResolveKey(id);
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[GameFlow] HUD click with empty id.");
            return;
        }

        AnimalSpot a;
        if (!animals.TryGetValue(key, out a))
        {
            // fallback by displayName
            var normKey = Normalize(key);
            a = animals.Values.FirstOrDefault(an => Normalize(an.displayName) == normKey);
        }

        if (a == null)
        {
            Debug.LogWarning($"[GameFlow] HUD click: no animal mapped for id '{key}'.");
            return;
        }

        OpenFacts(a);
    }

    // ----- helpers -----
    (string[] labels, int correctIndex) BuildOptions(AnimalSpot a)
    {
        var preset = (a.options ?? Array.Empty<OptionItem>())
                     .Select(o => o?.label ?? "")
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .ToArray();

        if (preset.Length == 3)
        {
            a.correctIndex = Mathf.Clamp(a.correctIndex, 0, 2);
            return (preset, a.correctIndex);
        }

        var correct = string.IsNullOrWhiteSpace(a.displayName) ? a.id : a.displayName;

        var others = animals.Values
            .Where(x => x != null && x != a)
            .Select(x => string.IsNullOrWhiteSpace(x.displayName) ? x.id : x.displayName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string[] backup = { "Goanna", "Kookaburra", "Heron", "Wallaby", "Emu", "Carpet Python", "Brown Snake" };
        foreach (var b in backup)
            if (!others.Contains(b, StringComparer.OrdinalIgnoreCase) && !b.Equals(correct, StringComparison.OrdinalIgnoreCase))
                others.Add(b);

        System.Random rng = new System.Random();
        var wrongs = others.OrderBy(_ => rng.Next()).Take(2).ToList();

        var list = new List<string> { correct };
        list.AddRange(wrongs);

        var shuffled = list.OrderBy(_ => rng.Next()).ToArray();
        int newCorrectIndex = Array.FindIndex(shuffled, s => s.Equals(correct, StringComparison.OrdinalIgnoreCase));

        return (shuffled, newCorrectIndex < 0 ? 0 : newCorrectIndex);
    }

    string[] GetCluesFor(AnimalSpot a, string key)
    {
        key = ResolveKey(key);
        var merged = MergeUnique(a.clues,
            extraClues.TryGetValue(key, out var extra) ? extra : null);

        if (merged == null || merged.Length == 0)
            merged = new[] { "Look closely at my shape and colors.", "Check where I’m hiding in the scene." };

        return merged;
    }

    string[] MergeUnique(string[] original, string[] extra)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (original != null) foreach (var s in original) if (!string.IsNullOrWhiteSpace(s)) set.Add(s.Trim());
        if (extra != null) foreach (var s in extra) if (!string.IsNullOrWhiteSpace(s)) set.Add(s.Trim());
        return set.ToArray();
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                sb.Append(ch);
        return sb.ToString();
    }
}
