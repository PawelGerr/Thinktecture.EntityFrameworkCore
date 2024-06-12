namespace Thinktecture.EntityFrameworkCore.Internal;

/// <summary>
/// Annotation names.
/// </summary>
public static class ThinktectureRelationalAnnotationNames
{
    private const string _PREFIX = "Thinktecture:Relational:";

    /// <summary>
    /// Annotation name for table hints.
    /// </summary>
    public const string TABLE_HINTS = _PREFIX + "TableHints";

    /// <summary>
    /// Annotation name for ignored columns.
    /// </summary>
    public const string IGNORED_COLUMNS = _PREFIX + "IgnoredColumns";
}
