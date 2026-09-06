
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class InfantryLoadoutCustomization : InMatchClientSingleton<InfantryLoadoutCustomization>
{

    [Header("Prefabs")]
    public ClassManager classManager;
    [SerializeField] private GameObject playerPrefab;
    public GameObject buttonPrefab;
    public GameObject removeItemButtonPrefab;
    public GameObject classButtonPrefab;
    public GameObject loadoutOptionButtonPrefab;

    [Header("UI Parents")]
    [SerializeField] public GameObject customization_buttons_parent;
    [SerializeField] public GameObject weaponStatusParent;
    [SerializeField] public Transform classesParent;
    [SerializeField] public Transform loadoutOptionsParent;
    [SerializeField] public Transform weaponsGadgetsParent;
    [SerializeField] public Transform currentItemParent;

    [Header("UI Elements")]
    [SerializeField] public Button buy_weapon_button;
    [SerializeField] public Image class_selection_image;
    [SerializeField] public Sprite lockedItemImage;
    [SerializeField] public GameObject backButton;
    [SerializeField] public GameObject updateLoadoutButton;
    [SerializeField] public TextMeshProUGUI currentSelectionText;
    [SerializeField] public Slider weaponsGadgetsSlider;

    [Header("Selection Outline")]
    [SerializeField] public Color selectedOutlineColor = Color.white;
    [SerializeField] public float outlineWidth = 5f;

    [Header("Weapon Customization Buttons")]
    [SerializeField] public GameObject customizeWeaponButton;
    [FormerlySerializedAs("customizeWeaponButtonBarrel")]
    public GameObject customizeWeaponButtonNozzle;
    public GameObject customizeWeaponButtonBarrel;
    public GameObject customizeWeaponButtonSight;
    public GameObject customizeWeaponButtonCantedSight;
    public GameObject customizeWeaponButtonMag;
    public GameObject customizeWeaponButtonGrip;
    public GameObject customizeWeaponButtonSideGrip;
    public GameObject customizeWeaponButtonErgonomics;
    [Tooltip("Optional. If not assigned, the attachment reset button will be created at runtime.")]
    public GameObject resetWeaponAttachmentsButton;

    [Header("Layout Settings")]
    [SerializeField] public float classButtonSpacingX = 150f;
    [SerializeField] public float classButtonStartX = -300f;
    [SerializeField] public float classButtonY = 0f;
    [SerializeField] public float itemButtonSpacingY = -130f;
    [SerializeField] public float itemButtonStartY = 0;
    [SerializeField] public float itemButtonX = 0f;
    [SerializeField] public float loadoutOptionSpacingY = -80f;
    [SerializeField] public float loadoutOptionStartY = 100f;

    [Header("Slider Settings")]
    [SerializeField] public float sliderMinValue = 0f;
    [SerializeField] public float sliderMaxValue = 1f;
    [SerializeField] public float minScrollY = -200f;
    [SerializeField] public float maxScrollYIncreaser = 100f;

    [Header("Addressables")]
    [SerializeField] private AssetLabelReference primaryAssetLabelReference;
    [SerializeField] private AssetLabelReference secondaryAssetLabelReference;
    [SerializeField] private AssetLabelReference gadgetAssetLabelReference;

    [Header("Sound Effects")]
    [SerializeField] public SoundManager.SoundComponents purchaseItemSfx;
    [SerializeField] public SoundManager.SoundComponents purchaseDenialItemSfx;
    [SerializeField] public SoundManager.SoundComponents selectItemSfx;

    [Header("Current selection")]
    public GameObject selected_primary;
    public GameObject selected_secondary;
    public GameObject selected_gadget1;
    public GameObject selected_gadget2;

    // Statics
    public static SoundManager.SoundComponents reference_purchase_item_sfx { get; private set; }
    public static SoundManager.SoundComponents reference_purchase_denial_item_sfx { get; private set; }
    public static Sprite locked_item_image { get; private set; }
    public static Button BuyWeaponButton { get; private set; }

    public enum SelectionStage
    {
        ClassSelection,
        LoadoutOptionSelection,
        ItemSelection,
        WeaponCustomization,
        SkinSelection
    }

    private SelectionStage _currentStage = SelectionStage.ClassSelection;
    public ClassManager.Class _selectedClass;
    public GameObject _currentItemSelected;
    public GameObject _weaponBeingCustomized;

    public GameObject[] primaryWeapons { get; private set; }
    public GameObject[] secondaryWeapons { get; private set; }
    public GameObject[] gadgets { get; private set; }

    // Referências para os módulos
    public ClassSelectionManager classSelectionManager { get; private set; }
    public LoadoutOptionManager loadoutOptionManager { get; private set; }
    public ItemSelectionManager itemSelectionManager { get; private set; }
    public WeaponCustomizationManager weaponCustomizationManager { get; private set; }
    public LoadoutSaverManager loadoutSaverManager { get; private set; }
    public UIUpdateManager uIUpdateManager { get; private set; }
    // Adicione esta referência
    public SkinSelectionManager skinSelectionManager { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeManagers();
        WaitForLoadAllAddressables();
    }

    private async void WaitForLoadAllAddressables()
    {
        await LoadAllAddressables();

        // Verifica se os dados foram carregados corretamente
        if (primaryWeapons == null || primaryWeapons.Length == 0)
        {
            Debug.LogError("[Loadout] Failed to load Addressables in the build!");
            // Tenta carregar novamente ou usa fallback
            await LoadAllAddressables();
        }

        InitializeUIAfterDataLoad();
    }
    private async System.Threading.Tasks.Task LoadAllAddressables()
    {
        try
        {
            var primaryHandle = Addressables.LoadAssetsAsync<GameObject>(primaryAssetLabelReference, null);
            var secondaryHandle = Addressables.LoadAssetsAsync<GameObject>(secondaryAssetLabelReference, null);
            var gadgetsHandle = Addressables.LoadAssetsAsync<GameObject>(gadgetAssetLabelReference, null);

            primaryWeapons = (await primaryHandle.Task).ToArray();
            secondaryWeapons = (await secondaryHandle.Task).ToArray();
            gadgets = (await gadgetsHandle.Task).ToArray();

        }
        catch (System.Exception)
        {
            primaryWeapons = new GameObject[0];
            secondaryWeapons = new GameObject[0];
            gadgets = new GameObject[0];
        }
    }


    private void InitializeManagers()
    {
        classSelectionManager = gameObject.GetComponent<ClassSelectionManager>();
        loadoutOptionManager = gameObject.GetComponent<LoadoutOptionManager>();
        itemSelectionManager = gameObject.GetComponent<ItemSelectionManager>();
        weaponCustomizationManager = gameObject.GetComponent<WeaponCustomizationManager>();
        loadoutSaverManager = gameObject.GetComponent<LoadoutSaverManager>();
        uIUpdateManager = gameObject.GetComponent<UIUpdateManager>();
        skinSelectionManager = gameObject.GetComponent<SkinSelectionManager>(); // NOVO

        classSelectionManager.Initialize(this);
        loadoutOptionManager.Initialize(this);
        itemSelectionManager.Initialize(this);
        weaponCustomizationManager.Initialize(this);
        loadoutSaverManager.Initialize(this);
        uIUpdateManager.Initialize(this);
        skinSelectionManager.Initialize(this); // NOVO
    }

    private void InitializeUIAfterDataLoad()
    {
        // Agora os dados estão carregados, podemos iniciar a UI
        _selectedClass = AccountManager.Instance.selectedClass;

        classSelectionManager.InitializeUI();
        classSelectionManager.ShowClassSelection();
    }

    private void Start()
    {
        locked_item_image = lockedItemImage;
        BuyWeaponButton = buy_weapon_button;
        reference_purchase_item_sfx = purchaseItemSfx;
        reference_purchase_denial_item_sfx = purchaseDenialItemSfx;

    }

    private void Update()
    {
        uIUpdateManager.UpdateUI();
        if (SquadSelecionUI.Instance == null) return;

        if (_currentStage == SelectionStage.ClassSelection)
        {
            if (!SquadSelecionUI.Instance.gameObject.activeSelf) SquadSelecionUI.Instance.gameObject.SetActive(true);
        }
        else
        {
            if (SquadSelecionUI.Instance.gameObject.activeSelf) SquadSelecionUI.Instance.gameObject.SetActive(false);
        }
    }

    public void SaveCurrentLoadout() => loadoutSaverManager.SaveCurrentLoadout(_selectedClass);

    public void OnBackButtonClicked()
    {
        switch (_currentStage)
        {
            case SelectionStage.WeaponCustomization:
                weaponCustomizationManager.OnBackFromCustomization();
                break;
            case SelectionStage.ItemSelection:
                loadoutOptionManager.OnBackToLoadoutOptions();
                break;
            case SelectionStage.LoadoutOptionSelection:
                classSelectionManager.OnBackToClassSelection();
                break;
            case SelectionStage.SkinSelection: // NOVO
                loadoutOptionManager.OnBackToLoadoutOptions();
                break;
        }
    }
    public void UpdateWeaponStats(WeaponProperties wp)
    {
        StringBuilder allText = new StringBuilder();
        AttatchmentManager attachmentManager = wp.GetComponent<AttatchmentManager>();
        float attachmentPoints = attachmentManager != null
            ? attachmentManager.CurrentAttachmentPoints
            : Mathf.Max(0f, wp.currentAttachmentPoints);

        allText.AppendLine("Rate of Fire: " + wp.firing.rateOfFire.ToString("F0") + " RPM");
        allText.AppendLine("ADS Speed: " + wp.adsSpeed.ToString("F2") + "s");
        allText.AppendLine("Player Speed Modifier: " + wp.speedChange.ToString("F0"));
        Sight s = wp.GetComponentsInChildren<Sight>().FirstOrDefault(sight => !(sight is CantedSight));
        CantedSight cantedSight = wp.GetComponentInChildren<CantedSight>();
        string zoomText = s == null ? "--" : s.zoomChanges != null && s.zoomChanges.Length > 0 ? string.Join(" / ", s.zoomChanges) : s.zoomChange.ToString("F1");
        string cantedZoomText = cantedSight == null ? "--" : cantedSight.zoomChanges != null && cantedSight.zoomChanges.Length > 0 ? string.Join(" / ", cantedSight.zoomChanges) : cantedSight.zoomChange.ToString("F1");
        allText.AppendLine("Zoom: " + zoomText);
        allText.AppendLine("Canted Sight Zoom: " + cantedZoomText);
        allText.AppendLine("Fire Modes: " + string.Join(" / ", wp.firing.fireModes));
        allText.AppendLine("Destruction Force: " + wp.projectileValues.destructionRadius.ToString("F0"));
        allText.AppendLine("Damage: " + wp.projectileValues.infantryDamage.ToString("F1"));
        allText.AppendLine("Vehicle Base Damage: " + wp.projectileValues.vehicleDamage.ToString("F1"));
        allText.AppendLine("Headshot Multiplier: " + wp.projectileValues.headshotMultiplier.ToString("F1"));
        AnimationCurve damageCurve = wp.projectileValues.CreateRuntimeDamageCurve();
        allText.AppendLine("Spread Increaser: " + wp.spreadValues.spreadIncreaser.ToString("F2"));
        allText.AppendLine("Max Spread: " + wp.spreadValues.maxSpread.ToString("F2"));
        allText.AppendLine("Horizontal Recoil: " + wp.recoilValues.recoilPattern.Average(v => v.horizontalRecoil.value).ToString("F2"));
        allText.AppendLine("Vertical Recoil: " + wp.recoilValues.recoilPattern.Average(v => v.verticalRecoil.value).ToString("F2"));
        allText.AppendLine("First Shot Recoil Increaser: x" + wp.recoilValues.firstShootRecoilMultiplier.ToString("F1"));
        allText.AppendLine("Mag Count: " + wp.reloadValues.magCount.ToString());
        allText.AppendLine("Bullets Per Mag: " + wp.reloadValues.bulletsPerMag.ToString());
        allText.Append("Reload Speed: " + wp.reloadValues.reloadTime.ToString("F2") + "s");

        uIUpdateManager.UpdateItemStatusText(allText.ToString());
        uIUpdateManager.UpdateWeaponStatSliders(wp);
        uIUpdateManager.UpdateAttachmentPoints(attachmentPoints);
        uIUpdateManager.UpdateDamageCurveGraph(damageCurve);
    }

    public void PreviewAttachmentStats(Attatchment attachment)
    {
        if (attachment == null || _weaponBeingCustomized == null) return;

        WeaponProperties weaponProperties = _weaponBeingCustomized.GetComponent<WeaponProperties>();
        if (weaponProperties != null)
            uIUpdateManager.PreviewAttachmentStats(weaponProperties, attachment);
    }

    public void ClearAttachmentStatsPreview()
    {
        if (uIUpdateManager != null) uIUpdateManager.ClearAttachmentStatsPreview();
    }

    public GameObject GetCurrentPrimaryWeapon()
    {
        if (primaryWeapons == null) return null;

        if (selected_primary != null) return selected_primary;

        foreach (GameObject weapon in primaryWeapons)
        {
            if (weapon == null) continue;

            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (wp == null) continue;

            if (HasClassAccessToWeapon(wp) && HasFactionAccessToWeapon(wp) && wp.battleCoinsToUnlock == 0)
            {
                selected_primary = weapon;
                return weapon;
            }
        }

        return null;
    }

    public GameObject GetCurrentSecondaryWeapon()
    {
        if (secondaryWeapons == null) return null;

        if (selected_secondary != null) return selected_secondary;

        foreach (GameObject weapon in secondaryWeapons)
        {
            if (weapon == null) continue;

            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (wp == null) continue;

            if (HasClassAccessToWeapon(wp) && HasFactionAccessToWeapon(wp) && wp.battleCoinsToUnlock == 0)
            {
                selected_secondary = weapon;
                return weapon;
            }
        }

        return null;
    }

    public GameObject GetCurrentGadget1() => selected_gadget1;
    public GameObject GetCurrentGadget2() => selected_gadget2;

    private bool HasClassAccessToWeapon(WeaponProperties weaponProperties)
    {
        if (weaponProperties.classWeapon.Any(c => c == _selectedClass)) return true;

        return false;
    }

    private bool HasFactionAccessToWeapon(WeaponProperties weaponProperties)
    {
        if (AccountManager.Instance == null) return true;

        if (weaponProperties.faction.Any(c => c == AccountManager.Instance.selectedFaction)) return true;

        return false;
    }

    public void SetCurrentStage(SelectionStage stage) => _currentStage = stage;
    public SelectionStage GetCurrentStage() => _currentStage;
}
