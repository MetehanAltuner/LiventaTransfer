namespace LiventaTransfer.Domain.Enums;

public enum JobStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    PendingInvoice = 5,
    Invoiced = 6,
    Merged = 50,
    Cancelled = 99
}
