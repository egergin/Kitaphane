namespace Kitaphane.Domain.Books;

/// <summary>Reading status. Exchanged and stored as TO_READ / READING / COMPLETED.</summary>
public enum BookStatus
{
    ToRead,
    Reading,
    Completed,
}
