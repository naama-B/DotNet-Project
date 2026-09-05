namespace Gym.Core.Enums;

/// <summary>Application roles. At least two roles with different real permissions.</summary>
public enum UserRole
{
    Member = 0,
    Admin = 1
}

public enum ClassSessionStatus
{
    Scheduled = 0,
    Cancelled = 1
}

public enum BookingStatus
{
    /// <summary>The member holds a real spot in the class.</summary>
    Confirmed = 0,

    /// <summary>The member cancelled their spot.</summary>
    CancelledByMember = 1,

    /// <summary>The class was full; the member is queued on the waitlist.</summary>
    Waitlisted = 2
}
