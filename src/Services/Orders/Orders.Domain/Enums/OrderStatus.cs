namespace Orders.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Preparing = 4,
    ReadyForFulfillment = 5,
    Completed = 6,
    Cancelled = 7
}