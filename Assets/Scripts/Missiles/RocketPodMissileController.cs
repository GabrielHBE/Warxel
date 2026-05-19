using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class RocketPodController : MissileController
{

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        //CmdRequestReloadMissiles();
        //StartCoroutine(StartMissileForOwnerDelay());
        if(IsOwner) CmdReuestInitializeMissiles();
    }

    protected override void Update()
    {
        if (!IsSpawned) return;
        
        if(!IsOwner) return;
        
        base.Update();

        if (missiles.Count == 0 && can_reload_missiles)
        {
            // O timer desce na tela de todos os clientes para atualizar o HUD localmente
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {

                CmdRequestReloadMissiles();

                spawnInterval = original_spawn_interval;
            }
        }

    }

    public override void ShootMissile()
    {
        if (CanShoot())
        {
            if (only_show_missiles_when_shoot)
            {
                CmdEnableMesh(current_missile.gameObject);
                current_missile.mesh.enabled = true;
            }

            shoot_delay = original_shoot_delay;

            // Aqui o cliente chama o [ServerRpc] no míssil. 
            current_missile.Shoot(transform.forward);
            MoveToNextMissile();
        }
    }

    // Novo RPC para o Cliente pedir a recarga
    [ServerRpc(RequireOwnership = true)]
    private void CmdRequestReloadMissiles()
    {
        ReloadMissilesServer<RocketPodsMissile>();
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdReuestInitializeMissiles()
    {
        InitializeMissilesServer<RocketPodsMissile>();
    }

}