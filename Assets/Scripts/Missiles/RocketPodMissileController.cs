using FishNet.Object;
using UnityEngine;

public class RocketPodController : MissileController
{
    // Roda apenas quando o objeto é iniciado no Servidor
    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeMissilesServer<RocketPodsMissile>();
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
                    ReloadMissilesServer<RocketPodsMissile>();
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
            
            // Aqui o cliente chama o [ServerRpc] no míssil. 
            current_missile.Shoot(); 
            MoveToNextMissile();
        }
    }

    // Novo RPC para o Cliente pedir a recarga
    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestReloadMissiles()
    {
        ReloadMissilesServer<RocketPodsMissile>();
    }
    
}