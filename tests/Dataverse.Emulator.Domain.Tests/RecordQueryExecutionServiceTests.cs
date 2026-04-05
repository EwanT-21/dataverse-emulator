using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Domain.Tests;

public sealed class RecordQueryExecutionServiceTests
{
    [Fact]
    public void Execute_Applies_Filter_Sort_Paging_And_Projection()
    {
        var query = RecordQuery.Create(
            "account",
            selectedColumns: ["name", "accountnumber"],
            filter: new QueryFilter(
                FilterOperator.And,
                [
                    new QueryCondition(
                        "name",
                        ConditionOperator.BeginsWith,
                        ["Al"]),
                    new QueryCondition(
                        "accountnumber",
                        ConditionOperator.NotNull,
                        [])
                ],
                []),
            sorts:
            [
                new QuerySort(
                    "name",
                    SortDirection.Ascending)
            ],
            page: new PageRequest(
                1,
                continuationToken: "1"));

        Assert.False(query.IsError);

        var records = new[]
        {
            CreateRecord(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Alpha",
                    ["accountnumber"] = "A-100",
                    ["notes"] = "first"
                }),
            CreateRecord(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Alpine",
                    ["accountnumber"] = "A-200",
                    ["notes"] = "second"
                }),
            CreateRecord(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Bravo",
                    ["accountnumber"] = "B-100",
                    ["notes"] = "third"
                }),
            CreateRecord(
                "contact",
                new Dictionary<string, object?>
                {
                    ["fullname"] = "Ignored Contact"
                })
        };

        var service = new RecordQueryExecutionService();

        var result = service.Execute(query.Value, records);

        Assert.Single(result.Items);
        Assert.Equal("Alpine", result.Items[0].Values["name"]);
        Assert.Equal("A-200", result.Items[0].Values["accountnumber"]);
        Assert.False(result.Items[0].Values.Contains("notes"));
        Assert.Null(result.ContinuationToken);
    }

    [Fact]
    public void Execute_Uses_CaseInsensitive_String_Matching()
    {
        var query = RecordQuery.Create(
            "account",
            selectedColumns: ["name"],
            conditions:
            [
                new QueryCondition(
                    "name",
                    ConditionOperator.Equal,
                    ["contoso"])
            ]);

        Assert.False(query.IsError);

        var records = new[]
        {
            CreateRecord(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Contoso"
                }),
            CreateRecord(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "fabrikam"
                })
        };

        var service = new RecordQueryExecutionService();

        var result = service.Execute(query.Value, records);

        Assert.Single(result.Items);
        Assert.Equal("Contoso", result.Items[0].Values["name"]);
    }

    private static EntityRecord CreateRecord(
        string tableLogicalName,
        IReadOnlyDictionary<string, object?> values)
    {
        var recordValues = RecordValues.Create(values);
        Assert.False(recordValues.IsError);

        var record = EntityRecord.Create(tableLogicalName, Guid.NewGuid(), recordValues.Value);
        Assert.False(record.IsError);
        return record.Value;
    }
}
