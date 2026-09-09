namespace Hospital.Domain.Enums;

/// <summary>
/// Ayurvedic constitutional types. Combinations are stored as flags on Patient.Prakriti.
/// </summary>
[Flags]
public enum DoshaType
{
    None = 0,
    Vata = 1,
    Pitta = 2,
    Kapha = 4
}
