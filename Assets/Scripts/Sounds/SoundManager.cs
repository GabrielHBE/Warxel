using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Pool de Áudio")]
    [SerializeField] private AudioDistanceController audioDistanceControllerPrefab;
    [SerializeField, Tooltip("Prefab genérico vazio com o script LocalPooledObject e um AudioSource para sons 2D e Loops")]
    private GameObject audio2DPrefab;

    [Header("Configurações de Áudio (Atualizado Automatically)")]
    [SerializeField] private List<AudioClip> internalAudioList = new List<AudioClip>();
    [SerializeField] private string rootFolder = "Assets/Sounds";

    private readonly Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    private static readonly List<LoopAudio> loopAudioList = new List<LoopAudio>();

    // Variáveis estáticas para permitir acesso nos métodos estáticos
    private static AudioDistanceController staticAudioDistanceController;
    private static GameObject staticAudio2DPrefab;

    void Start()
    {
        staticAudioDistanceController = audioDistanceControllerPrefab;
        staticAudio2DPrefab = audio2DPrefab;
        Instance = this;
        InitializeAudioCache();
    }

    void Update()
    {
        if (loopAudioList.Count == 0) return;

        for (int i = loopAudioList.Count - 1; i >= 0; i--)
        {
            LoopAudio audio = loopAudioList[i];

            if (audio.target != null)
            {
                audio.transform.position = audio.target.position;
            }
            else
            {
                if (audio.audioSource != null) audio.audioSource.Stop();

                // Em vez de Destroy, usa Deactivate se for um item do Pool
                if (audio.gameObject != null)
                {
                    if (audio.gameObject.TryGetComponent(out LocalPooledObject pooled)) pooled.Deactivate();
                    else Destroy(audio.gameObject);
                }

                loopAudioList.RemoveAt(i);
            }
        }
    }

    private void InitializeAudioCache()
    {
        audioCache.Clear();
        foreach (var clip in internalAudioList)
        {
            if (clip == null) continue;
            if (audioCache.TryAdd(clip.name, clip)) continue;
            Debug.LogWarning($"[SoundManager] Áudio duplicado detectado e ignorado: '{clip.name}'");
        }
    }

    protected override void OnValidate()
    {
#if UNITY_EDITOR
        base.OnValidate();
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) return;
        UpdateSoundsButton();
#endif
    }

    [ContextMenu("Update Sounds Button")]
    public void UpdateSoundsButton()
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogWarning($"[SoundManager] A pasta '{rootFolder}' não foi encontrada!");
            return;
        }

        internalAudioList.Clear();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { rootFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip != null && !internalAudioList.Contains(clip))
            {
                internalAudioList.Add(clip);
            }
        }

        EditorUtility.SetDirty(this);
#endif
    }

    #region Helpers de Criação de Áudio
    private static AudioSource CreateConfiguredAudioSource(GameObject go, AudioClip clip, SoundProperties props, bool is3D, bool loop)
    {
        // Se vier do Pool, usa o componente existente para não adicionar múltiplos AudioSources
        if (!go.TryGetComponent(out AudioSource audioSource))
        {
            audioSource = go.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.priority = props.priority;
        audioSource.volume = props.volume;
        audioSource.pitch = props.pitch;
        audioSource.panStereo = props.stereoPan;
        audioSource.spatialBlend = is3D ? props.spatialBlend : 0f;
        audioSource.reverbZoneMix = props.reverbZoneMix;

        if (is3D)
        {
            audioSource.dopplerLevel = props.dopplerLevel;
            audioSource.spread = props.spread;
            audioSource.rolloffMode = props.rolloffMode;
            audioSource.minDistance = props.minDistance;
            audioSource.maxDistance = props.maxDistance;
        }

        return audioSource;
    }

    private static void SetupLoopAudio(string namePrefix, AudioClip clip, SoundProperties props, Transform target, bool is3D)
    {
        foreach (var existingLoop in loopAudioList)
        {
            if (existingLoop.target == target && existingLoop.audioSource != null && existingLoop.audioSource.clip == clip)
            {
                if (!existingLoop.audioSource.isPlaying) existingLoop.audioSource.Play();
                return;
            }
        }

        // Pega do Pool em vez de fazer "new GameObject()"
        GameObject loopObject = ObjectPooling.Instance.GetLocalPooledItem(staticAudio2DPrefab);
        if (loopObject == null) return;

        loopObject.GetComponent<LocalPooledObject>().Activate();
        loopObject.name = $"{namePrefix}_{clip.name}";

        if (target != null) loopObject.transform.position = target.position;

        AudioSource audioSource = CreateConfiguredAudioSource(loopObject, clip, props, is3D, true);

        LoopAudio loopAudio = new LoopAudio
        {
            audioSource = audioSource,
            target = target,
            transform = loopObject.transform,
            gameObject = loopObject
        };

        audioSource.Play();
        loopAudioList.Add(loopAudio);
    }
    #endregion

    #region Helpers de Modificação de Áudio
    private static void ModifyLoopAudio(AudioClip clip, Transform target, Action<AudioSource> action)
    {
        foreach (var loopAudio in loopAudioList)
        {
            if (loopAudio.target == target && loopAudio.audioSource != null && loopAudio.audioSource.clip == clip)
            {
                action(loopAudio.audioSource);
            }
        }
    }

    private static void DestroyLoopAudio(AudioClip clip, Transform target)
    {
        loopAudioList.RemoveAll(loopAudio =>
        {
            if (loopAudio.target == target && loopAudio.audioSource != null && loopAudio.audioSource.clip == clip)
            {
                loopAudio.audioSource.Stop();
                if (loopAudio.gameObject.TryGetComponent(out LocalPooledObject pooled)) pooled.Deactivate();
                return true;
            }
            return false;
        });
    }
    #endregion

    #region 3d Audio
    public void RequestPlay3dSound(string soundName, SoundProperties soundProperties, Vector3 position, bool playForCaller)
    {
        if (IsServerStarted)
        {
            int callerId = IsClientStarted ? ClientManager.Connection.ClientId : -1;
            RpcPlay3dSoundObservers(soundName, soundProperties, position, callerId, playForCaller);
        }
        else ServerPlay3dSound(soundName, position, soundProperties, playForCaller);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPlay3dSound(string soundName, Vector3 position, SoundProperties soundProperties, bool playForCaller, NetworkConnection caller = null)
    {
        int callerId = caller != null ? caller.ClientId : -1;
        RpcPlay3dSoundObservers(soundName, soundProperties, position, callerId, playForCaller);
    }

    [ObserversRpc]
    private void RpcPlay3dSoundObservers(string soundName, SoundProperties soundProperties, Vector3 position, int callerId, bool playForCaller)
    {
        if (!playForCaller && IsClientStarted && ClientManager.Connection.ClientId == callerId) return;
        Play3dSound(soundName, soundProperties, position);
    }

    private void Play3dSound(string soundName, SoundProperties soundProperties, Vector3 position)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip))
        {
            GameObject pooledObj = ObjectPooling.Instance.GetLocalPooledItem(audioDistanceControllerPrefab.gameObject);
            if (pooledObj != null)
            {
                pooledObj.transform.position = position;
                pooledObj.GetComponent<LocalPooledObject>().Activate();

                AudioDistanceController controller = pooledObj.GetComponent<AudioDistanceController>();
                controller.Setup(clip, soundProperties);
                controller.StartGrowth();
            }
        }
        else Debug.LogWarning($"Som '{soundName}' não foi encontrado no AudioManager!");
    }

    public void RequestPlay3dLoopSound(string soundName, SoundProperties soundProperties, Transform target, bool playForCaller)
    {
        if (IsServerStarted)
        {
            int callerId = IsClientStarted ? ClientManager.Connection.ClientId : -1;
            RpcPlay3dLoopSoundObservers(soundName, soundProperties, target, callerId, playForCaller);
        }
        else ServerPlay3dLoopSound(soundName, target, soundProperties, playForCaller);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPlay3dLoopSound(string soundName, Transform target, SoundProperties soundProperties, bool playForCaller, NetworkConnection caller = null)
    {
        int callerId = caller != null ? caller.ClientId : -1;
        RpcPlay3dLoopSoundObservers(soundName, soundProperties, target, callerId, playForCaller);
    }

    [ObserversRpc]
    private void RpcPlay3dLoopSoundObservers(string soundName, SoundProperties soundProperties, Transform target, int callerId, bool playForCaller)
    {
        if (!playForCaller && IsClientStarted && ClientManager.Connection.ClientId == callerId) return;
        Play3dLoopSound(soundName, soundProperties, target);
    }

    private void Play3dLoopSound(string soundName, SoundProperties soundProperties, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip))
            SetupLoopAudio("LoopAudio", clip, soundProperties, target, is3D: true);
        else Debug.LogWarning($"Som '{soundName}' não foi encontrado no AudioManager!");
    }

    // ================= PAUSE =================
    public void RequestPause3dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcPause3dLoopSoundObservers(soundName, target);
        else ServerPause3dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPause3dLoopSound(string soundName, Transform target) => RpcPause3dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcPause3dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.Pause());
    }

    // ================= RESUME =================
    public void RequestResume3dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcResume3dLoopSoundObservers(soundName, target);
        else ServerResume3dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerResume3dLoopSound(string soundName, Transform target) => RpcResume3dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcResume3dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.UnPause());
    }

    // ================= PITCH =================
    public void RequestSet3dLoopSoundPitch(string soundName, Transform target, float newPitch)
    {
        if (IsServerStarted) RpcSet3dLoopSoundPitchObservers(soundName, target, newPitch);
        else ServerSet3dLoopSoundPitch(soundName, target, newPitch);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSet3dLoopSoundPitch(string soundName, Transform target, float newPitch) => RpcSet3dLoopSoundPitchObservers(soundName, target, newPitch);

    [ObserversRpc]
    private void RpcSet3dLoopSoundPitchObservers(string soundName, Transform target, float newPitch)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.pitch = newPitch);
    }

    // ================= STOP =================
    public void RequestStop3dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcStop3dLoopSoundObservers(soundName, target);
        else ServerStop3dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerStop3dLoopSound(string soundName, Transform target) => RpcStop3dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcStop3dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) DestroyLoopAudio(clip, target);
    }
    #endregion

    #region 2d Audio
    public void RequestPlay2dSound(string soundName, SoundProperties soundProperties)
    {
        if (IsServerStarted) RpcPlay2dSoundObservers(soundName, soundProperties);
        else ServerPlay2dSound(soundName, soundProperties);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPlay2dSound(string soundName, SoundProperties soundProperties) => RpcPlay2dSoundObservers(soundName, soundProperties);

    [ObserversRpc]
    private void RpcPlay2dSoundObservers(string soundName, SoundProperties soundProperties) => Play2dSound(soundName, soundProperties);

    private void Play2dSound(string soundName, SoundProperties soundProperties)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) Play2dSoundLocal(clip, soundProperties);
        else Debug.LogWarning($"Som '{soundName}' não foi encontrado no AudioManager!");
    }

    // ================= PLAY 2D LOOP =================
    public void RequestPlay2dLoopSound(string soundName, SoundProperties soundProperties, Transform target, bool playForCaller)
    {
        if (IsServerStarted)
        {
            int callerId = IsClientStarted ? ClientManager.Connection.ClientId : -1;
            RpcPlay2dLoopSoundObservers(soundName, soundProperties, target, callerId, playForCaller);
        }
        else ServerPlay2dLoopSound(soundName, target, soundProperties, playForCaller);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPlay2dLoopSound(string soundName, Transform target, SoundProperties soundProperties, bool playForCaller, NetworkConnection caller = null)
    {
        int callerId = caller != null ? caller.ClientId : -1;
        RpcPlay2dLoopSoundObservers(soundName, soundProperties, target, callerId, playForCaller);
    }

    [ObserversRpc]
    private void RpcPlay2dLoopSoundObservers(string soundName, SoundProperties soundProperties, Transform target, int callerId, bool playForCaller)
    {
        if (!playForCaller && IsClientStarted && ClientManager.Connection.ClientId == callerId) return;
        Play2dLoopSound(soundName, soundProperties, target);
    }

    private void Play2dLoopSound(string soundName, SoundProperties soundProperties, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip))
            SetupLoopAudio("LoopAudio2D", clip, soundProperties, target, is3D: false);
        else Debug.LogWarning($"Som '{soundName}' não foi encontrado no AudioManager!");
    }

    // ================= PAUSE 2D LOOP =================
    public void RequestPause2dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcPause2dLoopSoundObservers(soundName, target);
        else ServerPause2dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPause2dLoopSound(string soundName, Transform target) => RpcPause2dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcPause2dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.Pause());
    }

    // ================= RESUME 2D LOOP =================
    public void RequestResume2dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcResume2dLoopSoundObservers(soundName, target);
        else ServerResume2dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerResume2dLoopSound(string soundName, Transform target) => RpcResume2dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcResume2dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.UnPause());
    }

    // ================= PITCH 2D LOOP =================
    public void RequestSet2dLoopSoundPitch(string soundName, Transform target, float newPitch)
    {
        if (IsServerStarted) RpcSet2dLoopSoundPitchObservers(soundName, target, newPitch);
        else ServerSet2dLoopSoundPitch(soundName, target, newPitch);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSet2dLoopSoundPitch(string soundName, Transform target, float newPitch) => RpcSet2dLoopSoundPitchObservers(soundName, target, newPitch);

    [ObserversRpc]
    private void RpcSet2dLoopSoundPitchObservers(string soundName, Transform target, float newPitch)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) ModifyLoopAudio(clip, target, src => src.pitch = newPitch);
    }

    // ================= STOP 2D LOOP =================
    public void RequestStop2dLoopSound(string soundName, Transform target)
    {
        if (IsServerStarted) RpcStop2dLoopSoundObservers(soundName, target);
        else ServerStop2dLoopSound(soundName, target);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerStop2dLoopSound(string soundName, Transform target) => RpcStop2dLoopSoundObservers(soundName, target);

    [ObserversRpc]
    private void RpcStop2dLoopSoundObservers(string soundName, Transform target)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip)) DestroyLoopAudio(clip, target);
    }
    #endregion

    #region Static Methods
    public static void Play2dSoundLocal(AudioClip clip, SoundProperties soundProperties)
    {
        GameObject tempGO = ObjectPooling.Instance.GetLocalPooledItem(staticAudio2DPrefab);
        if (tempGO == null) return;

        tempGO.GetComponent<LocalPooledObject>().Activate();
        tempGO.name = $"TempAudio2D_{clip.name}";

        AudioSource tempSource = CreateConfiguredAudioSource(tempGO, clip, soundProperties, is3D: false, loop: false);
        tempSource.PlayOneShot(clip);

        // Rotina para devolver ao Pool após o fim do som
        Instance.StartCoroutine(Instance.DeactivatePooledObjectDelay(tempGO, clip.length));
    }

    private System.Collections.IEnumerator DeactivatePooledObjectDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null && go.TryGetComponent(out LocalPooledObject pooled))
        {
            pooled.Deactivate();
        }
    }

    public static void Play3dSoundLocal(AudioClip clip, SoundProperties soundProperties, Vector3 position)
    {
        GameObject pooledObj = ObjectPooling.Instance.GetLocalPooledItem(staticAudioDistanceController.gameObject);
        if (pooledObj != null)
        {
            pooledObj.transform.position = position;
            pooledObj.GetComponent<LocalPooledObject>().Activate();

            AudioDistanceController controller = pooledObj.GetComponent<AudioDistanceController>();
            controller.Setup(clip, soundProperties);
            controller.StartGrowth();
        }
    }

    public static void Play3dLoopSoundLocal(AudioClip clip, SoundProperties soundProperties, Transform target) => SetupLoopAudio("LoopAudio", clip, soundProperties, target, is3D: true);
    public static void Play2dLoopSoundLocal(AudioClip clip, SoundProperties soundProperties, Transform target) => SetupLoopAudio("LoopAudio2D", clip, soundProperties, target, is3D: false);

    public static void ServerPause3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.Pause());
    public static void ServerContinue3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.UnPause());
    public static void Stop3dLoopSoundLocal(AudioClip clip, Transform target) => DestroyLoopAudio(clip, target);

    public static void ServerPause2dLoopSoundLocal(AudioClip clip, Transform target) => ServerPause3dLoopSoundLocal(clip, target);
    public static void ServerContinue2dLoopSoundLocal(AudioClip clip, Transform target) => ServerContinue3dLoopSoundLocal(clip, target);
    public static void Stop2dLoopSoundLocal(AudioClip clip, Transform target) => Stop3dLoopSoundLocal(clip, target);

    public static void ServerStop3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.Stop());
    public static void ServerStop2dLoopSoundLocal(AudioClip clip, Transform target) => ServerStop3dLoopSoundLocal(clip, target);

    public static void SetLoopSoundPitchLocal(AudioClip clip, Transform target, float newPitch) => ModifyLoopAudio(clip, target, src => src.pitch = newPitch);

    #endregion

    #region Helper
    public AudioClip GetClip(string soundName)
    {
        if (audioCache.TryGetValue(soundName, out AudioClip clip))
        {
            return clip;
        }
        return null;
    }
    #endregion

    #region structs & classes
    [System.Serializable]
    public struct SoundProperties
    {
        public int priority;
        [Range(0, 1)] public float volume;
        [Range(-3, 3)] public float pitch;
        [Range(-1, 1)] public float stereoPan;
        [Range(0, 1)] public float spatialBlend;
        [Range(0, 1.1f)] public float reverbZoneMix;
        [Range(0, 5)] public float dopplerLevel;
        [Range(0, 360)] public float spread;
        public AudioRolloffMode rolloffMode;
        public float minDistance;
        public float maxDistance;
        public bool enableCameraShake;
        public float cameraShakeIntensity;
        public float cameraShakeDuration;

        public static SoundProperties Default => new SoundProperties
        {
            priority = 128,
            volume = 1,
            pitch = 1,
            maxDistance = 500,
            minDistance = 1,
            dopplerLevel = 1,
            rolloffMode = AudioRolloffMode.Linear,
            reverbZoneMix = 1,
            enableCameraShake = false,
            cameraShakeIntensity = 2,
            cameraShakeDuration = 1
        };
    }

    public struct LoopAudio
    {
        public AudioSource audioSource;
        public Transform target;
        public Transform transform;
        public GameObject gameObject;
    }
    #endregion
}