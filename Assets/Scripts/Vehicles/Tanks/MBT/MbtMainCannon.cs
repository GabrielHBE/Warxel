using System.Collections;
using FishNet.Object;
using UnityEngine;

public class MbtMainCannon : NetworkBehaviour, IVehicleArmory
{   
    [SerializeField] private Tank tank;

    [Header("Main Shell Settings")]
    [SerializeField] private TankMainShell tankMainShell;

    [Header("Effects & Audio")]
    [SerializeField] private ParticleSystem shoot_main_cannon_explosion;
    [SerializeField] private AudioSource main_cannon_sound;

    [Header("Positions & Transforms")]
    [SerializeField] private Transform cannonShootPos;
    [SerializeField] private Transform retactableTankCannon;
    [SerializeField] private GameObject turret;



    private float cannon_shoot_delay = 0;

    private void Start()
    {
        if (tankMainShell != null)
            cannon_shoot_delay = tankMainShell.reload_time;

    }

    public void ActivateArmory(){ }
    

    public void DeactivateArmory(){ }

    public Sprite GetArmoryIcon() => tankMainShell.image_hud;

    public string GetCurrentAmmo()
    {
        if(cannon_shoot_delay == tankMainShell.reload_time) return "Ready";

        return cannon_shoot_delay.ToString("F1");
    }

    public float GetHeatingLevel() => 0;

    public float GetMaxOverheat() => 0;

    public void Shoot()
    {
        if (cannon_shoot_delay == tankMainShell.reload_time && InputManager.GetKeyDown(Settings.Instance._keybinds.TANK_shoot_key))
        {
            if (main_cannon_sound != null) main_cannon_sound.Play();

            if (shoot_main_cannon_explosion != null)
                shoot_main_cannon_explosion.Play();

            ApplyCannonRecoil();
            StartCoroutine(ReloadMainCannon());

            // Envia o comando para o Servidor
            CmdShootMainCannon(cannonShootPos.position, cannonShootPos.rotation, cannonShootPos.forward);
        }
    }

    [ServerRpc]
    private void CmdShootMainCannon(Vector3 position, Quaternion rotation, Vector3 direction)
    {
        GameObject current_shell = Instantiate(tankMainShell.gameObject, position, rotation);

        if (current_shell.GetComponent<NetworkObject>() != null)
            Spawn(current_shell, Owner);

        // Passa o vehicleGameObject para que a bala saiba quem causou o disparo
        current_shell.GetComponent<TankMainShell>().Shoot(direction, tank.gameObject != null ? tank.gameObject : gameObject);

        RpcShootMainCannonEffects();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcShootMainCannonEffects()
    {
        if (main_cannon_sound != null) main_cannon_sound.Play();

        if (shoot_main_cannon_explosion != null)
        {
            shoot_main_cannon_explosion.Play();
        }
    }

    private void ApplyCannonRecoil()
    {
        if (turret == null || tankMainShell == null) return;

        float currentCannonRotation = turret.transform.eulerAngles.y;

        if (currentCannonRotation > 180f)
            currentCannonRotation -= 360f;

        float rotationDirection = Mathf.Sign(currentCannonRotation);
        float recoilForce = tankMainShell.recoil_force * rotationDirection;

        Vector3 recoilTorque = turret.transform.right * (-rotationDirection * recoilForce);

        tank.rb.AddTorque(recoilTorque * (tank.rb.mass / 2), ForceMode.Impulse);
    }

    private IEnumerator ReloadMainCannon()
    {
        float reloadTime = tankMainShell.reload_time;
        cannon_shoot_delay = reloadTime;

        Vector3 originalLocalPosition = retactableTankCannon.localPosition;

        Vector3 localRecoilDirection = -Vector3.forward; 
        Vector3 recoilPosition = originalLocalPosition + (localRecoilDirection * 5);

        float recoilDuration = reloadTime * 0.05f;
        float recoilTimer = 0f;

        while (recoilTimer < recoilDuration)
        {
            recoilTimer += Time.deltaTime;
            float t = recoilTimer / recoilDuration;
            retactableTankCannon.localPosition = Vector3.Lerp(originalLocalPosition, recoilPosition, t);
            cannon_shoot_delay = reloadTime - recoilTimer;
            yield return null;
        }

        float holdDuration = reloadTime * 0.1f;
        yield return new WaitForSeconds(holdDuration);
        cannon_shoot_delay = reloadTime - recoilDuration - holdDuration;

        float returnDuration = reloadTime * 0.85f;
        float returnTimer = 0f;

        while (returnTimer < returnDuration)
        {
            returnTimer += Time.deltaTime;
            float t = returnTimer / returnDuration;
            retactableTankCannon.localPosition = Vector3.Lerp(recoilPosition, originalLocalPosition, t);
            cannon_shoot_delay = reloadTime - recoilDuration - holdDuration - returnTimer;
            yield return null;
        }

        retactableTankCannon.localPosition = originalLocalPosition;
        cannon_shoot_delay = reloadTime;
    }
}