using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JournalOpenAnimator : MonoBehaviour
{
    public RectTransform closedBook;
    public GameObject journalPanel;
    public float slideDuration = 0.6f;
    public Vector2 startPos = new Vector2(0, 800);
    public Vector2 endPos = new Vector2(0, 0);
    public float swingAmplitude = 10f; // degrees of swing
    public KeyCode toggleKey = KeyCode.K;

    private bool isOpen = false;
    private bool isAnimating = false;

    void Start()
    {
        closedBook.anchoredPosition = startPos;
        closedBook.rotation = Quaternion.identity;
        journalPanel.SetActive(false);
        closedBook.gameObject.SetActive(false);
        Debug.Log("[JournalAnimator] Initialized. Journal closed and off-screen.");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !isAnimating)
        {
            Debug.Log($"[JournalAnimator] Toggle key pressed. isOpen = {isOpen}");
            if (!isOpen)
                StartCoroutine(OpenJournal());
            else
                StartCoroutine(CloseJournalAnim());
        }
    }

    IEnumerator OpenJournal()
    {
        Debug.Log("[JournalAnimator] Starting OPEN animation...");
        isAnimating = true;
        closedBook.gameObject.SetActive(true);
        journalPanel.SetActive(false);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);

            // Slide down
            closedBook.anchoredPosition = Vector2.Lerp(startPos, endPos, bounce);

            // Swing: full swing mid-way, back to neutral at end
            float angle = Mathf.Sin(t * Mathf.PI) * swingAmplitude;
            closedBook.rotation = Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        closedBook.rotation = Quaternion.identity;
        closedBook.gameObject.SetActive(false);
        journalPanel.SetActive(true);

        Debug.Log("[JournalAnimator] OPEN animation complete. Showing journal panel.");
        isOpen = true;
        isAnimating = false;
    }

    IEnumerator CloseJournalAnim()
    {
        Debug.Log("[JournalAnimator] Starting CLOSE animation...");
        isAnimating = true;
        journalPanel.SetActive(false);
        closedBook.gameObject.SetActive(true);
        closedBook.anchoredPosition = endPos;
        closedBook.rotation = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            // Slide up
            closedBook.anchoredPosition = Vector2.Lerp(endPos, startPos, Mathf.SmoothStep(0, 1, t));

            // Reverse swing direction
            float angle = Mathf.Sin(t * Mathf.PI) * -swingAmplitude;
            closedBook.rotation = Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        closedBook.rotation = Quaternion.identity;
        closedBook.gameObject.SetActive(false);

        Debug.Log("[JournalAnimator] CLOSE animation complete. Journal hidden.");
        isOpen = false;
        isAnimating = false;
    }
}
