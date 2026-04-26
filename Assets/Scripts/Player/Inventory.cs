using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : NetworkBehaviour
{
    public UnityEvent PickedUpAmulet = new();
    public UnityEvent SpentAmulet = new();

    [SyncVar(hook = nameof(OnClientAmuletOwnershipChanged))]
    public bool HasAmulet;

    [SerializeField] private PlayerAudioController _audioController;
    private GameHud _gameHud;

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind((hud, hasAmulet) => hud.SetInvestigatorAmulet(hasAmulet), HasAmulet);
    }

    [Server]
    public bool ServerPickUpAmulet()
    {
        if (!isServer || HasAmulet)
            return false;

        HasAmulet = true;
        return true;
    }

    [Server]
    public bool ServerSpendAmulet()
    {
        if (!isServer || !HasAmulet)
            return false;

        HasAmulet = false;
        return true;
    }

    [Client]
    public void OnClientAmuletOwnershipChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer)
            return;

        _ = _gameHud.Bind((hud, hasAmulet) => hud.SetInvestigatorAmulet(hasAmulet), newValue);
        //_ = _audioController.Bind(newValue ? c => c.PlayAmuletPickUpSound() : c => c.PlayAmuletSpendSound());

        if (newValue)
            PickedUpAmulet.Invoke();
        else
            SpentAmulet.Invoke();
    }
}
