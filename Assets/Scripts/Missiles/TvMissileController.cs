using FishNet.Object;
using UnityEngine;

public class TvMissileController : MissileController
{
    public Camera original_camera;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeMissilesServer<TvMissile>();
        /*
        // Garante que a câmera seja aplicada a todos os mísseis já existentes
        if (original_camera != null)
        {
            foreach (var missile in missiles)
            {
                var tvMissile = missile.GetComponent<TvMissile>();
                if (tvMissile != null)
                {
                    // Apenas armazena a referência, não ativa ainda
                }
            }
        }

        current_missile = missiles[current_missile_index];
        */
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
                    ReloadMissilesServer<TvMissile>();
                }

                spawnInterval = original_spawn_interval;
            }
        }


        if (!is_active) return;

        UpdateRocketsHUD();

    }

    TvMissile tvMissile;

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode) && tvMissile == null)
        {
            if (only_show_missiles_when_shoot)
                current_missile.GetComponent<MeshRenderer>().enabled = true;

            // Desativa a câmera original
            if (original_camera != null)
                original_camera.enabled = false;

            shoot_delay = original_shoot_delay;

            // Passa a referência da câmera original para o míssil
            tvMissile = current_missile.GetComponent<TvMissile>();
            if (tvMissile != null)
            {
                tvMissile.Shoot(original_camera);
            }

            MoveToNextMissile();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestReloadMissiles()
    {
        ReloadMissilesServer<TvMissile>();
    }

}