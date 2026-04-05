using System.Globalization;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;

namespace Dataverse.Emulator.Application.Seeding;

public sealed class SeedScenarioSnapshotMapper(
    RecordValidationService recordValidationService)
{
    public const string CurrentSchemaVersion = "1.0";

    public ErrorOr<SeedScenarioSnapshotDocument> ToDocument(
        SeedScenario scenario,
        DateTimeOffset capturedAtUtc)
    {
        var errors = new List<Error>();

        var tables = scenario.Tables
            .OrderBy(table => table.EntitySetName, StringComparer.OrdinalIgnoreCase)
            .Select(table => new SeedScenarioSnapshotTableDocument(
                table.LogicalName,
                table.EntitySetName,
                table.PrimaryIdAttribute,
                table.PrimaryNameAttribute,
                table.Columns
                    .OrderBy(column => column.LogicalName, StringComparer.OrdinalIgnoreCase)
                    .Select(column => new SeedScenarioSnapshotColumnDocument(
                        column.LogicalName,
                        column.AttributeType.Name,
                        column.RequiredLevel.Name,
                        column.IsPrimaryId,
                        column.IsPrimaryName,
                        column.LookupTargetTable))
                    .ToArray(),
                table.AlternateKeys
                    .OrderBy(key => key.LogicalName, StringComparer.OrdinalIgnoreCase)
                    .Select(key => new SeedScenarioSnapshotAlternateKeyDocument(
                        key.LogicalName,
                        key.ColumnLogicalNames.ToArray()))
                    .ToArray()))
            .ToArray();

        var records = new List<SeedScenarioSnapshotRecordDocument>();
        foreach (var record in scenario.Records
                     .OrderBy(item => item.TableLogicalName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Id))
        {
            var values = new Dictionary<string, SeedScenarioSnapshotValueDocument>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in record.Values.Items.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                var valueResult = ToSnapshotValue(pair.Key, pair.Value);
                if (valueResult.IsError)
                {
                    errors.AddRange(valueResult.Errors);
                    continue;
                }

                values[pair.Key] = valueResult.Value;
            }

            records.Add(new SeedScenarioSnapshotRecordDocument(
                record.TableLogicalName,
                record.Id,
                record.Version,
                values));
        }

        return errors.Count > 0
            ? errors
            : new SeedScenarioSnapshotDocument(CurrentSchemaVersion, capturedAtUtc, tables, records);
    }

    public ErrorOr<SeedScenario> ToScenario(SeedScenarioSnapshotDocument document)
    {
        var errors = new List<Error>();

        if (!string.Equals(document.SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add(DomainErrors.Validation(
                "Seeding.Snapshot.SchemaVersionUnsupported",
                $"Snapshot schema version '{document.SchemaVersion}' is not supported."));
        }

        var duplicateTables = document.Tables
            .GroupBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicateTable in duplicateTables)
        {
            errors.Add(DomainErrors.Validation(
                "Seeding.Snapshot.Table.Duplicate",
                $"Snapshot contains duplicate table '{duplicateTable}'."));
        }

        var tables = new List<TableDefinition>();
        foreach (var tableDocument in document.Tables)
        {
            var tableResult = ToTableDefinition(tableDocument);
            if (tableResult.IsError)
            {
                errors.AddRange(tableResult.Errors);
                continue;
            }

            tables.Add(tableResult.Value);
        }

        var tablesByLogicalName = tables.ToDictionary(
            table => table.LogicalName,
            StringComparer.OrdinalIgnoreCase);

        var duplicateRecords = document.Records
            .GroupBy(record => $"{record.TableLogicalName}|{record.Id:N}", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicateRecord in duplicateRecords)
        {
            errors.Add(DomainErrors.Validation(
                "Seeding.Snapshot.Record.Duplicate",
                $"Snapshot contains duplicate row '{duplicateRecord}'."));
        }

        var records = new List<EntityRecord>();
        foreach (var recordDocument in document.Records)
        {
            if (!tablesByLogicalName.TryGetValue(recordDocument.TableLogicalName, out var table))
            {
                errors.Add(DomainErrors.UnknownTable(recordDocument.TableLogicalName));
                continue;
            }

            var recordResult = ToEntityRecord(table, recordDocument);
            if (recordResult.IsError)
            {
                errors.AddRange(recordResult.Errors);
                continue;
            }

            records.Add(recordResult.Value);
        }

        return errors.Count > 0
            ? errors
            : new SeedScenario(tables, records);
    }

    private ErrorOr<TableDefinition> ToTableDefinition(SeedScenarioSnapshotTableDocument tableDocument)
    {
        var errors = new List<Error>();
        var columns = new List<ColumnDefinition>();

        foreach (var columnDocument in tableDocument.Columns)
        {
            var attributeTypeResult = ToAttributeType(columnDocument.AttributeType);
            if (attributeTypeResult.IsError)
            {
                errors.AddRange(attributeTypeResult.Errors);
                continue;
            }

            var requiredLevelResult = ToRequiredLevel(columnDocument.RequiredLevel);
            if (requiredLevelResult.IsError)
            {
                errors.AddRange(requiredLevelResult.Errors);
                continue;
            }

            var columnResult = ColumnDefinition.Create(
                columnDocument.LogicalName,
                attributeTypeResult.Value,
                requiredLevelResult.Value,
                columnDocument.IsPrimaryId,
                columnDocument.IsPrimaryName,
                columnDocument.LookupTargetTable);

            if (columnResult.IsError)
            {
                errors.AddRange(columnResult.Errors);
                continue;
            }

            columns.Add(columnResult.Value);
        }

        var alternateKeys = new List<AlternateKeyDefinition>();
        foreach (var alternateKeyDocument in tableDocument.AlternateKeys)
        {
            var alternateKeyResult = AlternateKeyDefinition.Create(
                alternateKeyDocument.LogicalName,
                alternateKeyDocument.ColumnLogicalNames);

            if (alternateKeyResult.IsError)
            {
                errors.AddRange(alternateKeyResult.Errors);
                continue;
            }

            alternateKeys.Add(alternateKeyResult.Value);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return TableDefinition.Create(
            tableDocument.LogicalName,
            tableDocument.EntitySetName,
            tableDocument.PrimaryIdAttribute,
            tableDocument.PrimaryNameAttribute,
            columns,
            alternateKeys);
    }

    private ErrorOr<EntityRecord> ToEntityRecord(
        TableDefinition table,
        SeedScenarioSnapshotRecordDocument recordDocument)
    {
        var errors = new List<Error>();
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in recordDocument.Values)
        {
            var valueResult = ToDomainValue(pair.Key, pair.Value);
            if (valueResult.IsError)
            {
                errors.AddRange(valueResult.Errors);
                continue;
            }

            values[pair.Key] = valueResult.Value;
        }

        if (!values.ContainsKey(table.PrimaryIdAttribute))
        {
            values[table.PrimaryIdAttribute] = recordDocument.Id;
        }
        else if (values[table.PrimaryIdAttribute] is Guid primaryId && primaryId != recordDocument.Id)
        {
            errors.Add(DomainErrors.Validation(
                "Seeding.Snapshot.Record.PrimaryIdMismatch",
                $"Snapshot row '{recordDocument.Id}' on table '{table.LogicalName}' must match '{table.PrimaryIdAttribute}' when both are supplied."));
        }

        errors.AddRange(recordValidationService.ValidateCreate(table, values));
        if (errors.Count > 0)
        {
            return errors;
        }

        var recordValuesResult = RecordValues.Create(values);
        if (recordValuesResult.IsError)
        {
            return recordValuesResult.Errors;
        }

        return EntityRecord.Create(
            table.LogicalName,
            recordDocument.Id,
            recordValuesResult.Value,
            recordDocument.Version);
    }

    private static ErrorOr<SeedScenarioSnapshotValueDocument> ToSnapshotValue(
        string logicalName,
        object? value)
    {
        if (value is null)
        {
            return new SeedScenarioSnapshotValueDocument("Null", null);
        }

        return value switch
        {
            string text => new SeedScenarioSnapshotValueDocument("String", text),
            Guid guid => new SeedScenarioSnapshotValueDocument("Guid", guid.ToString()),
            bool boolean => new SeedScenarioSnapshotValueDocument("Boolean", boolean ? "true" : "false"),
            int integer => new SeedScenarioSnapshotValueDocument("Integer", integer.ToString(CultureInfo.InvariantCulture)),
            decimal decimalValue => new SeedScenarioSnapshotValueDocument("Decimal", decimalValue.ToString(CultureInfo.InvariantCulture)),
            double doubleValue => new SeedScenarioSnapshotValueDocument("Decimal", doubleValue.ToString(CultureInfo.InvariantCulture)),
            float floatValue => new SeedScenarioSnapshotValueDocument("Decimal", floatValue.ToString(CultureInfo.InvariantCulture)),
            DateTimeOffset dateTimeOffset => new SeedScenarioSnapshotValueDocument("DateTimeOffset", dateTimeOffset.ToString("o", CultureInfo.InvariantCulture)),
            DateTime dateTime => new SeedScenarioSnapshotValueDocument(
                "DateTimeOffset",
                NormalizeDateTime(dateTime).ToString("o", CultureInfo.InvariantCulture)),
            _ => DomainErrors.Validation(
                "Seeding.Snapshot.Value.Unsupported",
                $"Snapshot export does not support value type '{value.GetType().FullName}' for column '{logicalName}'.")
        };
    }

    private static ErrorOr<object?> ToDomainValue(
        string logicalName,
        SeedScenarioSnapshotValueDocument valueDocument)
    {
        if (valueDocument is null)
        {
            return DomainErrors.Validation(
                "Seeding.Snapshot.Value.Required",
                $"Snapshot value for column '{logicalName}' is required.");
        }

        if (string.Equals(valueDocument.Kind, "Null", StringComparison.Ordinal))
        {
            return (object?)null;
        }

        if (string.Equals(valueDocument.Kind, "String", StringComparison.Ordinal))
        {
            return valueDocument.Value ?? string.Empty;
        }

        if (string.Equals(valueDocument.Kind, "Guid", StringComparison.Ordinal)
            && Guid.TryParse(valueDocument.Value, out var guid))
        {
            return guid;
        }

        if (string.Equals(valueDocument.Kind, "Boolean", StringComparison.Ordinal)
            && bool.TryParse(valueDocument.Value, out var boolean))
        {
            return boolean;
        }

        if (string.Equals(valueDocument.Kind, "Integer", StringComparison.Ordinal)
            && int.TryParse(valueDocument.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return integer;
        }

        if (string.Equals(valueDocument.Kind, "Decimal", StringComparison.Ordinal)
            && decimal.TryParse(valueDocument.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue;
        }

        if (string.Equals(valueDocument.Kind, "DateTimeOffset", StringComparison.Ordinal)
            && DateTimeOffset.TryParse(valueDocument.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
        {
            return dateTimeOffset;
        }

        return DomainErrors.Validation(
            "Seeding.Snapshot.Value.Invalid",
            $"Snapshot value for column '{logicalName}' uses unsupported kind '{valueDocument.Kind}'.");
    }

    private static ErrorOr<AttributeType> ToAttributeType(string name)
        => name switch
        {
            nameof(AttributeType.UniqueIdentifier) => AttributeType.UniqueIdentifier,
            nameof(AttributeType.String) => AttributeType.String,
            nameof(AttributeType.Integer) => AttributeType.Integer,
            nameof(AttributeType.Decimal) => AttributeType.Decimal,
            nameof(AttributeType.Boolean) => AttributeType.Boolean,
            nameof(AttributeType.DateTime) => AttributeType.DateTime,
            nameof(AttributeType.Lookup) => AttributeType.Lookup,
            _ => DomainErrors.Validation(
                "Seeding.Snapshot.Column.AttributeTypeInvalid",
                $"Snapshot column attribute type '{name}' is not supported.")
        };

    private static ErrorOr<RequiredLevel> ToRequiredLevel(string name)
        => name switch
        {
            nameof(RequiredLevel.None) => RequiredLevel.None,
            nameof(RequiredLevel.SystemRequired) => RequiredLevel.SystemRequired,
            nameof(RequiredLevel.ApplicationRequired) => RequiredLevel.ApplicationRequired,
            _ => DomainErrors.Validation(
                "Seeding.Snapshot.Column.RequiredLevelInvalid",
                $"Snapshot required level '{name}' is not supported.")
        };

    private static DateTimeOffset NormalizeDateTime(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime()),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        };
}
