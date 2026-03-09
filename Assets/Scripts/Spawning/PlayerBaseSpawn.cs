using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class PlayerBaseSpawn : MonoBehaviour
{
    [SerializeField] private float reespawn_delay;
    [SerializeField] private TextMeshProUGUI reespawn_delay_text;
    [SerializeField] private PlayerLoadoutCustomization playerLoadoutCustomization;
    [SerializeField] private GameObject player_prefab;
    [SerializeField] private Transform[] spawn_points;
    [SerializeField] private float dragSpeed = 50f;
    [SerializeField] private Vector2 boundsX = new Vector2(-5000f, 5000f);
    [SerializeField] private Vector2 boundsZ = new Vector2(-5000f, 5000f);

    [Header("Camera Settings")]
    [SerializeField] private Camera spawn_camera;
    [SerializeField] private float orthographicSize = 170;
    [SerializeField] private float minOrthographicSize = 50f;
    [SerializeField] private float maxOrthographicSize;
    [SerializeField] private float zoomSpeed = 50f;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Messages")]
    [SerializeField] private TextMeshProUGUI deployment_messages;
    [SerializeField] private Image deployment_messages_image;

    private float x;
    private float y;

    public GameObject player;
    private Vector3 spawn_camera_original_pos;
    private Quaternion spawn_camera_original_rot;
    private Vector3 dragOrigin;
    private bool isTransitioning = false;
    private bool canDrag = true;
    private bool playerWasDestroyed = false;

    private bool isSpawning = false;

    private float original_spawn_delay;

    void Start()
    {
        original_spawn_delay = reespawn_delay;
        reespawn_delay = 0;
        x = spawn_camera.transform.rotation.x;
        y = spawn_camera.transform.rotation.y;

        maxOrthographicSize = orthographicSize;
        spawn_camera_original_pos = spawn_camera.transform.position;
        spawn_camera_original_rot = spawn_camera.transform.rotation;
    }

    void Update()
    {
        if (player == null)
        {
            if (reespawn_delay > 0)
            {
                reespawn_delay -= Time.deltaTime;
                if (playerLoadoutCustomization._currentStage == PlayerLoadoutCustomization.SelectionStage.ClassSelection)
                {
                    reespawn_delay_text.text = "Spawn delay: " + reespawn_delay.ToString("F1");
                }
                else
                {
                    reespawn_delay_text.text = "";
                }
            }
            else
            {
                reespawn_delay_text.text = "";
            }
        }
        else
        {
            reespawn_delay_text.text = "";
            reespawn_delay = original_spawn_delay;
        }

        CheckPlayerDestroyed();

        if (player == null && !SettingsHUD.Instance.is_menu_settings_active)
        {
            playerLoadoutCustomization.gameObject.SetActive(true);

            // Só permite drag/zoom se não estiver spawnando
            if (!isTransitioning && canDrag && !SettingsHUD.Instance.is_menu_settings_active)
            {
                HandleCameraDrag();
                HandleCameraZoom();
                HandleCameraRotation();
            }
        }
        else
        {
            playerLoadoutCustomization.gameObject.SetActive(false);
        }
    }

    void CheckPlayerDestroyed()
    {
        if (playerWasDestroyed && player == null)
        {
            ResetSpawnCamera();
            playerWasDestroyed = false;
        }
    }

    void HandleCameraDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CheckForVehicleClick())
            {
                return;
            }
            dragOrigin = Input.mousePosition;
            return;
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

    [SerializeField] private float zoomSmoothTime = 0.1f;
    private float zoomVelocity = 0f;
    float targetSize = 170;

    private void HandleCameraZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            float zoomAmount = scrollInput * zoomSpeed;
            targetSize = spawn_camera.orthographicSize - zoomAmount;
            targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
        }

        spawn_camera.orthographicSize = Mathf.SmoothDamp(
            spawn_camera.orthographicSize,
            targetSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    private void HandleCameraRotation()
    {
        if (Input.GetMouseButton(1))
        {
            x -= Input.GetAxis("Mouse Y");
            y += Input.GetAxis("Mouse X");

            spawn_camera.transform.rotation = Quaternion.Euler(x, y, 0f);
        }
    }

    private bool CheckForVehicleClick()
    {
        Ray ray = spawn_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vehicle vehicle = hit.collider.GetComponent<Vehicle>();
            if (vehicle != null && vehicle.can_spawn_in_vehicle)
            {
                if (AccountManager.Instance.selected_class == ClassManager.Class.Pilot)
                {
                    StartCoroutine(TransitionToVehicle(vehicle));
                    return true;
                }
                else
                {
                    GeneralHudAlertMessages.Instance.CreateMessage("Only the pilot Class can drive vehicles", 2);
                }
            }
        }

        return false;
    }

    void OnMouseDown()
    {
        if (reespawn_delay > 0)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Wait " + reespawn_delay.ToString("F1") + " seconds to spawn", 2);
            return;
        }

        if (isTransitioning || !canDrag || player != null || isSpawning) return;

        StartCoroutine(TransitionToSpawn());
    }

    private IEnumerator TransitionToSpawn()
    {
        isTransitioning = true;
        canDrag = false;

        Transform selectedSpawnPoint = spawn_points[Random.Range(0, spawn_points.Length)];
        Vector3 targetPosition = selectedSpawnPoint.position;
        Vector3 cameraStartPosition = spawn_camera.transform.position;
        Vector3 cameraTargetPosition = targetPosition + new Vector3(0, 5f, 0);

        float originalOrthographicSize = spawn_camera.orthographicSize;

        Quaternion startRotation = spawn_camera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - cameraTargetPosition);

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            spawn_camera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, t);
            spawn_camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        elapsedTime = 0f;

        while (spawn_camera.orthographicSize >= 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            spawn_camera.orthographicSize = Mathf.Lerp(originalOrthographicSize, 0, t);

            yield return null;
        }

        // Spawna o player diretamente
        SpawnPlayer(targetPosition, selectedSpawnPoint.rotation);

        spawn_camera.gameObject.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator TransitionToVehicle(Vehicle vehicle)
    {
        isTransitioning = true;
        canDrag = false;

        Vector3 targetPosition = vehicle.transform.position;
        Vector3 cameraStartPosition = spawn_camera.transform.position;
        Vector3 cameraTargetPosition = targetPosition + new Vector3(0, 5f, 0);

        float originalOrthographicSize = spawn_camera.orthographicSize;

        Quaternion startRotation = spawn_camera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - cameraTargetPosition);

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            spawn_camera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, t);
            spawn_camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        elapsedTime = 0f;

        while (spawn_camera.orthographicSize > 10.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            spawn_camera.orthographicSize = Mathf.Lerp(originalOrthographicSize, 10, t);

            yield return null;
        }

        SpawnPlayerAtVehicle(vehicle);
        spawn_camera.gameObject.SetActive(false);
        isTransitioning = false;
    }

    // Método para spawnar o player
    private void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (player_prefab == null)
        {
            Debug.LogError("Player Prefab não atribuído!");
            return;
        }

        isSpawning = true;

        Debug.Log("Spawnando player");

        GameObject playerInstance = Instantiate(player_prefab, spawnPosition, spawnRotation);
        player = playerInstance;

        SwitchWeapon switchWeapon = playerInstance.GetComponentInChildren<SwitchWeapon>();
        if (switchWeapon != null)
        {
            switchWeapon.primary = playerLoadoutCustomization.GetCurrentPrimaryWeapon();
            switchWeapon.secondary = playerLoadoutCustomization.GetCurrentSecondaryWeapon();
            switchWeapon.gadget1 = playerLoadoutCustomization.GetCurrentGadget1();
            switchWeapon.gadget2 = playerLoadoutCustomization.GetCurrentGadget2();
            switchWeapon.Initialize();
        }

        PlayerController playerController = playerInstance.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.InitializePlayer();

            WeaponProperties[] weaponProperties = playerInstance.GetComponentsInChildren<WeaponProperties>(true);
            foreach (WeaponProperties wp in weaponProperties)
            {
                wp.Initialize();
            }
        }
        else
        {
            Debug.LogError("PlayerController não encontrado no prefab!");
        }

        playerWasDestroyed = true;
        isSpawning = false;

        Debug.Log("Player spawnado com sucesso");
    }

    private void SpawnPlayerAtVehicle(Vehicle vehicle)
    {
        if (player_prefab == null)
        {
            Debug.LogError("Player Prefab não atribuído!");
            return;
        }

        isSpawning = true;

        Debug.Log($"Spawnando player no veículo: {vehicle.name}");

        GameObject playerInstance = Instantiate(player_prefab, vehicle.exit_vehicle_position.position, vehicle.exit_vehicle_position.rotation);
        player = playerInstance;

        PlayerController playerController = playerInstance.GetComponent<PlayerController>();

        if (playerController != null)
        {
            playerController.InitializePlayer();
            playerController.DisableColliders();

            // Entra no veículo
            vehicle.EnterVehicle(playerInstance);
        }
        else
        {
            Debug.LogError("PlayerController não encontrado no prefab!");
        }

        playerWasDestroyed = true;
        isSpawning = false;

        Debug.Log($"Player spawnado no veículo: {vehicle.name}");
    }

    public void ResetSpawnCamera()
    {
        isTransitioning = false;
        isSpawning = false;
        spawn_camera.transform.position = spawn_camera_original_pos;
        spawn_camera.transform.rotation = spawn_camera_original_rot;
        spawn_camera.gameObject.SetActive(true);
        spawn_camera.orthographicSize = orthographicSize;
        player = null;
        canDrag = true;
    }
}