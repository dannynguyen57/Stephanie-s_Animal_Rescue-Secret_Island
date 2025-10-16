using UnityEngine;
using System.Collections;

public class ScreenManager : MonoBehaviour
{
    public CanvasGroup screenWelcome;
    public CanvasGroup screenBoat;
    public CanvasGroup screenGame;
    public float fadeTime = 0.25f;

    void Start()
    {
        Show(screenWelcome);
        Hide(screenBoat);
        Hide(screenGame);
    }

    public void ToBoat() => StartCoroutine(Swap(screenWelcome, screenBoat));
    public void ToGame() => StartCoroutine(Swap(screenBoat, screenGame));

    IEnumerator Swap(CanvasGroup from, CanvasGroup to)
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            from.alpha = 1 - t / fadeTime;
            yield return null;
        }
        Hide(from);

        Show(to);
        t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            to.alpha = t / fadeTime;
            yield return null;
        }
        to.alpha = 1;
    }

    void Show(CanvasGroup cg)
    {
        if (!cg) return;
        cg.gameObject.SetActive(true);
        cg.blocksRaycasts = cg.interactable = true;
        cg.alpha = 1;
    }

    void Hide(CanvasGroup cg)
    {
        if (!cg) return;
        cg.alpha = 0;
        cg.blocksRaycasts = cg.interactable = false;
        cg.gameObject.SetActive(false);
    }
}
