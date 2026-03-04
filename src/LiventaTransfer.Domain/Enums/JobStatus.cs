namespace LiventaTransfer.Domain.Enums;

public enum JobStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    PendingInvoice = 5,
    Invoiced = 6,
    Cancelled = 99
}
