using System.Collections;
using ProcessReload;
using UnityEngine;

[System.Serializable]
public class VehicleArmoryProperties
{
    public enum ShootingSoundMode
    {
        PerBullet,
        StartLoopEnd
    }

    [Header("UI")]
    public Sprite hudIcon;

    [Header("Sounds")]
    [Tooltip("PerBullet plays shootSound for each projectile. StartLoopEnd plays start, then a loop, then end when firing stops.")]
    public ShootingSoundMode shootingSoundMode = ShootingSoundMode.PerBullet;
    public SoundManager.SoundComponents shootSound;
    public SoundManager.SoundComponents startShootSound;
    public SoundManager.SoundComponents shootLoopSound;
    public SoundManager.SoundComponents endShootSound;

    [Header("Bullet Prefabs")]
    public DummyProjectile dummyBullet;
    public GameObject bulletPref;

    [Header("Heat Settings")]
    public bool useHeatValues;
    public Heating.HeatValues heatValues;

    [Header("Reload Values")]
    public bool useReloadValues;
    public Reload.ReloadValues reloadValues;

    [Header("Fring Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    [Header("Recoil Settings")]
    public Recoil.RecoilValues recoilValues;
    [Tooltip("Child transform that receives visual position and rotation recoil.")]
    public Transform visualRecoilTransform;

    public void Awake()
    {
        recoilValues.CalculateRecoilSpeed(firing.interval);

    }
}

// Keeps the audio sequence independent from the projectile firing interval.
public class VehicleArmoryFireAudio
{
    private readonly VehicleArmoryProperties properties;
    private readonly Transform target;
    private bool isFiring;
    private bool isLoopPlaying;
    private float loopStartTime;

    public VehicleArmoryFireAudio(VehicleArmoryProperties properties, Transform target)
    {
        this.properties = properties;
        this.target = target;
    }

    public void OnShot()
    {
        if (properties.shootingSoundMode == VehicleArmoryProperties.ShootingSoundMode.PerBullet)
            PlayOneShot(properties.shootSound);
    }

    public void Update(bool shouldBeFiring)
    {
        if (properties.shootingSoundMode != VehicleArmoryProperties.ShootingSoundMode.StartLoopEnd) return;

        if (!shouldBeFiring)
        {
            Stop();
            return;
        }

        if (!isFiring)
        {
            isFiring = true;
            PlayOneShot(properties.startShootSound);

            var start = properties.startShootSound;
            float duration = start != null && start.clip != null
                ? start.clip.length / Mathf.Max(Mathf.Abs(start.properties.pitch), 0.01f)
                : 0f;
            loopStartTime = Time.unscaledTime + duration;
        }

        if (!isLoopPlaying && Time.unscaledTime >= loopStartTime)
        {
            var loop = properties.shootLoopSound;
            if (loop != null && loop.clip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.RequestPlay3dLoopSound(loop.clip.name, loop.properties, target, false);
                SoundManager.Play2dLoopSoundLocal(loop.clip, loop.properties, target);
                isLoopPlaying = true;
            }
        }
    }

    public void Stop()
    {
        if (!isFiring) return;

        isFiring = false;
        if (isLoopPlaying)
        {
            var loop = properties.shootLoopSound;
            if (loop != null && loop.clip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.RequestStop3dLoopSound(loop.clip.name, target);
                SoundManager.Stop2dLoopSoundLocal(loop.clip, target);
            }
            isLoopPlaying = false;
        }

        PlayOneShot(properties.endShootSound);
    }

    private void PlayOneShot(SoundManager.SoundComponents sound)
    {
        if (sound == null || sound.clip == null || SoundManager.Instance == null) return;

        SoundManager.Instance.RequestPlay3dSound(sound.clip.name, sound.properties, target.position, false);
        SoundManager.Play2dSoundLocal(sound.clip, sound.properties);
    }
}

public class VehicleArmoryVisualRecoil
{
    private readonly MonoBehaviour host;
    private readonly Transform target;
    private readonly Recoil.RecoilValues values;
    private readonly Vector3 initialLocalPosition;
    private readonly Quaternion initialLocalRotation;
    private Vector3 positionOffset;
    private Quaternion rotationOffset = Quaternion.identity;
    private Coroutine coroutine;
    private bool hasApplied;

    public VehicleArmoryVisualRecoil(MonoBehaviour host, VehicleArmoryProperties properties)
    {
        this.host = host;
        target = properties.visualRecoilTransform;
        values = properties.recoilValues;

        if (target == null) return;
        initialLocalPosition = target.localPosition;
        initialLocalRotation = target.localRotation;
    }

    public void Play()
    {
        if (target == null || values == null) return;

        Vector3 targetPositionOffset = positionOffset + Recoil.CalculateVisualRecoilOffset(values.visualPositionRecoil, false);
        Vector3 maxRotation = values.maxRotationRecoil;
        float maxX = Mathf.Abs(maxRotation.x);
        Vector3 recoilRotation = new Vector3(
            Random.Range(-maxX, maxX),
            Random.Range(-maxRotation.y, maxRotation.y),
            Random.Range(-maxRotation.z, maxRotation.z));
        Quaternion targetRotationOffset = rotationOffset * Quaternion.Euler(recoilRotation);

        if (coroutine != null) host.StopCoroutine(coroutine);
        coroutine = host.StartCoroutine(Animate(positionOffset, targetPositionOffset, rotationOffset, targetRotationOffset));
    }

    private IEnumerator Animate(Vector3 startPosition, Vector3 targetPosition, Quaternion startRotation, Quaternion targetRotation)
    {
        // Keep both phases visible even when the firing interval is shorter than a few frames.
        float applyDuration = Mathf.Max(values.applyRecoilSpeed, 0.06f);
        float resetDuration = Mathf.Max(values.resetRecoilSpeed, 0.08f);

        // Slow-firing weapons still need a quick kick and recovery.
        // Keep explicitly configured manual durations unchanged by these caps.
        if (!values.manualCalculateRecoil)
        {
            applyDuration = Mathf.Min(applyDuration, 0.1f);
            resetDuration = Mathf.Min(resetDuration, 0.2f);
        }

        yield return Recoil.ApplyVisualRecoilAnimation(
            startPosition,
            targetPosition,
            startRotation,
            targetRotation,
            applyDuration,
            resetDuration,
            values.applyCurve,
            values.resetCurve,
            ApplyOffset);

        coroutine = null;
    }

    private void ApplyOffset(Vector3 newPositionOffset, Quaternion newRotationOffset)
    {
        if (target == null) return;

        positionOffset = newPositionOffset;
        rotationOffset = newRotationOffset;
        target.localPosition = initialLocalPosition + positionOffset;
        target.localRotation = initialLocalRotation * rotationOffset;
        hasApplied = true;
    }

    public void Reset()
    {
        if (coroutine != null)
        {
            host.StopCoroutine(coroutine);
            coroutine = null;
        }

        if (!hasApplied) return;
        ApplyOffset(Vector3.zero, Quaternion.identity);
        hasApplied = false;
    }
}
