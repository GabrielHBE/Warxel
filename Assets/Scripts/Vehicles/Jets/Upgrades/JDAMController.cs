using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoxelDestructionPro.Demo;

public class JDAMController : MonoBehaviour, JetUpgradeController
{
    [SerializeField] private GameObject JDAM;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private AudioListener local_audio;
    [SerializeField] private Jet jet;
    [SerializeField] private LayerMask groundLayerMask = 1;

    [Header("Camera and hud")]
    [SerializeField] private Camera local_camera;
    [SerializeField] private Volume volume;

    public GameObject hud;

    [Header("Prediction Settings")]
    [SerializeField] private float predictionTimeStep = 0.02f; // Mais preciso
    [SerializeField] private int maxPredictionSteps = 500; // Mais passos
    [SerializeField] private bool showPredictionDebug = true;
    [SerializeField] private float cameraFollowSpeed = 3f;

    [Header("Advanced Prediction")]
    [SerializeField] private bool useAdvancedPhysics = true;
    [SerializeField] private float dragCoefficient = 0.1f;
    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private float crossSectionalArea = 0.5f;

    [Header("Post-Processing Effects")]
    [SerializeField] private bool enableFilmGrain = true;
    [SerializeField] private Texture filmGrainTexture;
    [SerializeField] private float filmGrainIntensity = 0.442f;
    [SerializeField] private float filmGrainResponse = 0.496f;

    private FilmGrain filmGrainComponent;
    private bool filmGrainAdded = false;

    private GameObject currentItem;
    private Jdam currentJdam;
    private Vector3 predictedImpactPoint;
    private Vector3 lastPredictedImpactPoint;
    private bool hasValidPrediction = false;
    private bool isJDAMLaunched = false;
    private LineRenderer predictionLine;
    private Quaternion originalCameraRotation;
    private bool isResettingCamera = false;
    private float cameraResetSpeed = 2f;

    bool JDAMReady;
    bool is_using_camera;


    void Start()
    {
        volume = GetVolume();
        jet = GetComponentInParent<Jet>();
        jet.upgrade = transform.GetComponent<JetUpgradeController>();
        local_audio.enabled = false;

        originalCameraRotation = local_camera.transform.localRotation;
        CreatePredictionLine();
        SpawnNewJDAM();
        RemoveFilmGrainEffect();
    }

    Volume GetVolume()
    {

        GameObject globalVolumeObj = GameObject.FindGameObjectWithTag("GlobalVolume");
        if (globalVolumeObj != null)
        {
            return globalVolumeObj.GetComponent<Volume>();
        }

        return null;

    }


    void Update()
    {
        if (currentItem == null && !isResettingCamera)
        {
            StartCameraReset();
        }

        if (isResettingCamera)
        {
            ResetCameraToOriginal();
        }

        CheckIfJDAMLaunched();

        if (!isJDAMLaunched)
        {
            PredictImpactPointBeforeLaunch();

            if (hasValidPrediction)
            {
                PointCameraAtImpactPoint(predictedImpactPoint);
            }
        }
        else
        {
            if (hasValidPrediction)
            {
                PointCameraAtImpactPoint(lastPredictedImpactPoint);
            }
        }

        if (currentItem == null)
        {
            spawnInterval -= Time.deltaTime;
            if (spawnInterval <= 0f)
            {
                SpawnNewJDAM();
                spawnInterval = 10f;
            }
        }

    }

    private void PredictImpactPointBeforeLaunch()
    {
        if (currentJdam == null) return;

        Vector3 launchPosition = spawnPoint.position;
        Vector3 initialVelocity = CalculateInitialVelocity();
        Vector3 gravity = Physics.gravity;

        Vector3 currentPosition = launchPosition;
        Vector3 currentVelocity = initialVelocity;

        List<Vector3> trajectoryPoints = new List<Vector3>();
        trajectoryPoints.Add(currentPosition);

        hasValidPrediction = false;
        predictedImpactPoint = launchPosition;

        // Simulação física mais precisa
        for (int i = 0; i < maxPredictionSteps; i++)
        {
            // Aplicar arrasto aerodinâmico se ativado
            if (useAdvancedPhysics)
            {
                ApplyAerodynamicDrag(ref currentVelocity);
            }

            // Integração de Verlet para maior precisão
            Vector3 previousPosition = currentPosition;

            // Calcular aceleração (gravidade + arrasto)
            Vector3 acceleration = gravity;
            if (useAdvancedPhysics)
            {
                acceleration += CalculateDragForce(currentVelocity) / GetJDAMMass();
            }

            // Atualizar velocidade e posição
            currentVelocity += acceleration * predictionTimeStep;
            currentPosition += currentVelocity * predictionTimeStep;

            trajectoryPoints.Add(currentPosition);

            // Detecção de colisão mais precisa
            if (CheckGroundCollision(currentPosition, previousPosition, out Vector3 collisionPoint))
            {
                predictedImpactPoint = collisionPoint;
                hasValidPrediction = true;
                UpdatePredictionLine(trajectoryPoints);
                break;
            }

            // Critérios de parada otimizados
            if (ShouldStopPrediction(i, currentPosition.y, launchPosition.y, currentVelocity.y))
            {
                predictedImpactPoint = currentPosition;
                hasValidPrediction = false;
                break;
            }

            predictedImpactPoint = currentPosition;
        }

        if (!hasValidPrediction && trajectoryPoints.Count > 1)
        {
            predictedImpactPoint = trajectoryPoints[trajectoryPoints.Count - 1];
            hasValidPrediction = true; // Considerar válida mesmo sem colisão
        }
    }

    private bool CheckGroundCollision(Vector3 currentPos, Vector3 previousPos, out Vector3 collisionPoint)
    {
        collisionPoint = currentPos;

        // Raycast mais preciso na direção do movimento
        Vector3 movementDirection = (currentPos - previousPos).normalized;
        float movementDistance = Vector3.Distance(currentPos, previousPos);

        if (Physics.Raycast(previousPos, movementDirection, out RaycastHit hit, movementDistance + 0.5f, groundLayerMask))
        {
            collisionPoint = hit.point;
            return true;
        }

        // Verificação adicional abaixo da posição atual
        if (Physics.Raycast(currentPos + Vector3.up * 2f, Vector3.down, out RaycastHit hitDown, 1000f, groundLayerMask))
        {
            collisionPoint = hitDown.point;
            return true;
        }

        return false;
    }

    private bool ShouldStopPrediction(int step, float currentHeight, float launchHeight, float verticalVelocity)
    {
        // Parar se estiver subindo muito alto sem perspectiva de queda
        if (step > 100 && verticalVelocity > 0 && currentHeight > launchHeight + 200f)
            return true;

        // Parar se estiver caindo muito abaixo do terreno esperado
        if (currentHeight < -100f)
            return true;

        // Parar se excedeu o número máximo de passos
        if (step >= maxPredictionSteps - 1)
            return true;

        return false;
    }

    private void ApplyAerodynamicDrag(ref Vector3 velocity)
    {
        if (velocity.magnitude < 0.1f) return;

        float dragForce = 0.5f * airDensity * velocity.sqrMagnitude * dragCoefficient * crossSectionalArea;
        Vector3 dragAcceleration = -velocity.normalized * (dragForce / GetJDAMMass());

        velocity += dragAcceleration * predictionTimeStep;
    }

    private Vector3 CalculateDragForce(Vector3 velocity)
    {
        if (velocity.magnitude < 0.1f) return Vector3.zero;

        float dragForceMagnitude = 0.5f * airDensity * velocity.sqrMagnitude * dragCoefficient * crossSectionalArea;
        return -velocity.normalized * dragForceMagnitude;
    }

    private float GetJDAMMass()
    {
        if (currentJdam == null) return 100f; // Massa padrão

        Rigidbody rb = currentJdam.GetComponent<Rigidbody>();
        return rb != null ? rb.mass : 100f;
    }

    private Vector3 CalculateInitialVelocity()
    {
        Vector3 jetVelocity = Vector3.zero;
        Rigidbody jetRb = jet.GetComponent<Rigidbody>();
        if (jetRb != null)
        {
            jetVelocity = jetRb.linearVelocity;
        }

        // Usar a direção do spawn point que deve estar alinhado com o jato
        Vector3 launchDirection = spawnPoint.forward;

        // Velocidade inicial mais realista
        Vector3 initialVelocity = jetVelocity + launchDirection * 2f; // Reduzir impulso forward

        return initialVelocity;
    }

    private void CreatePredictionLine()
    {
        predictionLine = gameObject.AddComponent<LineRenderer>();
        predictionLine.material = new Material(Shader.Find("Sprites/Default"));
        predictionLine.startColor = Color.red;
        predictionLine.endColor = Color.yellow;
        predictionLine.startWidth = 0.3f;
        predictionLine.endWidth = 0.1f;
        predictionLine.positionCount = 0;
        predictionLine.useWorldSpace = true;
    }

    // Resto dos métodos permanecem iguais...
    private void StartCameraReset()
    {
        isResettingCamera = true;
        hasValidPrediction = false;
        isJDAMLaunched = false;

        if (predictionLine != null)
            predictionLine.enabled = false;
    }

    private void ResetCameraToOriginal()
    {
        if (local_camera == null) return;

        local_camera.transform.localRotation = Quaternion.Slerp(
            local_camera.transform.localRotation,
            originalCameraRotation,
            Time.deltaTime * cameraResetSpeed
        );

        if (Quaternion.Angle(local_camera.transform.localRotation, originalCameraRotation) < 0.2f)
        {
            local_camera.transform.localRotation = originalCameraRotation;
            isResettingCamera = false;
        }
    }

    private void CheckIfJDAMLaunched()
    {
        if (currentJdam == null) return;

        Rigidbody jdamRb = currentJdam.GetComponent<Rigidbody>();
        if (jdamRb != null && !jdamRb.isKinematic && !isJDAMLaunched)
        {
            isJDAMLaunched = true;
            lastPredictedImpactPoint = predictedImpactPoint;
            if (predictionLine != null)
                predictionLine.enabled = false;
        }
        else if (jdamRb != null && jdamRb.isKinematic)
        {
            isJDAMLaunched = false;
        }
    }

    private void PointCameraAtImpactPoint(Vector3 targetPoint)
    {
        if (local_camera == null) return;

        Vector3 directionToTarget = targetPoint - local_camera.transform.position;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            local_camera.transform.rotation = Quaternion.Slerp(
                local_camera.transform.rotation,
                targetRotation,
                Time.deltaTime * cameraFollowSpeed
            );
        }
    }

    private void UpdatePredictionLine(List<Vector3> points)
    {
        if (predictionLine == null) return;

        if (hasValidPrediction && showPredictionDebug && !isJDAMLaunched)
        {
            predictionLine.positionCount = points.Count;
            predictionLine.SetPositions(points.ToArray());
            predictionLine.enabled = true;
        }
        else
        {
            predictionLine.enabled = false;
        }
    }

    private void SpawnNewJDAM()
    {
        currentItem = Instantiate(JDAM, spawnPoint.position, spawnPoint.rotation);
        currentItem.transform.SetParent(transform);
        currentItem.transform.position = spawnPoint.position;
        currentItem.transform.rotation = spawnPoint.rotation;
        currentJdam = currentItem.GetComponent<Jdam>();
        isJDAMLaunched = false;
        isResettingCamera = false;
        local_camera.transform.localRotation = originalCameraRotation;
        JDAMReady = true;
    }

    private void OnDrawGizmos()
    {
        if (hasValidPrediction && showPredictionDebug && !isResettingCamera)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(predictedImpactPoint, 0.5f);

            if (isJDAMLaunched)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(lastPredictedImpactPoint, 0.7f);
                Gizmos.DrawWireSphere(lastPredictedImpactPoint, 2f);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(predictedImpactPoint, 2f);
        }
    }


    public void Shoot()
    {
        currentJdam.Shoot();
        JDAMReady = false;
    }

    public bool CanShoot()
    {
        return JDAMReady;
    }

    public void AddFilmGrainEffect()
    {
        if (volume == null || volume.profile == null) return;

        // Verificar se já existe o componente
        if (!volume.profile.TryGet<FilmGrain>(out filmGrainComponent))
        {
            filmGrainComponent = volume.profile.Add<FilmGrain>();
            filmGrainAdded = true;
            Debug.Log("Film Grain adicionado ao volume");
        }

        ConfigureFilmGrain();
    }

    private void ConfigureFilmGrain()
    {
        if (filmGrainComponent == null) return;

        // Tipo Custom
        filmGrainComponent.type.overrideState = true;
        filmGrainComponent.type.value = FilmGrainLookup.Custom;

        // Texture
        if (filmGrainTexture != null)
        {
            filmGrainComponent.texture.overrideState = true;
            filmGrainComponent.texture.value = filmGrainTexture;
        }

        // Intensity
        filmGrainComponent.intensity.overrideState = true;
        filmGrainComponent.intensity.value = filmGrainIntensity;

        // Response
        filmGrainComponent.response.overrideState = true;
        filmGrainComponent.response.value = filmGrainResponse;
    }

    // Remover efeito Film Grain
    public void RemoveFilmGrainEffect()
    {
        if (volume == null || volume.profile == null) return;

        if (volume.profile.TryGet<FilmGrain>(out filmGrainComponent))
        {
            volume.profile.Remove<FilmGrain>();
            filmGrainComponent = null;
            filmGrainAdded = false;
            Debug.Log("Film Grain removido do volume");
        }
    }

    // Atualizar propriedades do Film Grain em tempo de execução
    public void UpdateFilmGrainProperties(float intensity, float response, Texture texture = null)
    {
        if (filmGrainComponent == null) return;

        filmGrainIntensity = intensity;
        filmGrainResponse = response;

        if (texture != null)
        {
            filmGrainTexture = texture;
        }

        ConfigureFilmGrain();
    }

    // Alternar Film Grain (ligar/desligar)
    public void ToggleFilmGrain()
    {
        enableFilmGrain = !enableFilmGrain;

        if (enableFilmGrain)
        {
            AddFilmGrainEffect();
        }
        else
        {
            RemoveFilmGrainEffect();
        }
    }

    // Controlar efeitos baseado no estado da câmera
    private void ControlPostProcessingEffects()
    {
        // Exemplo: Ativar Film Grain apenas quando a câmera do JDAM estiver ativa
        bool jdamCameraActive = local_camera.enabled;

        if (jdamCameraActive && !filmGrainAdded && enableFilmGrain)
        {
            AddFilmGrainEffect();
        }
        else if (!jdamCameraActive && filmGrainAdded)
        {
            RemoveFilmGrainEffect();
        }
    }

    // Método para adicionar múltiplos efeitos (expandível)
    public void AddMultipleEffects(bool addFilmGrain, bool addBloom, bool addVignette)
    {
        if (addFilmGrain)
        {
            AddFilmGrainEffect();
        }

        // Você pode adicionar mais efeitos aqui seguindo o mesmo padrão
        // if (addBloom) AddBloomEffect();
        // if (addVignette) AddVignetteEffect();
    }

    // Remover todos os efeitos do volume
    public void RemoveAllEffects()
    {
        if (volume == null || volume.profile == null) return;

        // Lista de tipos de efeitos para remover
        List<System.Type> effectTypes = new List<System.Type>
        {
            typeof(FilmGrain),
            typeof(Bloom),
            typeof(Vignette),
            typeof(ColorAdjustments)
            // Adicione mais tipos conforme necessário
        };

        foreach (var effectType in effectTypes)
        {
            RemoveEffectByType(effectType);
        }

        filmGrainAdded = false;
        Debug.Log("Todos os efeitos de pós-processamento removidos");
    }

    // Método genérico para remover efeitos por tipo
    private void RemoveEffectByType(System.Type effectType)
    {
        if (volume.profile.TryGet(effectType, out VolumeComponent component))
        {
            volume.profile.Remove(effectType);
        }
    }


    // Modificar o método UseCamera para controlar os efeitos
    public void UseCamera(bool active)
    {
        if (active)
        {
            hud.SetActive(true);
            jet.vehicle_camera.enabled = false;
            jet.vehicle_audio_listener.enabled = false;
            local_camera.enabled = true;
            local_audio.enabled = true;

            // Ativar efeitos quando a câmera do JDAM estiver ativa
            if (enableFilmGrain)
            {
                AddFilmGrainEffect();
            }
        }
        else
        {
            hud.SetActive(true);
            jet.vehicle_camera.enabled = true;
            jet.vehicle_audio_listener.enabled = true;
            local_camera.enabled = false;
            local_audio.enabled = false;

            // Remover efeitos quando voltar para a câmera normal
            RemoveFilmGrainEffect();
        }
    }

    // ... [resto dos seus métodos existentes] ...

    // Método para debug no Inspector
    [ContextMenu("Toggle Film Grain")]
    private void DebugToggleFilmGrain()
    {
        ToggleFilmGrain();
    }

    [ContextMenu("Add Film Grain")]
    private void DebugAddFilmGrain()
    {
        AddFilmGrainEffect();
    }

    [ContextMenu("Remove Film Grain")]
    private void DebugRemoveFilmGrain()
    {
        RemoveFilmGrainEffect();
    }

    public void SetActive(bool active)
    {
        throw new System.NotImplementedException();
    }
}