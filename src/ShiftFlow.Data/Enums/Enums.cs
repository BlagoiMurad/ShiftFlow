namespace ShiftFlow.Data.Enums;

public enum ShiftStatus
{
    Planned = 0,
    Completed = 1,
    Cancelled = 2
}

public enum RequestStatus
{
    /// <summary>Чака отговор от колегата (само при размяна).</summary>
    PendingColleague = 0,
    /// <summary>Чака решение от управител.</summary>
    PendingManager = 1,
    Approved = 2,
    Rejected = 3,
    /// <summary>Колегата е отказал размяната.</summary>
    Declined = 4,
    Cancelled = 5
}

public enum LeaveType
{
    Paid = 0,
    Sick = 1,
    Unpaid = 2
}
