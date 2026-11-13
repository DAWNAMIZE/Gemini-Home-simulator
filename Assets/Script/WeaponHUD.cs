using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-right weapon HUD. Shows the equipped weapon, pistol ammo and a Shoot
/// button. Built at runtime so the scene only needs the inventory slots.
/// </summary>
public class WeaponHUD : MonoBehaviour
{
    public static WeaponHUD instance;

    private GameObject panel;
    private TMP_Text nameLabel;
    private TMP_Text ammoLabel;
    private GameObject shootButton;
    private WeaponInstance lastWeapon;
    private bool built;

    public static WeaponHUD GetOrCreate()
    {
        if (instance != null) return instance;

        WeaponHUD existing = Object.FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("WeaponHUD");
        instance = go.AddComponent<WeaponHUD>();
        return instance;
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        Build();
        HideLegacyShootButton();
    }

    void Update()
    {
        if (!built) Build();

        WeaponInstance weapon = null;
        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
            weapon = PlayerInventory.instance.currentlyHeldModel.GetComponent<WeaponInstance>();

        if (weapon == null)
        {
            if (panel != null) panel.SetActive(false);
            lastWeapon = null;
            return;
        }

        if (panel != null && !panel.activeSelf) panel.SetActive(true);

        if (weapon != lastWeapon)
        {
            lastWeapon = weapon;
            if (nameLabel != null) nameLabel.text = weapon.weaponName.ToUpperInvariant();
        }

        if (ammoLabel != null)
        {
            ammoLabel.gameObject.SetActive(weapon.isPistol);
            if (weapon.isPistol)
                ammoLabel.text = "AMMO  " + WeaponInstance.PistolAmmo + " / " + WeaponInstance.PistolMaxAmmo;
        }

        if (shootButton != null && shootButton.activeSelf != weapon.isPistol)
            shootButton.SetActive(weapon.isPistol);
    }

    public void Refresh()
    {
        if (!built) Build();
        lastWeapon = null;
    }

    private void Build()
    {
        if (built) return;
        built = true;

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("WeaponCanvas", 940);
        canvas.transform.SetParent(transform, false);

        Image background = RuntimeUI.CreateImage(canvas.transform, "WeaponPanel",
            new Color(0.03f, 0.04f, 0.06f, 0.88f));
        panel = background.gameObject;

        RectTransform panelRt = background.rectTransform;
        panelRt.anchorMin = new Vector2(1f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(1f, 0f);
        panelRt.anchoredPosition = new Vector2(-26f, 26f);
        panelRt.sizeDelta = new Vector2(360f, 196f);

        nameLabel = RuntimeUI.CreateText(panel.transform, "WeaponName", "WEAPON",
            30f, Color.white, TextAlignmentOptions.Center);
        RectTransform nameRt = nameLabel.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -12f);
        nameRt.sizeDelta = new Vector2(-24f, 46f);

        ammoLabel = RuntimeUI.CreateText(panel.transform, "Ammo", "AMMO  15 / 15",
            26f, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.Center);
        RectTransform ammoRt = ammoLabel.rectTransform;
        ammoRt.anchorMin = new Vector2(0f, 1f);
        ammoRt.anchorMax = new Vector2(1f, 1f);
        ammoRt.pivot = new Vector2(0.5f, 1f);
        ammoRt.anchoredPosition = new Vector2(0f, -58f);
        ammoRt.sizeDelta = new Vector2(-24f, 38f);

        Button shoot = RuntimeUI.CreateButton(panel.transform, "ShootButton", "SHOOT",
            new Vector2(300f, 70f), new Vector2(0f, 18f), new Vector2(0.5f, 0f),
            new Color(0.70f, 0.18f, 0.15f, 1f), new UnityAction(OnShootClicked));
        shootButton = shoot.gameObject;

        panel.SetActive(false);
    }

    private void OnShootClicked()
    {
        if (PlayerInventory.instance != null)
            PlayerInventory.instance.FireEquippedWeapon();
    }

    private static void HideLegacyShootButton()
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == "Shoot")
                    child.gameObject.SetActive(false);
            }
        }
    }
}
