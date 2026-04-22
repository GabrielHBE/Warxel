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
    [SerializeField] private float reespawn_delay;
    [SerializeField] private TextMeshProUGUI reespawn_delay_text;
    [SerializeField] private GameObject player_prefab;
    [SerializeField] private float dragSpeed = 50f;
    [SerializeField] private Vector2 boundsX = new Vector2(-5000f, 5000f);
    [SerializeField] private Vector2 boundsZ = new Vector2(-5000f, 5000f);

    [Header("Camera Settings")]
    public Camera spawn_camera;
    [SerializeField] private float zoomSpeed = 50f;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [HideInInspector] public GameObject player_instantiated;
    private bool is_respawning = false;
    private float original_spawn_delay;
    private Vector3 dragOrigin;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private bool isInitialized = false;

    private Transform map_spawn_camera_pos;
    public override void OnStartClient()
    {
        base.OnStartClient();

        map_spawn_camera_pos = GameObject.FindWithTag("SpawnCameraPos").transform;
        transform.position = map_spawn_camera_pos.position;
        transform.rotation = map_spawn_camera_pos.rotation;

        if (IsOwner)
        {
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

        isInitialized = true;

        // Inicializa e guarda referência
        StartCoroutine(InitializeInstance());

        // Guarda posição inicial da câmera
        if (spawn_camera != null)
        {
            initialCameraPosition = spawn_camera.transform.position;
            initialCameraRotation = spawn_camera.transform.rotation;
        }
    }

    private void InitializeForNonOwner()
    {
        // Desativa a câmera para não-owners
        if (spawn_camera != null)
        {
            spawn_camera.enabled = false;
            spawn_camera.GetComponent<AudioListener>().enabled = false;
        }
        enabled = false; // Desativa o Update para não-owners
    }

    private IEnumerator InitializeInstance()
    {
        yield return new WaitForSeconds(0.2f);
        if (base.IsOwner)
        {
            PlayerSpawnManager.Instance?.SetPlayerSpawnController(this);
        }
    }

    private void Update()
    {
        // Só executa se for owner e estiver inicializado
        if (!base.IsOwner || !isInitialized) return;

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

        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
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

    public void InitializeSpawnPlayer(Transform spawn_point)
    {
        // Só permite spawn se for owner
        if (!IsOwner) return;

        if (is_respawning) return;

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

        if (PlayerLoadoutCustomization.Instance != null)
        {
            PlayerLoadoutCustomization.Instance.gameObject.SetActive(false);
        }

        is_respawning = true;
        StartCoroutine(TransitionCameraToSpawnPoint(spawn_point));
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

        // Passa posição e rotação em vez do Transform
        SpawnPlayer(spawn_point.position, spawn_point.rotation);
    }

    [ServerRpc]
    private void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        NetworkManager manager = InstanceFinder.NetworkManager;

        GameObject spawnedObject = Instantiate(player_prefab, spawnPosition, spawnRotation);
        NetworkObject spawnedNetworkObject = spawnedObject.GetComponent<NetworkObject>();

        // Spawna o objeto para o owner específico
        manager.ServerManager.Spawn(spawnedNetworkObject, Owner);

        player_instantiated = spawnedObject;



        // NOTIFICA APENAS O OWNER usando TargetRpc
        TargetOnSpawnComplete(Owner, spawnedNetworkObject.gameObject);


    }


    [TargetRpc]
    private void TargetOnSpawnComplete(NetworkConnection conn, GameObject spawnedPlayer)
    {
        // Este método só executa no cliente alvo (o owner)
        // O parâmetro conn é o NetworkConnection do cliente alvo

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
            playerController.GetComponent<PlayerProperties>().player_name = AccountManager.Instance.account_name;
            WeaponProperties[] weaponProperties = player_instantiated.GetComponentsInChildren<WeaponProperties>(true);
            foreach (WeaponProperties wp in weaponProperties)
            {
                wp.GetComponent<WeaponHolder>().SetHands();
                wp.Initialize();
            }

        }

        if (spawn_camera != null)
        {
            spawn_camera.enabled = false;
            spawn_camera.GetComponent<AudioListener>().enabled = false;
        }
    }

    public void Reestart()
    {
        if (!IsOwner) return;

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
    }
}