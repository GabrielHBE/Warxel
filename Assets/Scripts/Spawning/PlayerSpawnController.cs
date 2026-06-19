using FishNet.Object;
using UnityEngine;
using TMPro;
using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Connection;

public class PlayerSpawnController : NetworkBehaviour
{
    public static PlayerSpawnController Instance { get; private set; }

    [SerializeField] private UI_SpawnMenuController infantaryVehicleSwitch;
    [SerializeField] private float reespawn_delay;
    [SerializeField] private TextMeshProUGUI reespawn_delay_text;
    [SerializeField] private GameObject player_prefab;
    [SerializeField] private float dragSpeed = 50f;
    [SerializeField] private Vector2 boundsX = new Vector2(-5000f, 5000f);
    [SerializeField] private Vector2 boundsZ = new Vector2(-5000f, 5000f);
    [SerializeField] private GameObject canvas;

    [Header("Camera Settings")]
    public Camera spawn_camera;
    [SerializeField] private float zoomSpeed = 50f;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [HideInInspector] public GameObject player_instantiated;
    [HideInInspector] public GameObject vehicle_instantiated;
    private bool is_respawning = false;
    private float original_spawn_delay;
    private Vector3 dragOrigin;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private Transform map_spawn_camera_pos;
    private GameObject[] infantary_spawn_flags;
    private GameObject[] vehicle_spawn_flags;

    private enum CurrentSpawnType
    {
        Infantary,
        Vehicle,
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        map_spawn_camera_pos = GameObject.FindWithTag("SpawnCameraPos").transform;
        transform.position = map_spawn_camera_pos.position;
        transform.rotation = map_spawn_camera_pos.rotation;

        if (IsOwner)
        {
            infantary_spawn_flags = GameObject.FindGameObjectsWithTag("InfantarySpawnFlags");
            vehicle_spawn_flags = GameObject.FindGameObjectsWithTag("VehicleSpawnFlags");

            ToggleFlagsVisibility(vehicle_spawn_flags, false);

            spawn_camera.enabled = true;
            InitializeForOwner();
        }
        else
        {
            InitializeForNonOwner();
        }
    }



    private void InitializeForOwner()
    {
        Instance = this;
        original_spawn_delay = reespawn_delay;
        reespawn_delay = 0;
        // Guarda posição inicial da câmera
        if (spawn_camera != null)
        {
            initialCameraPosition = spawn_camera.transform.position;
            initialCameraRotation = spawn_camera.transform.rotation;
        }
    }

    private void InitializeForNonOwner()
    {
        gameObject.SetActive(false);
    }


    private void Update()
    {
        // Só executa se for owner e estiver inicializado
        if (!IsOwner) return;

        if (player_instantiated == null)
        {
            if (reespawn_delay > 0)
            {
                reespawn_delay -= Time.deltaTime;
                if (PlayerLoadoutCustomization.Instance != null &&
                    PlayerLoadoutCustomization.Instance._currentStage == PlayerLoadoutCustomization.SelectionStage.ClassSelection)
                {
                    reespawn_delay_text.text = "Spawn delay: " + reespawn_delay.ToString("F1");
                }
                else if (reespawn_delay_text != null)
                {
                    reespawn_delay_text.text = "";
                }
            }
            else if (reespawn_delay_text != null)
            {
                reespawn_delay_text.text = "";
            }

            // Só processa drag se a câmera estiver ativa
            if (spawn_camera != null && spawn_camera.enabled)
            {
                HandleCameraDrag();
            }
        }
        else
        {
            if (reespawn_delay_text != null)
            {
                reespawn_delay_text.text = "";
            }
            reespawn_delay = original_spawn_delay;
        }
    }

    private void HandleCameraDrag()
    {

        if (InputManager.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
        }

        if (InputManager.GetMouseButton(0))
        {
            Vector3 pos = spawn_camera.ScreenToViewportPoint(Input.mousePosition - dragOrigin);

            Vector3 cameraForward = spawn_camera.transform.forward;
            Vector3 cameraRight = spawn_camera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 move = (cameraRight * pos.x + cameraForward * pos.y) * dragSpeed;

            Vector3 newPosition = spawn_camera.transform.position - move;

            newPosition.x = Mathf.Clamp(newPosition.x, boundsX.x, boundsX.y);
            newPosition.z = Mathf.Clamp(newPosition.z, boundsZ.x, boundsZ.y);

            spawn_camera.transform.position = newPosition;

            dragOrigin = Input.mousePosition;
        }
    }

    public void InitializeSpawnVehicle(Vehicle vehicle, Transform spawn_point)
    {
        // Só permite spawn se for owner
        if (!IsOwner || is_respawning) return;

        if (reespawn_delay > 0)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Spawn delay: " + reespawn_delay.ToString("F1") + "s", 1);
            return;
        }

        DisableItens();

        is_respawning = true;
        StartCoroutine(SpawnSequence(spawn_point, CurrentSpawnType.Vehicle, vehicle));
    }

    public void InitializeSpawnPlayer(Transform spawn_point)
    {
        // Só permite spawn se for owner
        if (!IsOwner || is_respawning) return;

        if (reespawn_delay > 0)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Spawn delay: " + reespawn_delay.ToString("F1") + "s", 1);
            return;
        }

        if (PlayerLoadoutCustomization.Instance.selected_primary == null)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Select a primary gun to deploy");
            return;
        }

        DisableItens();

        is_respawning = true;
        StartCoroutine(SpawnSequence(spawn_point, CurrentSpawnType.Infantary));
    }

    // Criamos essa Coroutine Mestre para gerenciar a ordem dos acontecimentos
    private IEnumerator SpawnSequence(Transform spawn_point, CurrentSpawnType currentSpawnType, Vehicle vehicle = null)
    {
        yield return StartCoroutine(TransitionCameraToSpawnPoint(spawn_point));

        if (currentSpawnType == CurrentSpawnType.Infantary)
        {
            SpawnPlayer(spawn_point.position, spawn_point.rotation);
        }
        else
        {
            // Chama o novo método unificado
            SpawnPlayerAndVehicle(spawn_point.position, spawn_point.rotation, vehicle);
        }
    }

    private IEnumerator TransitionCameraToSpawnPoint(Transform spawn_point)
    {
        Vector3 startPosition = spawn_camera.transform.position;
        Vector3 targetPosition = new Vector3(spawn_point.position.x, spawn_point.position.y + 5, spawn_point.position.z); // Ajuste a posição da câmera para ficar acima e atrás do ponto de spawn

        Quaternion startRotation = spawn_camera.transform.rotation;

        Quaternion targetRotation = spawn_point.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            spawn_camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            spawn_camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        spawn_camera.transform.position = targetPosition;
        spawn_camera.transform.rotation = targetRotation;

    }

    [ServerRpc]
    private void SpawnPlayerAndVehicle(Vector3 spawnPosition, Quaternion spawnRotation, Vehicle vehiclePrefab)
    {
        // 1. Instancia e Spawna o Player na rede
        player_instantiated = Instantiate(player_prefab, spawnPosition, spawnRotation);
        NetworkObject playerNetObj = player_instantiated.GetComponent<NetworkObject>();
        Spawn(playerNetObj, Owner);

        // 2. Instancia e Spawna o Veículo na rede
        GameObject spawnedVehicle = Instantiate(vehiclePrefab.gameObject, spawnPosition, spawnRotation);
        Vehicle vScript = spawnedVehicle.GetComponent<Vehicle>();
        Spawn(spawnedVehicle);

        // 3. Força a entrada do player (roda direto no servidor, evitando delays)
        // O método EnterVehicle da classe Vehicle já lida com RPCs de atualizar assentos e transferir Ownership
        vScript.EnterVehicle(Owner, player_instantiated);

        // 4. Atualiza o Cliente original de que o spawn terminou
        TargetOnSpawnPlayerComplete(Owner, player_instantiated);
    }

    [ServerRpc]
    private void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        player_instantiated = Instantiate(player_prefab, spawnPosition, spawnRotation);
        NetworkObject spawnedNetworkObject = player_instantiated.GetComponent<NetworkObject>();

        // Spawna o objeto para o owner específico
        Spawn(spawnedNetworkObject, Owner);

        // NOTIFICA APENAS O OWNER usando TargetRpc
        TargetOnSpawnPlayerComplete(Owner, spawnedNetworkObject.gameObject);

    }

    [TargetRpc]
    private void TargetOnSpawnPlayerComplete(NetworkConnection conn, GameObject spawnedPlayer)
    {

        player_instantiated = spawnedPlayer;
        is_respawning = false;

        PlayerController playerController = player_instantiated.GetComponent<PlayerController>();

        // Configurações locais do cliente
        SwitchWeapon switchWeapon = player_instantiated.GetComponentInChildren<SwitchWeapon>(true);
        if (switchWeapon != null && PlayerLoadoutCustomization.Instance != null)
        {
            switchWeapon.primary = PlayerLoadoutCustomization.Instance.GetCurrentPrimaryWeapon();
            switchWeapon.secondary = PlayerLoadoutCustomization.Instance.GetCurrentSecondaryWeapon();
            switchWeapon.gadget1 = PlayerLoadoutCustomization.Instance.GetCurrentGadget1();
            switchWeapon.gadget2 = PlayerLoadoutCustomization.Instance.GetCurrentGadget2();
            switchWeapon.Initialize();
        }

        if (playerController != null)
        {
            playerController.GetComponent<PlayerProperties>().player_name.Value = AccountManager.Instance.account_name;
            WeaponProperties[] weaponProperties = player_instantiated.GetComponentsInChildren<WeaponProperties>(true);
            foreach (WeaponProperties wp in weaponProperties)
            {
                wp.Initialize();
                wp.GetComponent<WeaponHolder>().Initialize();

                foreach (Attatchment a in wp.GetComponentsInChildren<Attatchment>())
                {
                    a.Initialize();
                }
            }

        }

        if (spawn_camera != null)
        {
            spawn_camera.enabled = false;
            spawn_camera.GetComponent<AudioListener>().enabled = false;
        }
    }

    private void DisableItens()
    {
        if (is_respawning) return;

        if (reespawn_delay > 0)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Spawn delay: " + reespawn_delay.ToString("F1") + "s", 1);
            return;
        }

        if (PlayerLoadoutCustomization.Instance != null)
        {
            PlayerLoadoutCustomization.Instance.gameObject.SetActive(false);
        }

        if (VehicleLoadoutCustomization.Instance != null)
        {
            VehicleLoadoutCustomization.Instance.gameObject.SetActive(false);
        }

        if (canvas != null)
        {
            canvas.SetActive(false);
        }
        else
        {
            Debug.LogError("Falha crítica: Canvas não encontrado no Cliente!");
        }
    }

    public void Reestart()
    {
        if (!IsOwner) return;

        EnablePlayerCustomization();
        if (spawn_camera != null)
        {
            spawn_camera.transform.position = initialCameraPosition;
            spawn_camera.transform.rotation = initialCameraRotation;
            spawn_camera.enabled = true;
            spawn_camera.GetComponent<AudioListener>().enabled = true;
        }

        if (PlayerLoadoutCustomization.Instance != null)
        {
            PlayerLoadoutCustomization.Instance.gameObject.SetActive(true);
        }

        if (canvas != null)
        {
            canvas.SetActive(true);
        }
    }
    private Coroutine fov_transition_coroutine;
    public void EnablePlayerCustomization()
    {
        if (!IsOwner) return; // Proteção para rodar apenas localmente

        ToggleFlagsVisibility(vehicle_spawn_flags, false);
        ToggleFlagsVisibility(infantary_spawn_flags, true);

        PlayerLoadoutCustomization.Instance.gameObject.SetActive(true);
        VehicleLoadoutCustomization.Instance.gameObject.SetActive(false);

        if (fov_transition_coroutine != null) StopCoroutine(fov_transition_coroutine);

        fov_transition_coroutine = StartCoroutine(CameraFOVChange(80));
    }

    public void EnableVehicleCustomization()
    {
        if (!IsOwner) return; // Proteção para rodar apenas localmente

        ToggleFlagsVisibility(vehicle_spawn_flags, true);
        ToggleFlagsVisibility(infantary_spawn_flags, false);

        PlayerLoadoutCustomization.Instance.gameObject.SetActive(false);
        VehicleLoadoutCustomization.Instance.gameObject.SetActive(true);

        if (fov_transition_coroutine != null) StopCoroutine(fov_transition_coroutine);

        fov_transition_coroutine = StartCoroutine(CameraFOVChange(120));
    }

    // A função corrigida que não usa SetActive no GameObject principal
    private void ToggleFlagsVisibility(GameObject[] flags, bool state)
    {
        if (flags == null) return;

        foreach (GameObject flag in flags)
        {
            if (flag == null) continue;

            // Liga/Desliga apenas a malha 3D (para esconder visualmente)
            UnityEngine.UI.Image[] images = flag.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (UnityEngine.UI.Image i in images)
            {
                i.enabled = state;
            }

            // Liga/Desliga a colisão (para não permitir o clique)
            Collider[] colliders = flag.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                c.enabled = state;
            }
        }
    }

    private IEnumerator CameraFOVChange(float targetFov)
    {
        float timer = 0f;
        float duration = 1f;
        float startFov = spawn_camera.fieldOfView;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = transitionCurve.Evaluate(timer / duration);
            spawn_camera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            yield return null;
        }
        spawn_camera.fieldOfView = targetFov;

        fov_transition_coroutine = null;
    }

    public void SwitchPerspectiveButtons(bool state)
    {
        infantaryVehicleSwitch.parent.SetActive(state);
    }

}