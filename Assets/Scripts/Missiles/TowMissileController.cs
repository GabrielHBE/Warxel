using FishNet.Object;
using UnityEngine;

public class TowMissileController : MissileController
{
    public Transform camera_transform;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeMissilesServer<TowMissile>();
    }

    protected override void Update()
    {
        base.Update();

        if (missiles.Count == 0 && can_reload_missiles)
        {
            // O timer desce na tela de todos os clientes para atualizar o HUD localmente
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {
                // Se eu sou o piloto controlando a arma, eu peço ao servidor para recarregar
                if (is_active)
                {
                    CmdRequestReloadMissiles();
                }
                // Se o veículo estiver vazio (sem dono), o próprio servidor recarrega sozinho
                else if (IsServerInitialized && !Owner.IsValid)
                {
                    ReloadMissilesServer<TowMissile>();
                }

                spawnInterval = original_spawn_interval;
            }
        }

        if (!is_active) return;

        UpdateRocketsHUD();
    }

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode))
        {
            if (only_show_missiles_when_shoot)
            {
                CmdEnableMesh(current_missile.gameObject);
                current_missile.GetComponent<MeshRenderer>().enabled = true;
            }

            shoot_delay = original_shoot_delay;

            // Passamos 'this' (o TowMissileController inteiro) em vez do transform
            current_missile.GetComponent<TowMissile>().Shoot(this);

            MoveToNextMissile();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestReloadMissiles()
    {
        ReloadMissilesServer<TowMissile>();
    }

}
