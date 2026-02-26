using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Jet : Vehicle
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private Transform eject_position;
    [SerializeField] private JetHudManager _hudManager;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private GameObject _mainCannon;
    [SerializeField] private GameObject _trails;
    [SerializeField] private Transform _core;
    [SerializeField] private Transform _shootPosition;
    //[SerializeField] private Image _blackImage;
    [SerializeField] private GameObject _turbineSmoke;
    [SerializeField] private JetProperties _properties;
    [SerializeField] private JetBombsAndMissiles bombsAndMissiles;


    [Header("Sound")]
    [SerializeField] private AudioSource _stopShooting;
    [SerializeField] private AudioSource _tinnitusAudio;
    [SerializeField] private AudioSource _bulletHitAudio;


    #endregion

    #region Public Fields

    [Header("State")]
    [HideInInspector] public bool isNearGround;
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;
    [HideInInspector] public int currentCameraIndex;
    [HideInInspector] public bool retractLandingGear = false;
    [HideInInspector] public float speed;
    [HideInInspector] public bool usingMainCannon = true;
    [HideInInspector] public bool isWheelTouchingGround = true;

    [HideInInspector] public float _overheatAmount;
    [HideInInspector] public float leanValue;

    #endregion

    #region Private Fields

    private float _nextFireTime = 0;

    private bool _isOverheated;
    private float _passoutTimer;
    private bool _isPassingOut;
    private float _diveSpeedModifier;
    private float _afterburnerSpeedModifier;
    private float _totalThrottle;
    private float _exitCooldown;
    private Volume _volume;
    private float _currentGravity = 0;
    private float _downwardComponent;
    private float _gForce;
    private float _maxSpeed = 500;
    private float _mainCannonRotationValue = 0;
    private float _shootDelayTimer = 0;

    #endregion

    #region Unity Lifecycle

    public override void Spawn()
    {
        base.Spawn();
        minFov = video.jet_fov;

        if (bombsAndMissiles.missile != null) _hudManager.SetImages(_properties.hud_icon, bombsAndMissiles.missile.image_hud, countermeasures.image_icon_hud);
        if (bombsAndMissiles.bombs != null) _hudManager.SetImages(_properties.hud_icon, bombsAndMissiles.bombs.image_hud, countermeasures.image_icon_hud);

        rb.mass = _properties.mass;
        SetHpProperties(_properties.hp, _properties.resistance);

        _turbineSmoke.SetActive(false);
        _volume = GetGlobalVolume();
        currentCameraIndex = 1;
        acceleration = _properties.aceleration;
    }

    protected override void FixedUpdate()
    {
        if (!vehicle_destroyed)
        {
            if (is_in_vehicle)
            {
                if (start_engine && !settings.is_menu_settings_active)
                {
                    Move();
                    Rotate();
                }
                ApplyDiveSpeedBoost();
            }

            ApplyGravityModifier();
            rb.AddForce(Physics.gravity * _currentGravity, ForceMode.Acceleration);

            if (speed < _maxSpeed)
            {
                _totalThrottle = throttle + _diveSpeedModifier + _afterburnerSpeedModifier;
                rb.AddForce(forwardReference * _totalThrottle * _properties.max_throttle);
            }
        }
        else
        {
            DestroyAnimation();
        }
    }

    protected override void Update()
    {
        speed = rb.linearVelocity.magnitude;

        if (is_in_vehicle && !settings.is_menu_settings_active)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Damage(100);
            }

            UpdateHUD();
            PilotBehaviour();

        }
        else
        {
            SlowDownEngine();

        }

        UpdateEngineSound();
    }

    protected void OnCollisionStay(Collision collision)
    {
        if (vehicle_destroyed && IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            ContactPoint contact = collision.contacts[0]; // Primeiro ponto de contato
            Vector3 contactPoint = contact.point; // Ponto da colisão
            Vector3 contactNormal = contact.normal; // Normal da colisão

            Explode(contactPoint, contactNormal, collision.gameObject.layer, 12);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (isWheelTouchingGround || collision.gameObject.GetComponent<Missiles>() != null) return;

        if (vehicle_destroyed || hp <= 0)
        {
            if (playerController != null) playerController.Damage(100);
            ExitVehicle();
        }

        if (speed < 100)
        {
            base.OnCollisionEnter(collision);
        }
        else
        {
            if (playerController != null) playerController.Damage(100);
            ExitVehicle();

            HandleCollision(collision, 50);
            Explode(collision.contacts[0].point, transform.localEulerAngles.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }



    }

    #endregion

    #region Engine & Movement

    protected void PilotBehaviour()
    {
        moveForward = Input.GetAxisRaw("Vertical");
        forwardReference = -transform.right;
        _exitCooldown += Time.deltaTime;


        CameraController();
        FreeLook();
        CalculateGForce();

        if (_properties.can_afterburner)
            AfterBurner();

        HandleExitInput();

        if (!vehicle_destroyed)
        {
            Start_Stop_Engine();
            Shoot();
            Switch_weapon();

        }

        if (start_engine)
        {

            if (!vehicle_destroyed) Lean();
            HandlePassout();

            //_properties.interior_turbine.pitch = Math.Clamp(_properties.interior_turbine.pitch, 0.15f, 2);
            _turbineSmoke.SetActive(true);
            UpdateLandingGear();
        }
        else
        {
            _turbineSmoke.SetActive(false);
            SlowDownEngine();
        }
    }

    protected void SlowDownEngine()
    {
        ResetSpeedModifiers();
        //_properties.interior_turbine.pitch = Math.Clamp(_properties.interior_turbine.pitch, 0.01f, 2);
        throttle = Mathf.Lerp(throttle, 0, Time.deltaTime / 2);
    }


    protected override void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (isNearGround && mouseY > 0 && speed > 50)
        {
            rb.AddForce(Vector3.up * rb.mass * 20);
        }

        HandleThrottleControl(deltaTime);
    }

    protected void HandleThrottleControl(float deltaTime)
    {
        if (moveForward > 0 && !_isPassingOut)
        {
            IncreaseThrottle(deltaTime);
        }
        else if (moveForward < 0 && !_isPassingOut)
        {
            DecreaseThrottle(deltaTime);
        }
        else
        {
            Decelerate(deltaTime);
        }
    }

    protected void IncreaseThrottle(float deltaTime)
    {
        //_properties.interior_turbine.pitch = Mathf.MoveTowards( _properties.interior_turbine.pitch, 2f, 0.1f * deltaTime);

        throttle += acceleration * deltaTime;
        throttle = Mathf.Min(throttle, _properties.max_throttle);
    }

    protected void DecreaseThrottle(float deltaTime)
    {
        if (isNearGround)
        {
            if (throttle > -30)
            {
                //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
                throttle -= acceleration * deltaTime * 2;
            }
        }
        else
        {
            if (throttle > 100)
            {
                //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
                throttle -= acceleration * deltaTime;
            }
        }
    }

    protected void Decelerate(float deltaTime)
    {
        if (isNearGround)
        {
            float decelerationRate = acceleration * 0.8f;
            throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);

            //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
        }
        else
        {
            float decelerationRate = acceleration * 0.1f;
            throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);

            //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.005f * deltaTime);
        }

        //_properties.interior_turbine.pitch = Mathf.Clamp(_properties.interior_turbine.pitch, 0.15f, 2f);
    }

    protected void Rotate()
    {
        if (_isPassingOut) return;

        float deltaTime = Time.fixedDeltaTime;

        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * controls.jet_sensibility,
                           -_properties.max_rotation_value, _properties.max_rotation_value);
        mouseY = Math.Clamp(Input.GetAxis("Mouse Y") * controls.jet_sensibility,
                           -_properties.max_pitch_value, _properties.max_pitch_value);

        HandlePitchKeys();

        if (_properties.invertY)
            mouseY *= -1;

        if (Math.Abs(mouseY) > 1 && throttle > 0 && !isNearGround)
        {
            throttle -= Math.Abs(mouseY) * deltaTime * 10;
        }

        UpdateTrails();
        ApplyRotationTorque();
    }

    protected void HandlePitchKeys()
    {
        if (Input.GetKey(keyBinds.JET_pitchUpKey))
            mouseY = _properties.max_pitch_value;
        if (Input.GetKey(keyBinds.JET_pitchDownKey))
            mouseY = -_properties.max_pitch_value;
    }

    protected void UpdateTrails()
    {
        foreach (TrailRenderer trail in _trails.GetComponentsInChildren<TrailRenderer>())
        {
            bool shouldEmit = (mouseX > 10 || mouseX < 10 || mouseY > 10 || mouseY < 10)
                           && (mouseX != 0 || mouseY != 0) && speed > 100;

            if (shouldEmit && !trail.emitting)
            {
                trail.Clear();
                trail.emitting = true;
            }
            else if (!shouldEmit)
            {
                trail.emitting = false;
            }
        }
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(transform.right * mouseX * speed * _properties.rotation_value * 20);
        rb.AddTorque(-transform.forward * mouseY * speed * _properties.pitch_value * 7);
    }

    protected void Lean()
    {
        if (_isPassingOut) return;

        HandleLeanInput();

        float speedFactor = Mathf.Clamp01(speed / _properties.max_throttle);
        float rotationAmount = isNearGround && (throttle >= 20 || throttle < -10) && throttle <= 50
            ? leanValue * Time.deltaTime
            : leanValue * speedFactor * Time.deltaTime;

        if (throttle < 0)
            rotationAmount *= -1;

        rotationAmount = Mathf.Clamp(rotationAmount, -_properties.max_lean_value, _properties.max_lean_value);
        transform.Rotate(Vector3.up * rotationAmount, Space.Self);
    }

    protected void HandleLeanInput()
    {
        if (Input.GetKey(keyBinds.JET_yawLeftKey))
        {
            leanValue -= _properties.lean_value * Time.deltaTime;
        }
        else if (Input.GetKey(keyBinds.JET_yawRightKey))
        {
            leanValue += _properties.lean_value * Time.deltaTime;
        }
        else
        {
            leanValue = Mathf.MoveTowards(leanValue, 0f, 25 * Time.deltaTime);
        }
    }

    #endregion

    #region Speed & Physics Modifiers

    protected void ApplyDiveSpeedBoost()
    {
        Vector3 jetForward = -transform.right;
        _downwardComponent = -jetForward.y;

        if (_downwardComponent > 0.3f) // Down
        {
            ApplyDiveBoost();
        }
        else if (_downwardComponent < -0.3f) // Up
        {
            ApplyClimbPenalty();
        }
        else
        {
            _diveSpeedModifier = Mathf.Lerp(_diveSpeedModifier, 0, 2 * Time.fixedDeltaTime);
        }
    }

    protected void ApplyDiveBoost()
    {
        float gravityBoost = _downwardComponent * Physics.gravity.magnitude * 0.5f;
        float aerodynamicBoost = _downwardComponent * _properties.dive_speed_boost;
        float totalBoost = (gravityBoost + aerodynamicBoost) * Time.fixedDeltaTime;

        totalBoost = Mathf.Clamp(totalBoost, 0, _properties.max_throttle * 1.2f);
        _diveSpeedModifier = totalBoost * 400 * Time.fixedDeltaTime;
    }

    protected void ApplyClimbPenalty()
    {
        float upwardIntensity = -_downwardComponent;
        float airResistance = upwardIntensity * Physics.gravity.magnitude * 0.3f;
        float gravityPenalty = upwardIntensity * _properties.dive_speed_boost * 0.5f;
        float totalPenalty = (airResistance + gravityPenalty) * Time.fixedDeltaTime;

        totalPenalty = Mathf.Clamp(totalPenalty, 0, _properties.max_throttle * 0.7f);
        _diveSpeedModifier = -totalPenalty * 400 * Time.fixedDeltaTime;
    }

    protected void ApplyGravityModifier()
    {
        if (isNearGround)
        {
            _currentGravity = 0;
            return;
        }

        float targetGravity = 1f;

        if (_downwardComponent > 0.3f) // Down
        {
            targetGravity = moveForward > 0
                ? (_properties.max_throttle / (speed * 2)) * -_downwardComponent
                : (_properties.max_throttle / speed) * -_downwardComponent;
        }
        else if (_downwardComponent < -0.3f) // Up
        {
            targetGravity = moveForward > 0
                ? 1.5f * -_downwardComponent
                : (_properties.max_throttle / speed) * -_downwardComponent;
        }
        else
        {
            if (moveForward > 0)
            {
                targetGravity = 0;
            }
            else if (throttle < 100)
            {
                targetGravity = _properties.max_throttle / (speed * 10);
            }
        }

        _currentGravity = Mathf.Lerp(_currentGravity, targetGravity, Time.fixedDeltaTime);
        _currentGravity = Mathf.Clamp(_currentGravity, 0f, 5f);
    }

    protected void CalculateGForce()
    {
        float deltaTime = Time.deltaTime;

        if (mouseY != 0)
        {
            _gForce = (deltaTime / 3) * speed * mouseY;
        }
        else
        {
            _gForce = Mathf.MoveTowards(_gForce, 0f, deltaTime * 5);
        }

        _gForce = Math.Clamp(_gForce, -10, 10);
    }

    #endregion

    #region Combat

    private bool _hasPlayedShootSound = false;

    protected virtual void Shoot()
    {
        float delayToShoot = 0.05f;
        float deltaTime = Time.deltaTime;

        bool isShooting = Input.GetKey(keyBinds.JET_shootVehicleKey);
        bool canShootMain = usingMainCannon && !_isOverheated && isShooting;

        HandleShootDelay(canShootMain, deltaTime);

        bool readyToShoot = canShootMain && _shootDelayTimer >= delayToShoot;

        if (readyToShoot)
        {
            FireMainCannon(deltaTime);
        }
        else
        {
            CoolDownCannon(deltaTime);
            // Reseta a flag quando não está atirando
            if (_hasPlayedShootSound)
            {
                _properties.shoot_sound.Stop();
                _stopShooting.PlayOneShot(_stopShooting.clip);
                _hasPlayedShootSound = false;
            }


        }

        _nextFireTime -= deltaTime;
        HandleSecondaryWeapon();
        RotateMainCannon(deltaTime);
    }

    protected void HandleShootDelay(bool canShoot, float deltaTime)
    {
        if (canShoot)
        {
            _shootDelayTimer += deltaTime;
        }
        else
        {
            _shootDelayTimer = 0;
        }
    }

    protected void FireMainCannon(float deltaTime)
    {
        // Toca o som apenas no primeiro frame
        if (!_hasPlayedShootSound)
        {
            _properties.shoot_sound.PlayOneShot(_properties.shoot_sound.clip);
            _hasPlayedShootSound = true;
        }

        if (_nextFireTime <= 0f)
        {
            Transform bulletObj = Instantiate(
                _properties.bullefPref,
                _shootPosition.position,
                _shootPosition.rotation
            );

            bulletObj.GetComponent<Bullet>().CreateBullet(
                _shootPosition.forward,
                _properties.muzzle_velocity,
                _properties.bullet_drop,
                _properties.damage,
                _properties.damage_dropoff,
                _properties.damage_dropoff_timer,
                _properties.destruction_force,
                _properties.minimum_damage,
                2, 2, 0, true,
                _properties.damage,
                _properties.bullet_hit_effect,
                _bulletHitAudio
            );

            Destroy(bulletObj.gameObject, 10f);
            _nextFireTime = _properties.interval;
        }

        _overheatAmount += deltaTime;
        _mainCannonRotationValue = _properties.fire_rate;

        if (_overheatAmount >= _properties.overheat_time)
            _isOverheated = true;
    }


    protected void CoolDownCannon(float deltaTime)
    {
        _mainCannonRotationValue = Mathf.Lerp(_mainCannonRotationValue, 0f, deltaTime * 3f);
        float coolSpeed = _isOverheated ? (deltaTime / 2f) : deltaTime;
        _overheatAmount = Mathf.MoveTowards(_overheatAmount, 0f, coolSpeed);
        if (_overheatAmount <= 0)
        {
            _isOverheated = false;
        }
    }

    protected void HandleSecondaryWeapon()
    {

        if (!usingMainCannon && bombsAndMissiles != null)
        {

            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.Shoot(keyBinds.JET_shootVehicleKey);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.Shoot(keyBinds.JET_shootVehicleKey);


        }

    }

    protected void RotateMainCannon(float deltaTime)
    {
        _mainCannon.transform.Rotate(Vector3.left * _mainCannonRotationValue * deltaTime);
    }

    protected override void Switch_weapon()
    {

        if (Input.GetKeyDown(keyBinds.VEHICLE_weapon1))
        {
            usingMainCannon = true;

            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.SetActive(false);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.SetActive(false);

        }

        if (Input.GetKeyDown(keyBinds.VEHICLE_weapon2))
        {

            usingMainCannon = false;
            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.SetActive(true);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.SetActive(true);
        }

    }

    #endregion

    #region Player Interaction

    public override void EnterVehicle(GameObject _player)
    {
        base.EnterVehicle(_player);
        _exitCooldown = 0;

        player.transform.SetParent(_playerPosition);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;


        _turbineSmoke.SetActive(start_engine);
    }

    protected void HandleExitInput()
    {
        if (Input.GetKeyDown(keyBinds.PLAYER_interactKey) && _exitCooldown > 0.1f)
        {
            _turbineSmoke.SetActive(false);

            if (throttle > 10)
            {
                EjectPlayer();
            }
            else
            {
                ExitVehicle();
            }
        }
    }

    protected void EjectPlayer()
    {

        if (!is_in_vehicle) return;

        if (!player.activeSelf) player.SetActive(true);

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(false);

        if (playerProperties != null)
        {
            playerProperties.is_in_vehicle = false;
            playerProperties = null;
        }

        if (playerController != null)
        {

            playerController.HideOwnerItems(true);
            playerController = null;
        }

        if (player != null)
        {
            player.transform.position = eject_position.position;
            player.transform.SetParent(null);
            player = null;
        }


        if (player_rb != null)
        {

            player_rb.isKinematic = false;
            player_rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 ejectDirection = transform.up;

            player_rb.AddForce(ejectDirection * 2 * player_rb.mass, ForceMode.Impulse);
            player_rb.AddForce(forwardReference * player_rb.mass * speed, ForceMode.Impulse);

        }


        is_in_vehicle = false;

    }

    #endregion

    #region Systems

    protected void AfterBurner()
    {
        float maxAfterburnerSpeed = 100;

        if (Input.GetKey(keyBinds.JET_boostKey) && moveForward > 0)
        {
            _afterburnerSpeedModifier += Time.deltaTime * 50;
        }
        else
        {
            _afterburnerSpeedModifier -= Time.deltaTime * 50;
        }

        _afterburnerSpeedModifier = Math.Clamp(_afterburnerSpeedModifier, 0, maxAfterburnerSpeed);
    }

    protected void FreeLook()
    {
        if (Input.GetKey(keyBinds.VEHICLE_freeLookKey))
        {
            float mouseYFreeLook = Input.GetAxis("Mouse Y") * -controls.helicopter_sensibility;
            float mouseXFreeLook = Input.GetAxis("Mouse X") * controls.helicopter_sensibility;

            Vector3 currentEuler = vehicle_camera.transform.localEulerAngles;

            float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
            float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

            currentX += mouseYFreeLook;
            currentY += mouseXFreeLook;

            currentX = Mathf.Clamp(currentX, -80f, 20f);
            currentY = Mathf.Clamp(currentY, -90f, 90f);

            vehicle_camera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
        else
        {
            vehicle_camera.transform.localRotation = Quaternion.Lerp(
                vehicle_camera.transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * 3
            );
        }
    }

    protected void Zoom()
    {
        if (!vehicle_camera.enabled) return;

        if (Input.GetKey(keyBinds.HELICOPTER_zoom_key))
        {

            float targetFov = minFov / _properties.zoom;
            vehicle_camera.fieldOfView = Mathf.Lerp(vehicle_camera.fieldOfView, targetFov, 4 * Time.deltaTime);

        }
        else
        {
            vehicle_camera.fieldOfView = Mathf.Lerp(
                vehicle_camera.fieldOfView,
                minFov,
                4 * Time.deltaTime);

        }
    }

    protected void UpdateLandingGear()
    {
        retractLandingGear = !Physics.Raycast(_core.position, Vector3.down, 10,
            LayerMask.GetMask("Ground") | LayerMask.GetMask("Voxel"));
    }

    protected void ResetSpeedModifiers()
    {
        _diveSpeedModifier = 0;
        _afterburnerSpeedModifier = 0;
    }

    #endregion

    #region Visual & Audio


    protected override void Start_Stop_Engine()
    {
        if (Input.GetKeyDown(keyBinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;

            if (start_engine)
            {
                _properties.interior_turbine.Play();
                //StartCoroutine(IncreasePitch(_properties.interior_turbine, 2));
            }
            else
            {
                //StartCoroutine(DecreasePitch(_properties.interior_turbine, 2));
            }
        }
    }

    private bool _wasEnginePlaying = false;

    private void UpdateEngineSound()
    {
        if (vehicle_destroyed) return;

        // Mapeia o throttle (0 a _properties.max_throttle) para o pitch (0.01 a 2)
        float t = throttle / _properties.max_throttle;

        float targetPitch;

        if (start_engine)
        {
            targetPitch = Mathf.Lerp(0.4f, 2f, t);
        }
        else
        {
            targetPitch = Mathf.Lerp(0.01f, 2f, t);
        }

        // Suaviza a transição do pitch
        _properties.interior_turbine.pitch = Mathf.Lerp(
            _properties.interior_turbine.pitch,
            targetPitch,
            Time.deltaTime * 2
        );

        // Verifica se deve parar o som
        bool shouldBePlaying = _properties.interior_turbine.pitch > 0.01f;

        // Só chama Stop() se estava tocando e agora não deve mais tocar
        if (_wasEnginePlaying && !shouldBePlaying)
        {
            _properties.interior_turbine.Stop();
        }
        // Só chama Play() se não estava tocando e agora deve tocar
        else if (!_wasEnginePlaying && shouldBePlaying)
        {
            _properties.interior_turbine.Play();
        }

        _wasEnginePlaying = shouldBePlaying;
    }

    /*

    IEnumerator IncreasePitch(AudioSource audio, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audio.pitch += 0.0001f;
            yield return null;
        }
    }

    IEnumerator DecreasePitch(AudioSource audio, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audio.pitch -= 0.0001f;
            yield return null;
        }

        audio.Stop();
    }
    */

    #endregion

    #region HUD & UI

    protected override void UpdateHUD()
    {
        _hudManager.UpdateSpeed(speed);
        _hudManager.UpdateAltitude(transform.position.y / 3);
        _hudManager.UpdateGravity(_currentGravity);
        _hudManager.UpdateGforce(_gForce);
        _hudManager.UpdateHeat(_overheatAmount);
        _hudManager.ChangeHeatIndicatorActive(usingMainCannon);
        if (countermeasures != null)
        {
            if (countermeasures.is_active)
            {
                _hudManager.UpdateCountermeasuresStatus("Active");
            }
            else if (!countermeasures.is_active && countermeasures.is_reloading)
            {
                _hudManager.UpdateCountermeasuresStatus("Reloading... [" + countermeasures.reload_countermeasures_duration.ToString("F0") + "]");
            }
            else if (!countermeasures.is_active && !countermeasures.is_reloading)
            {
                _hudManager.UpdateCountermeasuresStatus("Ready");
            }
        }
    }

    #endregion

    #region Helper Methods

    private Volume GetGlobalVolume()
    {
        GameObject globalVolumeObj = GameObject.FindGameObjectWithTag("GlobalVolume");
        if (globalVolumeObj != null)
        {
            return globalVolumeObj.GetComponent<Volume>();
        }
        return null;
    }

    private void HandlePassout()
    {
        // Implementação comentada para revisão futura
    }

    protected override void CameraController()
    {
        Zoom();
        // Implementação de troca de câmera comentada
    }


    bool DestroyAnimation_do_once = true;
    float deltaTime = 0;
    protected override void DestroyAnimation()
    {
        deltaTime += Time.fixedDeltaTime;
        if (deltaTime >= 10)
        {
            Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }
        if (DestroyAnimation_do_once)
        {
            fire_effects_parent.SetActive(true);
            DestroyAnimation_do_once = false;
        }

        rb.AddForce(forwardReference * _totalThrottle * _properties.max_throttle);
        rb.AddTorque(transform.right * 400 * rb.mass);
    }

    #endregion
}