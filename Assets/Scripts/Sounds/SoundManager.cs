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
    [SerializeField] private AudioDistanceController audioDistanceControllerPrefab;

    [Header("Configurações de Áudio (Atualizado Automatically)")]
    [SerializeField] private List<AudioClip> internalAudioList = new List<AudioClip>();
    [SerializeField] private string rootFolder = "Assets/Sounds";

    private readonly Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    private static readonly List<LoopAudio> loopAudioList = new List<LoopAudio>();
    private static AudioDistanceController staticAudioDistanceController;

    void Start()
    {
        staticAudioDistanceController = audioDistanceControllerPrefab;
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
                if (audio.gameObject != null) Destroy(audio.gameObject);
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
        AudioSource audioSource = go.AddComponent<AudioSource>();
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
        // 1. Verificação de Prevenção de Duplicatas
        foreach (var existingLoop in loopAudioList)
        {
            // Se já existe um som com o mesmo clip anexado ao mesmo target
            if (existingLoop.target == target && existingLoop.audioSource != null && existingLoop.audioSource.clip == clip)
            {
                // Apenas garanta que ele está tocando e aborte a criação de um novo
                if (!existingLoop.audioSource.isPlaying)
                {
                    existingLoop.audioSource.Play();
                }
                return;
            }
        }

        // 2. Criação Padrão (caso não exista)
        GameObject loopObject = new GameObject($"{namePrefix}_{clip.name}");
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
                if (loopAudio.gameObject != null) Destroy(loopAudio.gameObject);
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
            AudioDistanceController controller = Instantiate(audioDistanceControllerPrefab, position, Quaternion.identity);
            controller.Setup(clip, soundProperties);
            controller.StartGrowth();
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

    // ================= PITCH (NOVO) =================
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
        if (audioCache.TryGetValue(soundName, out AudioClip clip))
            Play2dSoundLocal(clip, soundProperties);
        else
            Debug.LogWarning($"Som '{soundName}' não foi encontrado no AudioManager!");
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

    // ================= PITCH 2D LOOP (NOVO) =================
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
        GameObject tempGO = new GameObject($"TempAudio2D_{clip.name}");
        AudioSource tempSource = CreateConfiguredAudioSource(tempGO, clip, soundProperties, is3D: false, loop: false);
        tempSource.PlayOneShot(clip);
        Destroy(tempGO, clip.length);
    }

    public static void Play3dSoundLocal(AudioClip clip, SoundProperties soundProperties, Vector3 position)
    {
        AudioDistanceController controller = Instantiate(staticAudioDistanceController, position, Quaternion.identity);
        controller.Setup(clip, soundProperties);
        controller.StartGrowth();
    }

    public static void Play3dLoopSoundLocal(AudioClip clip, SoundProperties soundProperties, Transform target)
        => SetupLoopAudio("LoopAudio", clip, soundProperties, target, is3D: true);

    public static void Play2dLoopSoundLocal(AudioClip clip, SoundProperties soundProperties, Transform target)
        => SetupLoopAudio("LoopAudio2D", clip, soundProperties, target, is3D: false);

    public static void ServerPause3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.Pause());
    public static void ServerContinue3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.UnPause());
    public static void Stop3dLoopSoundLocal(AudioClip clip, Transform target) => DestroyLoopAudio(clip, target);

    public static void ServerPause2dLoopSoundLocal(AudioClip clip, Transform target) => ServerPause3dLoopSoundLocal(clip, target);
    public static void ServerContinue2dLoopSoundLocal(AudioClip clip, Transform target) => ServerContinue3dLoopSoundLocal(clip, target);
    public static void Stop2dLoopSoundLocal(AudioClip clip, Transform target) => Stop3dLoopSoundLocal(clip, target);

    public static void ServerStop3dLoopSoundLocal(AudioClip clip, Transform target) => ModifyLoopAudio(clip, target, src => src.Stop());
    public static void ServerStop2dLoopSoundLocal(AudioClip clip, Transform target) => ServerStop3dLoopSoundLocal(clip, target);

    // Método Estático Local para Alterar o Pitch (NOVO)
    public static void SetLoopSoundPitchLocal(AudioClip clip, Transform target, float newPitch) => ModifyLoopAudio(clip, target, src => src.pitch = newPitch);
    #endregion

    #region structs & classes
    [System.Serializable]
    public struct SoundProperties
    {
        [Tooltip("Sound priority: lower number = higher priority. Higher priority sounds are favored when voice limits are reached (same as AudioSource.priority - range: 0-256)")]
        public int priority;

        [Tooltip("Sound volume: 0 = silence, 1 = maximum volume (same as AudioSource.volume)")]
        [Range(0, 1)] public float volume;

        [Tooltip("Sound pitch: negative values = lower tone, positive = higher tone. 1 = original pitch (same as AudioSource.pitch)")]
        [Range(-3, 3)] public float pitch;

        [Tooltip("Stereo pan: -1 = full left, 0 = center, 1 = full right (same as AudioSource.panStereo)")]
        [Range(-1, 1)] public float stereoPan;

        [Tooltip("Blend between 2D and 3D sound: 0 = completely 2D (no spatial effects), 1 = completely 3D (affected by distance and position) (same as AudioSource.spatialBlend)")]
        [Range(0, 1)] public float spatialBlend;

        [Tooltip("Influence of reverb zones: 0 = no reverb, 1 = full reverb (same as AudioSource.reverbZoneMix)")]
        [Range(0, 1.1f)] public float reverbZoneMix;

        [Tooltip("Doppler effect level: 0 = no Doppler, 5 = maximum (same as AudioSource.dopplerLevel)")]
        [Range(0, 5)] public float dopplerLevel;

        [Tooltip("Sound spread angle in degrees: 0 = point sound, 360 = omnidirectional sound (same as AudioSource.spread)")]
        [Range(0, 360)] public float spread;

        [Tooltip("Sound attenuation mode with distance: Logarithmic (most realistic), Linear, or Custom (same as AudioSource.rolloffMode)")]
        public AudioRolloffMode rolloffMode;

        [Tooltip("Minimum distance where sound begins to fade. Before this, volume is at maximum (same as AudioSource.minDistance)")]
        public float minDistance;

        [Tooltip("Maximum distance where sound can be heard. Beyond this, volume is zero (same as AudioSource.maxDistance)")]
        public float maxDistance;

        [Tooltip("If enabled, the camera will shake when this sound is played (extension of AudioSource)")]
        public bool enableCameraShake;

        [Tooltip("Intensity of the camera shake. Higher values = stronger shake (extension of AudioSource)")]
        public float cameraShakeIntensity;

        [Tooltip("Duration of the camera shake in seconds (extension of AudioSource)")]
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