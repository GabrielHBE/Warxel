using System.Collections;
using TMPro;
using UnityEngine;

public class BattleCoinsUI : PersistentLocalSingleton<BattleCoinsUI>
{
    [SerializeField] private TextMeshProUGUI currentBattleCoinsText;
    [SerializeField] private TextMeshProUGUI updateBattleCoinsText;

    private Vector3 updateBattleCoinsInitialPos;
    private Vector3 updateBattleCoinsDestiny;

    private const int UPDATE_BATTLE_COINS_ANIMATION_OFFSET_Y = 35;

    protected override void Awake()
    {
        base.Awake();
        updateBattleCoinsInitialPos = updateBattleCoinsText.transform.localPosition;
        updateBattleCoinsText.text = "";

        updateBattleCoinsDestiny = new Vector3(updateBattleCoinsInitialPos.x, updateBattleCoinsInitialPos.y + UPDATE_BATTLE_COINS_ANIMATION_OFFSET_Y, updateBattleCoinsInitialPos.z);
        StartCoroutine(InitializeBattleCoinsDelay());
    }

    private IEnumerator InitializeBattleCoinsDelay()
    {
        while (AccountManager.Instance == null) yield return null;

        UpdateCurrentBattleCoins(AccountManager.Instance.battleCoins, AccountManager.Instance.battleCoins);
    }

    public void UpdateCurrentBattleCoins(int next, int qnt = 0)
    {
        currentBattleCoinsText.text = "$ " + next.ToString();
        AnimateUpdateBattleCoinsText(qnt);
    }

    private void AnimateUpdateBattleCoinsText(int difference)
    {
        // Cancela animações ativas no objeto para não encavalar caso a função seja chamada rapidamente
        LeanTween.cancel(updateBattleCoinsText.gameObject);

        // Opcional: Adiciona um sinal de "+" se for um ganho de moedas
        updateBattleCoinsText.text = difference > 0 ? $"+{difference}" : difference.ToString();

        // 1. Reseta a posição inicial e o alpha antes de começar a animação
        updateBattleCoinsText.transform.localPosition = updateBattleCoinsInitialPos;
        updateBattleCoinsText.alpha = 0f;

        float alphaDuration = 0.5f; // Duração para o alpha chegar a 100%
        float moveDuration = 0.8f;  // Duração do movimento até o destino

        // 2. Anima o Alpha de 0 para 1 (100%)
        LeanTween.value(updateBattleCoinsText.gameObject, 0f, 1f, alphaDuration)
            .setOnUpdate((float alphaVal) =>
            {
                // Atualiza o alpha do TextMeshPro a cada frame da animação
                updateBattleCoinsText.alpha = alphaVal;
            })
            .setOnComplete(() =>
            {
                // 3. Quando o Alpha atinge 100%, move para o destino com EaseInOut
                LeanTween.moveLocal(updateBattleCoinsText.gameObject, updateBattleCoinsDestiny, moveDuration)
                    .setEase(LeanTweenType.easeInOutQuad); // Ease In and Out
            });
    }
}