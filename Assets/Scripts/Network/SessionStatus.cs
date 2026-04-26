using UnityEngine;

#region Host

public enum StartSessionStatus
{
    None = 0,
    Created,
    Failed
}

public enum KickPlayerStatus
{
    None = 0,
    Kicked,
    IsNotHost,
    Failed
}

public enum StopSessionStatus
{
    None = 0,
    Stopped,
    Failed
}

#endregion Host

#region Client

public enum FindSessionsStatus
{
    None = 0,
    Found,
    Failed
}

public enum JoinSessionStatus
{
    None = 0,
    Joined,
    NotFound,
    SessionIsFull,
    Failed
}

public enum LeaveSessionStatus
{
    None = 0,
    Left,
    Failed
}

#endregion Client
