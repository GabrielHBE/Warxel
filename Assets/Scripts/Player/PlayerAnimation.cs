using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PlayerAnimation : NetworkBehaviour
{
    public bool can_update_animation = false;
    [SerializeField] private ThirdPersonWeaponController thirdPersonWeaponController;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController playercontroller;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private GameObject weapon_hand_holder;
    private Quaternion weapon_hand_holder_original_rotation;
    private Quaternion weapon_hand_holder_aim_rotation;

    [Header("Switch Weapon")]
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private Transform save_weapon_pos;

    private GameObject current_weapon;

    [Header("Itens")]
    [SerializeField] private Transform itens_parent;

    // Mantém como GameObject, mas agora com permissão padrão (server authority)
    public readonly SyncVar<GameObject> primary = new SyncVar<GameObject>();
    public readonly SyncVar<GameObject> secondary = new SyncVar<GameObject>();
    public readonly SyncVar<GameObject> gadget1 = new SyncVar<GameObject>();
    public readonly SyncVar<GameObject> gadget2 = new SyncVar<GameObject>();

    [Header("Right Hand Transition")]
    [SerializeField] private float rightHandSwitchSpeed = 4f;
    [SerializeField] private float rightHandReturnSpeed = 8f;

    [Header("Aiming Settings")]
    [SerializeField] private TwoBoneIKConstraint rightHand_twoBoneIKConstraint;
    [SerializeField] private Transform rightHand_target;
    [SerializeField] private Transform rightHand;

    [SerializeField] private Transform leftHand;
    [SerializeField] private TwoBoneIKConstraint leftHand_twoBoneIKConstraint;
    private Transform leftHand_target;


    [SerializeField] private Transform vehicleLeftHandTarget;
    [SerializeField] private Transform vehicleRightHandTarget;

    private float left_twoBoneIKConstraint_weight;
    private float right_twoBoneIKConstraint_weight;

    // Death animation
    private int randomDeadAnimationValue = 1;
    private bool hasGeneratedDeadAnimation = false;
    private bool wasDead = false;

    private float right_hand_transition_speed;

    // Controle Local de Inicialização
    private bool is_initialized = false;
    private int currently_instantiated_weapon_index = -1;

    // --- RASTREADORES LOCAIS (Para evitar spam de rede) ---
    private bool last_aiming;
    private bool last_firing;
    private bool last_grounded;
    private bool last_prone_transition;
    private bool last_in_vehicle;
    private bool last_dead;
    private bool last_switch;
    private int last_weapon = -1;

    // --- VARIÁVEIS SINCRONIZADAS PARA OBSERVERS (IK) ---
    private readonly SyncVar<bool> sync_is_aiming = new SyncVar<bool>();
    private readonly SyncVar<bool> sync_is_firing = new SyncVar<bool>();
    private readonly SyncVar<bool> sync_is_grounded = new SyncVar<bool>();
    private readonly SyncVar<bool> sync_is_prone_transition = new SyncVar<bool>();
    private readonly SyncVar<bool> sync_is_in_vehicle = new SyncVar<bool>();
    private readonly SyncVar<bool> sync_is_dead = new SyncVar<bool>();
    private readonly SyncVar<int> sync_current_weapon = new SyncVar<int>();
    private readonly SyncVar<bool> sync_switch = new SyncVar<bool>();

    // --- PROPRIEDADES DE LEITURA ---
    private bool StateAiming => IsOwner ? playerProperties.is_aiming : sync_is_aiming.Value;
    private bool StateFiring => IsOwner ? playerProperties.is_firing : sync_is_firing.Value;
    private bool StateGrounded => IsOwner ? playerProperties.isGrounded : sync_is_grounded.Value;
    private bool StateProneTrans => IsOwner ? playerProperties.isProneTransition : sync_is_prone_transition.Value;
    private bool StateInVehicle => IsOwner ? playerProperties.is_in_vehicle : sync_is_in_vehicle.Value;
    private bool StateDead => IsOwner ? playerProperties.is_dead.Value : sync_is_dead.Value;
    private bool StateSwitch => IsOwner ? (switchWeapon != null && switchWeapon._switch) : sync_switch.Value;
    private int StateWeapon => IsOwner ? (switchWeapon != null ? switchWeapon.currentWeapon : 1) : sync_current_weapon.Value;

    private Vector3 original_rightHand_target;
    private Transform thirdPersonWeapon_left_hand_position;
    private bool thirdPersonWeapon_set_left_hand_on_aim;

    private enum WeaponSlot
    {
        Primary = 1,
        Secondary = 2,
        Gadget1 = 3,
        Gadget2 = 4
    }

    // Cache para armas instanciadas
    private Dictionary<int, GameObject> instantiatedWeapons = new Dictionary<int, GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();


        // Registra os callbacks para mudanças nos SyncVars
        sync_current_weapon.OnChange += OnWeaponSyncChanged;
        primary.OnChange += OnWeaponPrefabChanged;
        secondary.OnChange += OnWeaponPrefabChanged;
        gadget1.OnChange += OnWeaponPrefabChanged;
        gadget2.OnChange += OnWeaponPrefabChanged;

        original_rightHand_target = rightHand_target.localPosition;

        if (IsOwner)
        {
            // Para o owner, carrega as armas do loadout e envia para o servidor
            SetWeapons();
            // Força uma sincronização inicial dos estados
            Invoke(nameof(SyncInitialStates), 0.5f);
        }

        if (!IsOwner)
        {
            weapon_hand_holder_original_rotation = weapon_hand_holder.transform.localRotation * Quaternion.Euler(0, 0f, 30f);
            weapon_hand_holder_aim_rotation = weapon_hand_holder.transform.localRotation;
            randomDeadAnimationValue = Random.Range(1, 8);

            left_twoBoneIKConstraint_weight = 1;
            right_twoBoneIKConstraint_weight = 0;

            // Aguarda os SyncVars terem valores
            Invoke(nameof(InitializeForObserver), 0.2f);
        }

    }

    private void SyncInitialStates()
    {
        if (IsOwner && switchWeapon != null && playerProperties != null)
        {
            CmdSyncIKStates(
                playerProperties.is_aiming,
                playerProperties.is_firing,
                playerProperties.isGrounded,
                playerProperties.isProneTransition,
                playerProperties.is_in_vehicle,
                playerProperties.is_dead.Value,
                switchWeapon._switch,
                switchWeapon.currentWeapon
            );
        }
    }

    private void InitializeForObserver()
    {
        if (is_initialized) return;

        int initialWeapon = sync_current_weapon.Value == 0 ? 1 : sync_current_weapon.Value;
        SwitchWeapon(initialWeapon);
        is_initialized = true;
    }

    private void SetWeapons()
    {
        // Carrega as armas do loadout
        GameObject primaryWeapon = PlayerLoadoutCustomization.Instance?.GetCurrentPrimaryWeapon();
        if (primaryWeapon != null)
        {
            WeaponProperties primaryWeaponProps = primaryWeapon.GetComponent<WeaponProperties>();
            if (primaryWeaponProps != null && primaryWeaponProps.third_person_prefab != null)
            {
                CmdSetPrimaryWeapon(primaryWeaponProps.third_person_prefab);
            }
        }

        GameObject secondaryWeapon = PlayerLoadoutCustomization.Instance?.GetCurrentSecondaryWeapon();
        if (secondaryWeapon != null)
        {
            WeaponProperties secondaryWeaponProps = secondaryWeapon.GetComponent<WeaponProperties>();
            if (secondaryWeaponProps != null && secondaryWeaponProps.third_person_prefab != null)
            {
                CmdSetSecondaryWeapon(secondaryWeaponProps.third_person_prefab);
            }
        }

        GameObject gadget1Weapon = PlayerLoadoutCustomization.Instance?.GetCurrentGadget1();
        if (gadget1Weapon != null)
        {
            Gadget gadget1Component = gadget1Weapon.GetComponent<Gadget>();
            if (gadget1Component != null && gadget1Component.third_person_prefab != null)
            {
                CmdSetGadget1(gadget1Component.third_person_prefab);
            }
        }

        GameObject gadget2Weapon = PlayerLoadoutCustomization.Instance?.GetCurrentGadget2();
        if (gadget2Weapon != null)
        {
            Gadget gadget2Component = gadget2Weapon.GetComponent<Gadget>();
            if (gadget2Component != null && gadget2Component.third_person_prefab != null)
            {
                CmdSetGadget2(gadget2Component.third_person_prefab);
            }
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdSetPrimaryWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab != null)
        {
            primary.Value = weaponPrefab;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdSetSecondaryWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab != null)
        {
            secondary.Value = weaponPrefab;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdSetGadget1(GameObject gadgetPrefab)
    {
        if (gadgetPrefab != null)
        {
            gadget1.Value = gadgetPrefab;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdSetGadget2(GameObject gadgetPrefab)
    {
        if (gadgetPrefab != null)
        {
            gadget2.Value = gadgetPrefab;
        }
    }

    private void OnWeaponPrefabChanged(GameObject prev, GameObject next, bool asServer)
    {
        // Quando um prefab de arma muda, limpa o cache para essa arma ser recriada
        if (!IsOwner)
        {
            int weaponIndex = -1;
            if (ReferenceEquals(primary.Value, next)) weaponIndex = 1;
            else if (ReferenceEquals(secondary.Value, next)) weaponIndex = 2;
            else if (ReferenceEquals(gadget1.Value, next)) weaponIndex = 3;
            else if (ReferenceEquals(gadget2.Value, next)) weaponIndex = 4;

            if (weaponIndex != -1 && instantiatedWeapons.ContainsKey(weaponIndex))
            {
                if (instantiatedWeapons[weaponIndex] != null)
                    Destroy(instantiatedWeapons[weaponIndex]);
                instantiatedWeapons.Remove(weaponIndex);
            }

            // Se for a arma atual, recria
            if (sync_current_weapon.Value == weaponIndex)
            {
                SwitchWeapon(weaponIndex);
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        sync_current_weapon.OnChange -= OnWeaponSyncChanged;
        primary.OnChange -= OnWeaponPrefabChanged;
        secondary.OnChange -= OnWeaponPrefabChanged;
        gadget1.OnChange -= OnWeaponPrefabChanged;
        gadget2.OnChange -= OnWeaponPrefabChanged;

        // Limpa armas instanciadas
        foreach (var weapon in instantiatedWeapons.Values)
        {
            if (weapon != null) Destroy(weapon);
        }
        instantiatedWeapons.Clear();
    }

    public void Initialize()
    {
        if (is_initialized) return;

        if (thirdPersonWeaponController != null)
            thirdPersonWeaponController.HideWeapon();

        weapon_hand_holder_original_rotation = weapon_hand_holder.transform.localRotation * Quaternion.Euler(0, 0f, 30f);
        weapon_hand_holder_aim_rotation = weapon_hand_holder.transform.localRotation;

        if (switchWeapon != null)
            SwitchWeapon(switchWeapon.currentWeapon);

        randomDeadAnimationValue = Random.Range(1, 8);

        left_twoBoneIKConstraint_weight = 1;
        right_twoBoneIKConstraint_weight = 0;

        is_initialized = true;
    }

    private void OnWeaponSyncChanged(int prev, int next, bool asServer)
    {
        if (!IsOwner)
        {
            SwitchWeapon(next);
        }
    }

    void LateUpdate()
    {
        if (IsOwner)
        {
            CheckAndSyncStates();
        }
        UpdateLeftHandTarget();
        UpdateAnimatorParameters();
        UpdateDeathAnimation();

        // MUDE A ORDEM: primeiro executa o IK
        UpdateLeftHandIK();
        UpdateRightHandIK();      // ← Agora executa antes

        // Depois executa a transformação da mão direita
        UpdateRightHandTransform(); // ← Agora executa depois

        // O código comentado sobre a arma...
        // Lógica do veículo
        
        if (StateInVehicle)
        {
            if (current_weapon != null && current_weapon.activeSelf)
                current_weapon.SetActive(false);
        }
        else
        {
            if (current_weapon != null && !current_weapon.activeSelf)
                current_weapon.SetActive(true);
        }
        
    }

    #region Sincronizacao de Rede

    private void CheckAndSyncStates()
    {
        if (switchWeapon == null || playerProperties == null) return;

        bool current_switch = switchWeapon._switch;
        int current_weapon_idx = switchWeapon.currentWeapon;

        if (last_aiming != playerProperties.is_aiming ||
            last_firing != playerProperties.is_firing ||
            last_grounded != playerProperties.isGrounded ||
            last_prone_transition != playerProperties.isProneTransition ||
            last_in_vehicle != playerProperties.is_in_vehicle ||
            last_dead != playerProperties.is_dead.Value ||
            last_switch != current_switch ||
            last_weapon != current_weapon_idx)
        {
            last_aiming = playerProperties.is_aiming;
            last_firing = playerProperties.is_firing;
            last_grounded = playerProperties.isGrounded;
            last_prone_transition = playerProperties.isProneTransition;
            last_in_vehicle = playerProperties.is_in_vehicle;
            last_dead = playerProperties.is_dead.Value;
            last_switch = current_switch;
            last_weapon = current_weapon_idx;

            CmdSyncIKStates(last_aiming, last_firing, last_grounded, last_prone_transition, last_in_vehicle, last_dead, last_switch, last_weapon);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdSyncIKStates(bool aiming, bool firing, bool grounded, bool proneTrans, bool inVehicle, bool dead, bool isSwitch, int weaponIdx)
    {
        sync_is_aiming.Value = aiming;
        sync_is_firing.Value = firing;
        sync_is_grounded.Value = grounded;
        sync_is_prone_transition.Value = proneTrans;
        sync_is_in_vehicle.Value = inVehicle;
        sync_is_dead.Value = dead;
        sync_switch.Value = isSwitch;
        sync_current_weapon.Value = weaponIdx;
    }

    #endregion

    #region Animator

    private void UpdateAnimatorParameters()
    {
        if (!can_update_animation || !IsOwner) return;
        if (anim == null || playercontroller == null || playerProperties == null) return;

        anim.SetFloat("Horizontal", playercontroller.moveHorizontal, 0.1f, Time.deltaTime);
        anim.SetFloat("Vertical", playercontroller.moveForward, 0.1f, Time.deltaTime);

        anim.SetBool("Crouched", playerProperties.crouched);
        anim.SetBool("Proned", playerProperties.is_proned);
        anim.SetBool("Walking",
            !playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));

        anim.SetBool("Sprinting",
            !playerProperties.crouched &&
            playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));

        anim.SetBool("Reloading", playerProperties.is_reloading);
        anim.SetBool("ProneTransition", playerProperties.isProneTransition);
        anim.SetBool("Roll", playerProperties.roll);
        anim.SetBool("Dead", playerProperties.is_dead.Value);
        anim.SetFloat("Rotation", Input.GetAxis("Mouse X"));
        anim.SetBool("HasLeftHandHolder", leftHand_target != null);
        anim.SetBool("IsGrounded", playerProperties.isGrounded);
        anim.SetBool("InVehicle", playerProperties.is_in_vehicle);
        anim.SetBool("Aiming", playerProperties.is_aiming);
    }

    #endregion

    #region Death Animation

    private void UpdateDeathAnimation()
    {
        if (StateDead && !wasDead)
        {
            if (!hasGeneratedDeadAnimation)
            {
                randomDeadAnimationValue = Random.Range(1, 8);
                hasGeneratedDeadAnimation = true;
            }
        }
        else if (!StateDead && wasDead)
        {
            hasGeneratedDeadAnimation = false;
        }

        wasDead = StateDead;

        if (can_update_animation && IsOwner && anim != null)
            anim.SetInteger("RandomDeadAnimation", randomDeadAnimationValue);
    }

    public void ResetDeathAnimation()
    {
        hasGeneratedDeadAnimation = false;
        wasDead = false;
        randomDeadAnimationValue = Random.Range(1, 8);
    }

    #endregion

    #region Right Hand Transform (SMOOTH TRANSITION)

    private void UpdateRightHandTransform()
    {
        if (weapon_hand_holder == null) return;

        // LOG 2: Verificar se está ignorando quando no veículo
        if (StateInVehicle)
        {
            return;
        }

        // Resto do código permanece igual...
        if (StateAiming || StateFiring || leftHand_target == null)
        {
            weapon_hand_holder.transform.localRotation = Quaternion.Slerp(
                weapon_hand_holder.transform.localRotation,
                weapon_hand_holder_aim_rotation,
                Time.deltaTime * 6
            );
        }
        else
        {
            weapon_hand_holder.transform.localRotation = Quaternion.Slerp(
                weapon_hand_holder.transform.localRotation,
                weapon_hand_holder_original_rotation,
                Time.deltaTime * 6
            );
        }

        Transform target = StateSwitch ? save_weapon_pos : rightHand_target;
        float speed = StateSwitch ? rightHandSwitchSpeed : rightHandReturnSpeed;

        if (target != null && rightHand != null)
        {
            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;

            if (!StateGrounded)
            {
                targetPosition += Vector3.down * 0.6f;
                targetRotation *= Quaternion.Euler(0, 0f, -20f);
            }

            rightHand.position = Vector3.Lerp(
                rightHand.position,
                targetPosition,
                Time.deltaTime * speed
            );

            rightHand.rotation = Quaternion.Slerp(
                rightHand.rotation,
                targetRotation,
                Time.deltaTime * speed
            );
        }
    }

    #endregion

    #region Left Hand IK

    private void UpdateLeftHandIK()
    {
        // Lógica para Veículo
        if (StateInVehicle)
        {
            if (vehicleLeftHandTarget != null && leftHand != null)
            {
                left_twoBoneIKConstraint_weight = 1f;
                leftHand.position = vehicleLeftHandTarget.position;
                leftHand.rotation = vehicleLeftHandTarget.rotation;
            }
            else
            {
                // Se estiver no veículo mas sem alvo definido, desativa o IK
                left_twoBoneIKConstraint_weight = 0f;
            }

            if (leftHand_twoBoneIKConstraint != null)
                leftHand_twoBoneIKConstraint.weight = left_twoBoneIKConstraint_weight;

            return; // Sai do método para não executar a lógica de arma
        }

        // Lógica normal de Arma (fora do veículo)
        if (leftHand_target != null && leftHand != null)
        {
            leftHand.position = leftHand_target.position;
            leftHand.rotation = leftHand_target.rotation;
        }

        bool should_hold_weapon = (leftHand_target != null) && !StateProneTrans && !StateDead;

        if (should_hold_weapon)
            left_twoBoneIKConstraint_weight += Time.deltaTime * 7f;
        else
            left_twoBoneIKConstraint_weight -= Time.deltaTime * 7f;

        left_twoBoneIKConstraint_weight = Mathf.Clamp01(left_twoBoneIKConstraint_weight);

        if (leftHand_twoBoneIKConstraint != null)
            leftHand_twoBoneIKConstraint.weight = left_twoBoneIKConstraint_weight;
    }

    #endregion

    #region Right Hand IK

    private void UpdateRightHandIK()
    {
        // Lógica para Veículo
        if (StateInVehicle)
        {
            if (vehicleRightHandTarget != null && rightHand != null)
            {
                right_twoBoneIKConstraint_weight = 1f;
                rightHand.position = vehicleRightHandTarget.position;
                rightHand.rotation = vehicleRightHandTarget.rotation;
            }
            else
            {
                // Se estiver no veículo mas sem alvo definido, desativa o IK
                right_twoBoneIKConstraint_weight = 0f;
            }

            if (rightHand_twoBoneIKConstraint != null)
                rightHand_twoBoneIKConstraint.weight = right_twoBoneIKConstraint_weight;

            return; // Sai do método para não executar a lógica de arma/switch
        }

        // Lógica normal de Arma/Switch (fora do veículo)
        if (StateSwitch)
        {
            SwitchWeapon(StateWeapon);
            right_hand_transition_speed = 4f;
        }
        else
        {
            right_hand_transition_speed = 7f;
        }

        if ((StateAiming || StateFiring || StateSwitch) && leftHand_target != null)
        {
            right_twoBoneIKConstraint_weight += Time.deltaTime * right_hand_transition_speed;
        }
        else
        {
            right_twoBoneIKConstraint_weight -= Time.deltaTime * right_hand_transition_speed / 2f;
        }

        right_twoBoneIKConstraint_weight = Mathf.Clamp01(right_twoBoneIKConstraint_weight);

        if (rightHand_twoBoneIKConstraint != null)
            rightHand_twoBoneIKConstraint.weight = right_twoBoneIKConstraint_weight;
    }

    private void UpdateLeftHandTarget()
    {
        // Só fazemos algo se a arma tiver um local para a mão esquerda
        if (thirdPersonWeapon_left_hand_position != null)
        {
            // Se a arma exige mirar para a mão esquerda segurar nela...
            if (thirdPersonWeapon_set_left_hand_on_aim)
            {
                // Usamos StateAiming para que a mão obedeça tanto no seu PC quanto na tela dos inimigos!
                leftHand_target = StateAiming ? thirdPersonWeapon_left_hand_position : null;
            }
            else
            {
                // Se a arma não exige mirar, a mão esquerda fica sempre grudada nela
                leftHand_target = thirdPersonWeapon_left_hand_position;
            }
        }
        else
        {
            leftHand_target = null;
        }
    }

    #endregion

    #region Switch_weapon

    private GameObject GetWeaponPrefab(int index)
    {
        switch (index)
        {
            case 1: return primary.Value;
            case 2: return secondary.Value;
            case 3: return gadget1.Value;
            case 4: return gadget2.Value;
            default: return null;
        }
    }

    private void SwitchWeapon(int index)
    {
        if (index == currently_instantiated_weapon_index && current_weapon != null) return;

        // Salva a arma atual no cache
        if (current_weapon != null && currently_instantiated_weapon_index != -1)
        {
            if (instantiatedWeapons.ContainsKey(currently_instantiated_weapon_index))
            {
                instantiatedWeapons[currently_instantiated_weapon_index] = current_weapon;
            }
            //current_weapon.SetActive(false);
        }

        // Tenta recuperar do cache
        if (instantiatedWeapons.TryGetValue(index, out GameObject cachedWeapon) && cachedWeapon != null)
        {
            current_weapon = cachedWeapon;
            //current_weapon.SetActive(true);
            currently_instantiated_weapon_index = index;
        }
        else
        {
            // Cria nova arma
            GameObject weaponPrefab = GetWeaponPrefab(index);
            if (weaponPrefab == null) return;

            // Instancia a nova arma
            current_weapon = Instantiate(weaponPrefab, itens_parent);
            //current_weapon.transform.localPosition = Vector3.zero;
            //current_weapon.transform.localRotation = Quaternion.identity;
            currently_instantiated_weapon_index = index;

            // Armazena no cache
            instantiatedWeapons[index] = current_weapon;
        }

        if (current_weapon == null) return;

        current_weapon.SetActive(true);


        // Busca o LeftHandPosition
        ThirdPersonWeapon thirdPersonWeapon = current_weapon.GetComponent<ThirdPersonWeapon>();
        if (thirdPersonWeapon != null)
        {
            rightHand_target.localPosition = original_rightHand_target + thirdPersonWeapon.aim_position_offset;
            thirdPersonWeapon_left_hand_position = thirdPersonWeapon.left_hand_position;
            thirdPersonWeapon_set_left_hand_on_aim = thirdPersonWeapon.set_left_hand_on_aim;

            // APAGUE ESTA LINHA: leftHand_target = thirdPersonWeapon_left_hand_position;
        }
        else
        {
            leftHand_target = null;
            rightHand_target.localPosition = original_rightHand_target;
        }

        /*
        leftHand_target = null;
        foreach (Transform t in current_weapon.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag("LeftHandPosition"))
            {
                leftHand_target = t;
                break;
            }
        }
        */

        /*
        // Configura o ThirdPersonWeapon
        ThirdPersonWeaponController tpWeapon = current_weapon.GetComponent<ThirdPersonWeaponController>();
        if (tpWeapon == null)
        {
            tpWeapon = current_weapon.AddComponent<ThirdPersonWeaponController>();
        }

        tpWeapon.Reestart(IsOwner);
        */
        thirdPersonWeaponController.Reestart(IsOwner);


    }

    #endregion

    #region Public Methods
    [ServerRpc]
    public void DeactivateCurrentWeapon()
    {
        if (current_weapon != null)
        {
            CmdUpdateWeaponActive(false);
            current_weapon.SetActive(false);
        }
    }

    [ServerRpc]
    public void ActivateCurrentWeapon()
    {
        if (current_weapon != null)
        {
            CmdUpdateWeaponActive(true);
            current_weapon.SetActive(true);
        }
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void CmdUpdateWeaponActive(bool status)
    {
        current_weapon.SetActive(status);
    }

    public void SetVehicleIKTargets(Transform vehicleLeftHandTarget, Transform vehicleRightHandTarget)
    {
        this.vehicleLeftHandTarget = vehicleLeftHandTarget;
        this.vehicleRightHandTarget = vehicleRightHandTarget;
    }

    #endregion
}