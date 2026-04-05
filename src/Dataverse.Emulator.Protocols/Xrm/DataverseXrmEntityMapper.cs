using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmEntityMapper
{
    public static ErrorOr<IReadOnlyDictionary<string, object?>> ToCreateValues(
        Entity entity,
        TableDefinition table)
        => ToValues(entity, table, includePrimaryIdFromEntityId: true);

    public static ErrorOr<IReadOnlyDictionary<string, object?>> ToUpdateValues(
        Entity entity,
        TableDefinition table)
    {
        var valuesResult = ToValues(entity, table, includePrimaryIdFromEntityId: false);
        if (valuesResult.IsError)
        {
            return valuesResult.Errors;
        }

        var values = new Dictionary<string, object?>(valuesResult.Value, StringComparer.OrdinalIgnoreCase);
        values.Remove(table.PrimaryIdAttribute);
        return values;
    }

    public static ErrorOr<Guid> ResolveRecordId(Entity entity, TableDefinition table)
    {
        if (entity.Id != Guid.Empty)
        {
            return entity.Id;
        }

        if (entity.Attributes.TryGetValue(table.PrimaryIdAttribute, out var rawValue))
        {
            var valueResult = ToScalarValue(rawValue, table.PrimaryIdAttribute);
            if (!valueResult.IsError && valueResult.Value is Guid id)
            {
                return id;
            }
        }

        return DomainErrors.Validation(
            "Protocol.Xrm.Entity.IdRequired",
            $"Entity '{table.LogicalName}' requires a non-empty '{table.PrimaryIdAttribute}'.");
    }

    public static ErrorOr<IReadOnlyList<string>> ResolveSelectedColumns(ColumnSet columnSet)
        => ResolveSelectedColumns(columnSet, allowEmptySelection: false);

    public static ErrorOr<IReadOnlyList<string>> ResolveSelectedColumns(
        ColumnSet columnSet,
        bool allowEmptySelection)
    {
        if (columnSet is null)
        {
            return DataverseXrmErrors.ParameterRequired("columnSet");
        }

        if (columnSet.AllColumns)
        {
            return Array.Empty<string>();
        }

        if (columnSet.Columns.Count == 0)
        {
            if (allowEmptySelection)
            {
                return Array.Empty<string>();
            }

            return DomainErrors.Validation(
                "Protocol.Xrm.ColumnSet.Required",
                "ColumnSet must specify one or more columns or use AllColumns.");
        }

        return columnSet.Columns
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static object? ToReadAttributeValue(object? value, ColumnDefinition? column)
        => ToAttributeValue(value, column);

    public static Entity ToEntity(TableDefinition table, EntityRecord record)
    {
        var entity = new Entity(table.LogicalName, record.Id);

        foreach (var pair in record.Values.Items)
        {
            entity[pair.Key] = ToAttributeValue(pair.Value, table.FindColumn(pair.Key));
        }

        return entity;
    }

    public static EntityCollection ToEntityCollection(
        TableDefinition table,
        IReadOnlyList<EntityRecord> records)
        => ToEntityCollection(table, new PageResult<EntityRecord>(records), currentPageNumber: 1);

    public static EntityCollection ToEntityCollection(
        TableDefinition table,
        PageResult<EntityRecord> pageResult,
        int currentPageNumber)
    {
        var collection = new EntityCollection
        {
            EntityName = table.LogicalName,
            MoreRecords = pageResult.ContinuationToken is not null,
            PagingCookie = pageResult.ContinuationToken is not null
                ? DataverseXrmPagingCookie.Create(pageResult.ContinuationToken, currentPageNumber + 1)
                : null
        };

        foreach (var record in pageResult.Items)
        {
            collection.Entities.Add(ToEntity(table, record));
        }

        return collection;
    }

    public static EntityCollection ToEntityCollection(
        TableDefinition rootTable,
        IReadOnlyDictionary<string, TableDefinition> linkedTablesByAlias,
        LinkedRecordQuery query,
        PageResult<LinkedEntityRecord> pageResult,
        int currentPageNumber)
    {
        var collection = new EntityCollection
        {
            EntityName = rootTable.LogicalName,
            MoreRecords = pageResult.ContinuationToken is not null,
            PagingCookie = pageResult.ContinuationToken is not null
                ? DataverseXrmPagingCookie.Create(pageResult.ContinuationToken, currentPageNumber + 1)
                : null
        };

        foreach (var linkedRecord in pageResult.Items)
        {
            var entity = ToEntity(rootTable, linkedRecord.RootRecord);

            foreach (var join in query.Joins)
            {
                if (!linkedRecord.LinkedRecords.TryGetValue(join.Alias, out var relatedRecord)
                    || !linkedTablesByAlias.TryGetValue(join.Alias, out var linkedTable))
                {
                    continue;
                }

                var columnNames = join.ReturnAllColumns
                    ? linkedTable.Columns.Select(column => column.LogicalName)
                    : join.SelectedColumns;

                foreach (var columnName in columnNames.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!relatedRecord.Values.TryGetValue(columnName, out var value))
                    {
                        continue;
                    }

                    entity[$"{join.Alias}.{columnName}"] = new AliasedValue(
                        join.Alias,
                        columnName,
                        ToAttributeValue(value, linkedTable.FindColumn(columnName)));
                }
            }

            collection.Entities.Add(entity);
        }

        return collection;
    }

    public static ErrorOr<object?> ToScalarValue(object? value, string attributeName)
    {
        if (value is null)
        {
            return (object?)null;
        }

        if (value is AliasedValue aliasedValue)
        {
            value = aliasedValue.Value;
        }

        return value switch
        {
            null => (object?)null,
            string text => text,
            Guid guid => guid,
            bool boolean => boolean,
            int integer => integer,
            decimal decimalValue => decimalValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => NormalizeDateTime(dateTime),
            EntityReference entityReference => entityReference.Id,
            OptionSetValue optionSetValue => optionSetValue.Value,
            Money money => money.Value,
            _ => DomainErrors.Validation(
                "Protocol.Xrm.Value.Unsupported",
                $"Attribute '{attributeName}' uses unsupported SDK value type '{value.GetType().FullName}'.")
        };
    }

    private static ErrorOr<IReadOnlyDictionary<string, object?>> ToValues(
        Entity entity,
        TableDefinition table,
        bool includePrimaryIdFromEntityId)
    {
        if (entity is null)
        {
            return DataverseXrmErrors.ParameterRequired("entity");
        }

        if (entity.RelatedEntities.Count > 0)
        {
            return DataverseXrmErrors.UnsupportedOperation("Related entity graphs");
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<Error>();

        foreach (var attribute in entity.Attributes)
        {
            var convertedValue = ToScalarValue(attribute.Value, attribute.Key);
            if (convertedValue.IsError)
            {
                errors.AddRange(convertedValue.Errors);
                continue;
            }

            values[attribute.Key] = convertedValue.Value;
        }

        if (includePrimaryIdFromEntityId
            && entity.Id != Guid.Empty
            && !values.ContainsKey(table.PrimaryIdAttribute))
        {
            values[table.PrimaryIdAttribute] = entity.Id;
        }

        return errors.Count > 0
            ? errors
            : values;
    }

    private static object? ToAttributeValue(object? value, ColumnDefinition? column)
    {
        if (value is null)
        {
            return null;
        }

        if (column?.AttributeType == AttributeType.DateTime)
        {
            return value switch
            {
                DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
                DateTime dateTime => dateTime,
                _ => value
            };
        }

        if (column?.AttributeType == AttributeType.Lookup
            && value is Guid id
            && !string.IsNullOrWhiteSpace(column.LookupTargetTable))
        {
            return new EntityReference(column.LookupTargetTable, id);
        }

        return value;
    }

    private static DateTimeOffset NormalizeDateTime(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime()),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        };
}
