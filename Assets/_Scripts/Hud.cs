using UnityEngine;
using UnityEngine.UI;
using TMPro;

// On-screen interface: oxygen bar, ship compass with speed, combo counter, hints,
// short notices and the big end-of-level message. The HUD object lives in DraftScene;
// its parts are assigned in the Inspector (made by Filip > Rebuild Prefabs And Scene).
public class Hud : MonoBehaviour
{
    static readonly Color OxygenColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color GoodColor = new Color(0.4f, 1f, 0.5f);
    static readonly Color BadColor = new Color(1f, 0.35f, 0.3f);

    [Header("Oxygen")]
    [SerializeField] RectTransform oxygenFill;
    [SerializeField] Image oxygenFillImage;

    [Header("Messages")]
    [SerializeField] Text message;
    [SerializeField] Text messageSub;
    [SerializeField] GameObject signalOverlay;
    [SerializeField] GameObject signalText;
    [SerializeField] Text hintText;
    [SerializeField] Text toastText;
    [SerializeField] Text titleText;

    [Header("Compass")]
    [SerializeField] RectTransform compassNeedle;
    [SerializeField] Text compassText;
    [SerializeField] TMP_Text speedText; // TextMeshPro - Text (UI) on the HUD canvas
    [SerializeField] Image speedIcon;

    [Header("Combo")]
    [SerializeField] GameObject comboRoot;
    [SerializeField] Text comboText;
    [SerializeField] RectTransform comboBar;

    float oxygenFlash;
    int shownDistance = -1;
    int shownSpeed = -1;
    bool shownSpeedOk;
    int shownCombo = -1;
    string shownHint;
    float toastTimer;
    float titleTimer;

    void Awake()
    {
        HideMessage();
        SetSignalLost(false);
        SetHint(null);
        toastText.gameObject.SetActive(false);
        titleText.gameObject.SetActive(false);
        comboRoot.SetActive(false);
    }

    void Update()
    {
        if (oxygenFlash > 0f)
        {
            oxygenFlash = Mathf.Max(0f, oxygenFlash - Time.deltaTime * 4f);
            oxygenFillImage.color = Color.Lerp(OxygenColor, Color.white, oxygenFlash);
        }
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f) toastText.gameObject.SetActive(false);
        }
        if (titleTimer > 0f)
        {
            titleTimer -= Time.deltaTime;
            if (titleTimer <= 0f) titleText.gameObject.SetActive(false);
        }
    }

    public void SetOxygen(float fraction)
    {
        oxygenFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    // The oxygen bar flashes white when a crystal is collected.
    public void FlashOxygen()
    {
        oxygenFlash = 1f;
    }

    // toShip: vector from the player to the ship, in world units.
    public void SetCompass(Vector2 toShip)
    {
        float angle = Mathf.Atan2(toShip.y, toShip.x) * Mathf.Rad2Deg - 90f;
        compassNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);

        int distance = Mathf.RoundToInt(toShip.magnitude);
        if (distance != shownDistance)
        {
            shownDistance = distance;
            compassText.text = "SHIP\n" + distance + " m";
        }
    }

    // slowEnough: the player may dock at this speed.
    public void SetSpeed(float speed, bool slowEnough)
    {
        int rounded = Mathf.RoundToInt(speed);
        if (rounded == shownSpeed && slowEnough == shownSpeedOk) return;
        shownSpeed = rounded;
        shownSpeedOk = slowEnough;
        speedText.text = "SPEED " + rounded;
        speedText.color = slowEnough ? GoodColor : BadColor;
        speedIcon.color = slowEnough ? GoodColor : BadColor;   
    }

    // combo: kills in a row (shown from x2). timeLeft: 1 = just continued, 0 = about to end.
    public void SetCombo(int combo, float timeLeft)
    {
        bool show = combo >= 2;
        if (comboRoot.activeSelf != show) comboRoot.SetActive(show);
        if (!show) { shownCombo = -1; return; }

        if (combo != shownCombo)
        {
            shownCombo = combo;
            comboText.text = "COMBO x" + combo;
        }
        comboBar.anchorMin = new Vector2(1f - Mathf.Clamp01(timeLeft), 0f); // shrinks toward the right edge
    }

    // text == null hides the hint.
    public void SetHint(string text)
    {
        if (text == shownHint && hintText.gameObject.activeSelf == (text != null)) return;
        shownHint = text;
        hintText.gameObject.SetActive(text != null);
        if (text != null) hintText.text = text;
    }

    // Short notice that disappears by itself.
    public void Toast(string text)
    {
        toastText.text = text;
        toastText.gameObject.SetActive(true);
        toastTimer = 1.2f;
    }

    // Level name at the start of a level.
    public void ShowTitle(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        titleText.gameObject.SetActive(show);
        if (!show) { titleTimer = 0f; return; }
        titleText.text = text;
        titleTimer = 4f;
    }

    public void SetSignalLost(bool on)
    {
        if (signalOverlay.activeSelf == on) return;
        signalOverlay.SetActive(on);
        signalText.SetActive(on);
    }

    public void ShowMessage(string text, Color color, string sub)
    {
        message.text = text;
        message.color = color;
        messageSub.text = sub;
        message.gameObject.SetActive(true);
        messageSub.gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        message.gameObject.SetActive(false);
        messageSub.gameObject.SetActive(false);
    }
}
