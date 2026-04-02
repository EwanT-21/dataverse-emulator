using ErrorOr;

namespace Dataverse.Emulator.Domain.Common;

public static class DomainErrors
{
    public static Error UnknownTable(string logicalName)
        => Error.NotFound(
            code: "Metadata.Table.NotFound",
            description: $"Table '{logicalName}' does not exist.");

    public static Error RowNotFound(string tableLogicalName, Guid id)
        => Error.NotFound(
            code: "Records.Row.NotFound",
            description: $"Row '{id}' does not exist on table '{tableLogicalName}'.");

    public static Error DuplicateRow(string tableLogicalName, Guid id)
        => Error.Conflict(
            code: "Records.Row.Duplicate",
            description: $"Row '{id}' already exists on table '{tableLogicalName}'.");

    public static Error UnknownColumn(string tableLogicalName, string logicalName)
        => Error.Validation(
            code: "Metadata.Column.Unknown",
            description: $"Column '{logicalName}' does not exist on table '{tableLogicalName}'.");

    public static Error RequiredColumn(string tableLogicalName, string logicalName)
        => Error.Validation(
            code: "Metadata.Column.Required",
            description: $"Column '{logicalName}' is required on table '{tableLogicalName}'.");

    public static Error ImmutableColumn(string logicalName)
        => Error.Validation(
            code: "Metadata.Column.Immutable",
            description: $"Column '{logicalName}' is immutable.");

    public static Error IncompatibleColumnValue(string logicalName, object? value)
        => Error.Validation(
            code: "Metadata.Column.IncompatibleValue",
            description: $"Column '{logicalName}' does not accept values of type '{value?.GetType().Name ?? "null"}'.");
}
