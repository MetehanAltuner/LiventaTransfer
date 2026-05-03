namespace LiventaTransfer.Domain.Enums;

/// <summary>
/// Sürücünün operasyonel ilerleme aşamaları. JobStatus'un (özellikle InProgress) alt aşaması.
/// Computed: Job.ContactedAt / DepartedAt ve JobStop.PickedUpAt / DroppedOffAt timestamp'lerinden türetilir.
/// </summary>
public enum DriverStage
{
    NotStarted = 0,
    Contacted = 1,
    Departed = 2,
    PickedUp = 3,
    DroppedOff = 4
}
