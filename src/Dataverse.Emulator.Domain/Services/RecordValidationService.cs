using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Services;

public sealed class RecordValidationService
{
    public IReadOnlyCollection<Error> ValidateCreate(
        TableDefinition table,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<Error>();

        foreach (var pair in values)
        {
            var column = table.FindColumn(pair.Key);
            if (column is null)
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, pair.Key));
                continue;
            }

            if (!IsCompatible(column, pair.Value))
            {
                errors.Add(DomainErrors.IncompatibleColumnValue(pair.Key, pair.Value));
            }
        }

        foreach (var column in table.Columns.Where(column =>
                     !column.IsPrimaryId
                     && (column.RequiredLevel == RequiredLevel.ApplicationRequired || column.RequiredLevel == RequiredLevel.SystemRequired)))
        {
            if (!values.ContainsKey(column.LogicalName))
            {
                errors.Add(DomainErrors.RequiredColumn(table.LogicalName, column.LogicalName));
            }
        }

        return errors;
    }

    public IReadOnlyCollection<Error> ValidateUpdate(
        TableDefinition table,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<Error>();

        foreach (var pair in values)
        {
            var column = table.FindColumn(pair.Key);
            if (column is null)
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, pair.Key));
                continue;
            }

            if (column.IsPrimaryId || pair.Key.Equals(table.PrimaryIdAttribute, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(DomainErrors.ImmutableColumn(pair.Key));
                continue;
            }

            if (!IsCompatible(column, pair.Value))
            {
                errors.Add(DomainErrors.IncompatibleColumnValue(pair.Key, pair.Value));
            }
        }

        return errors;
    }

    private static bool IsCompatible(ColumnDefinition column, object? value)
    {
        if (value is null)
        {
            return column.RequiredLevel == RequiredLevel.None;
        }

        if (column.AttributeType == AttributeType.UniqueIdentifier)
        {
            return value is Guid;
        }

        if (column.AttributeType == AttributeType.String)
        {
            return value is string;
        }

        if (column.AttributeType == AttributeType.Integer)
        {
            return value is int;
        }

        if (column.AttributeType == AttributeType.Decimal)
        {
            return value is decimal or double or float;
        }

        if (column.AttributeType == AttributeType.Boolean)
        {
            return value is bool;
        }

        if (column.AttributeType == AttributeType.DateTime)
        {
            return value is DateTime or DateTimeOffset;
        }

        if (column.AttributeType == AttributeType.Lookup)
        {
            return value is Guid;
        }

        return false;
    }
}
