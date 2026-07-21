using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomInterestManager : HexSpatialHash3DInterestManagement
{
    public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
    {
        if (identity.TryGetComponent(out AlwaysVisible _))
            return true;

        return base.OnCheckObserver(identity, newObserver);
    }
}
