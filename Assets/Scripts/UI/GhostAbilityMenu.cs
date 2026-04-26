using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class GhostAbilityMenu : Menu
{
    [SerializeField] private GridLayoutGroup _abilitiesGrid;
    [SerializeField] private GameObject _abilityCardPrefab;

    private GhostAbilityManager _ghostAbilityManager;

    private CursorLockMode _cursorLockStateBeforeActivation;

    private readonly List<GameObject> _spawnedAbilityCards = new();

    protected override void OnActivate()
    {
        var abilities = NetworkClient.localPlayer.GetComponents<GhostAbility>();

        foreach (var ability in abilities)
        {
            var abilityCardObj = Instantiate(_abilityCardPrefab, _abilitiesGrid.transform);
            _spawnedAbilityCards.Add(abilityCardObj);

            var abilityCard = abilityCardObj.GetComponent<GhostAbilityCard>();
            abilityCard.Initialize(ability);
            abilityCard.OnActivated.AddListener(DeactivateViaGhostAbilityManager);
        }

        _cursorLockStateBeforeActivation = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
    }

    protected override void OnDeactivate()
    {
        Cursor.lockState = _cursorLockStateBeforeActivation is not CursorLockMode.Locked
            ? _cursorLockStateBeforeActivation
            : CursorLockMode.Locked;

        foreach (var card in _spawnedAbilityCards)
            Destroy(card);

        _spawnedAbilityCards.Clear();
    }

    public void ActivateFrom(GhostAbilityManager ghostAbilityManager)
    {
        _ghostAbilityManager = ghostAbilityManager;
        Activate();
    }

    public void DeactivateViaGhostAbilityManager()
    {
        if (_ghostAbilityManager != null)
            _ghostAbilityManager.CloseAbilityMenu();
        else
            Deactivate();
    }
}
