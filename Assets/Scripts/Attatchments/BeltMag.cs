using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltMag : Mag
{
    private const string BulletObjectName = "Bullet";
    private const string FireClipName = "Firing";
    private const string PumpClipName = "Pump";
    private const float MinimumFireAnimationDuration = 0.01f;

    private static readonly int ShootAnimationHash = Animator.StringToHash("Shoot_anim");
    private static readonly int FireSpeedHash = Animator.StringToHash("Fire_speed");

    [Header("Belt Animation")]
    [Tooltip("Meshes ordered from the weapon feed to the free end of the belt. If empty, they are found automatically by name.")]
    [SerializeField] private MeshRenderer[] beltBullets;

    private AnimationClip fireClip;
    private Coroutine fireAnimationRoutine;
    private int fireStateHash;

    [Header("Belt Creation")]
    [SerializeField] private bool autoCreateBullets;
    [SerializeField] private int bulletAmmountDifference;
    [SerializeField] private GameObject bulletPref;
    [SerializeField] private Vector3 bulletOffset;
    [SerializeField] private Rigidbody currentJointRb;
    [SerializeField] private Transform bulletsParent;
    [SerializeField] private Vector3 currentBulletPosition;

    #region Unity Lifecycle
    private void Awake()
    {
        if (autoCreateBullets) InitializeBeltCreation();
    }
    #endregion

    public override void Initialize()
    {
        base.Initialize();
        CacheBeltBullets();
        CacheFireAnimation();
        RefreshBeltVisibility();
    }

    public override void PlayMagShootAnimation()
    {
        PlayBeltFireAnimation();
        HideBulletConsumedByCurrentShot();
    }

    public override void ResetMagState()
    {
        base.ResetMagState();

        if (beltBullets == null) return;

        foreach (MeshRenderer bullet in beltBullets)
        {
            if (bullet != null)
                bullet.enabled = true;
        }
    }
    private void CacheBeltBullets()
    {
        if (beltBullets != null && beltBullets.Length > 0) return;

        MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> bullets = new List<MeshRenderer>(childRenderers.Length);

        foreach (MeshRenderer childRenderer in childRenderers)
        {
            if (childRenderer.gameObject.name.IndexOf(BulletObjectName, StringComparison.OrdinalIgnoreCase) >= 0)
                bullets.Add(childRenderer);
        }

        beltBullets = bullets.ToArray();
    }

    private void HideBulletConsumedByCurrentShot()
    {
        if (beltBullets == null || beltBullets.Length == 0) return;
        if (weaponProperties == null || weaponProperties.reloadValues?.mags == null ||
            weaponProperties.reloadValues.mags.Count == 0) return;

        // Weapon.ExecuteShot chama este metodo antes de subtrair a municao.
        int ammoBeforeShot = weaponProperties.reloadValues.mags[^1];
        if (ammoBeforeShot <= 0 || ammoBeforeShot > beltBullets.Length) return;

        int consumedBulletIndex = ammoBeforeShot - 1;
        if (beltBullets[consumedBulletIndex] != null)
            beltBullets[consumedBulletIndex].enabled = false;
    }

    private void RefreshBeltVisibility()
    {
        if (beltBullets == null || beltBullets.Length == 0) return;

        int currentAmmo = weaponProperties != null && weaponProperties.reloadValues?.mags != null &&
                          weaponProperties.reloadValues.mags.Count > 0
            ? weaponProperties.reloadValues.mags[^1]
            : reloadValues.bulletsPerMag;

        int visibleBulletCount = Mathf.Clamp(currentAmmo, 0, beltBullets.Length);

        for (int i = 0; i < beltBullets.Length; i++)
        {
            if (beltBullets[i] != null)
                beltBullets[i].enabled = i < visibleBulletCount;
        }
    }

    private void CacheFireAnimation()
    {
        fireClip = null;
        fireStateHash = 0;

        if (MagAnimator == null || MagAnimator.runtimeAnimatorController == null) return;

        foreach (AnimationClip clip in MagAnimator.runtimeAnimatorController.animationClips)
        {
            if (!clip.name.Contains(FireClipName) && !clip.name.Contains(PumpClipName)) continue;

            fireClip = clip;
            fireStateHash = Animator.StringToHash(clip.name.Contains(PumpClipName) ? PumpClipName : FireClipName);
            break;
        }
    }

    private void PlayBeltFireAnimation()
    {
        if (MagAnimator == null) return;

        if (fireClip == null)
        {
            MagAnimator.SetTrigger(ShootAnimationHash);
            return;
        }

        bool changeAnimationSpeed = weaponProperties != null && weaponProperties.changeShootAnimationSpeed;
        if (!changeAnimationSpeed && fireAnimationRoutine != null) return;

        CancelFireAnimation();

        float animationDelay = weaponProperties != null ? weaponProperties.delayToShootAnimation : 0f;
        float targetDuration = fireClip.length;
        float speedMultiplier = 1f;

        if (changeAnimationSpeed)
        {
            targetDuration = weaponProperties.firing.interval - animationDelay;
            targetDuration = Mathf.Max(targetDuration, MinimumFireAnimationDuration);
            speedMultiplier = fireClip.length / targetDuration;
        }

        fireAnimationRoutine = StartCoroutine(
            ExecuteFireAnimation(animationDelay, targetDuration, speedMultiplier));
    }

    private IEnumerator ExecuteFireAnimation(
        float animationDelay,
        float targetDuration,
        float speedMultiplier)
    {
        if (animationDelay > 0f)
            yield return new WaitForSeconds(animationDelay);

        if (MagAnimator == null || !isActiveAndEnabled)
        {
            fireAnimationRoutine = null;
            yield break;
        }

        MagAnimator.SetFloat(FireSpeedHash, speedMultiplier);

        if (fireStateHash != 0 && MagAnimator.HasState(0, fireStateHash))
            MagAnimator.Play(fireStateHash, 0, 0f);
        else
            MagAnimator.SetTrigger(ShootAnimationHash);

        yield return new WaitForSeconds(targetDuration);

        fireAnimationRoutine = null;
    }

    private void CancelFireAnimation()
    {
        if (fireAnimationRoutine == null) return;

        StopCoroutine(fireAnimationRoutine);
        fireAnimationRoutine = null;
    }

    private void OnDisable()
    {
        CancelFireAnimation();

        if (MagAnimator != null)
            MagAnimator.SetFloat(FireSpeedHash, 1f);
    }

    #region Belt Bullet Creation
    private void InitializeBeltCreation()
    {
        if (bulletPref == null || bulletsParent == null || currentJointRb == null)
        {
            Debug.LogError("BeltMag possui referências não configuradas.", this);
            return;
        }

        Vector3 startLocalPosition = currentBulletPosition;
        Rigidbody previousBody = currentJointRb;

        Rigidbody[] bodies = new Rigidbody[reloadValues.bulletsPerMag];
        FixedJoint[] joints = new FixedJoint[reloadValues.bulletsPerMag];

        // Primeiro cria e posiciona todos os corpos.
        for (int i = 0; i < reloadValues.bulletsPerMag; i++)
        {
            GameObject bullet = Instantiate(bulletPref, bulletsParent);

            Rigidbody body = bullet.GetComponent<Rigidbody>();
            FixedJoint joint = bullet.GetComponent<FixedJoint>();

            if (body == null || joint == null)
            {
                Debug.LogError(
                    $"{bulletPref.name} precisa de Rigidbody e FixedJoint.",
                    bulletPref);

                Destroy(bullet);
                return;
            }

            Vector3 localPosition =
                startLocalPosition + bulletOffset * (i + 1);

            bullet.transform.localPosition = localPosition;
            bullet.transform.localRotation = bulletPref.transform.localRotation;

            // Sincroniza explicitamente a pose usada pelo PhysX.
            body.position = bullet.transform.position;
            body.rotation = bullet.transform.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            // Valores adequados para um elemento visual muito pequeno.
            body.mass = 0.02f;
            body.useGravity = false;
            body.linearDamping = 6f;
            body.angularDamping = 10f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.solverIterations = 12;
            body.solverVelocityIterations = 4;

            bodies[i] = body;
            joints[i] = joint;
        }

        // Atualiza todos os Transforms antes de criar as restrições.
        Physics.SyncTransforms();

        // Depois monta a corrente na ordem correta.
        for (int i = 0; i < bodies.Length; i++)
        {
            joints[i].connectedBody = previousBody;
            joints[i].enableCollision = false;
            joints[i].enablePreprocessing = true;

            previousBody = bodies[i];
        }
    }
    #endregion
}
