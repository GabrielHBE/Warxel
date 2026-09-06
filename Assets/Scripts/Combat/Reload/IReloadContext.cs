using ProcessReload;

public interface IReloadContext
{
    // Dados obrigatórios
    Reload.ReloadValues ReloadValues { get; }
    
    // Estados atuais
    bool IsActive { get; }
    bool IsFiring { get; }
    bool IsRolling { get; }
    
    // Getters e Setters de controle
    bool IsReloading { get; set; }
    bool HasFireClip { get; }
    bool IsInFireAnimation { get; }

    // Ações que o controlador pode invocar no objeto
    void SetCanShoot(bool canShoot);
    void StartReloadAnimation();
    void FinishReloadAnimation();
    void ResetFiringState();
    void OnReloadFailedNoAmmo(); // Para emitir alertas genéricos
}