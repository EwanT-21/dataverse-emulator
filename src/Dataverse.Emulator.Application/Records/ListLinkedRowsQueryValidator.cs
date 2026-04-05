using Dataverse.Emulator.Domain.Queries;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListLinkedRowsQueryValidator : AbstractValidator<ListLinkedRowsQuery>
{
    public ListLinkedRowsQueryValidator()
    {
        RuleFor(query => query.Query).NotNull().SetValidator(new LinkedRecordQueryValidator());
    }
}

public sealed class LinkedRecordQueryValidator : AbstractValidator<LinkedRecordQuery>
{
    public LinkedRecordQueryValidator()
    {
        RuleFor(query => query.RootTableLogicalName).NotEmpty();
        RuleFor(query => query.Joins).NotNull();
        RuleFor(query => query.Joins.Count).GreaterThan(0);
        RuleFor(query => query.Top).GreaterThan(0).When(query => query.Top.HasValue);
        RuleFor(query => query.Page!.Size).GreaterThan(0).When(query => query.Page is not null);
        RuleFor(query => query.CurrentPageNumber).GreaterThan(0);
        RuleForEach(query => query.Joins).SetValidator(new LinkedRecordJoinValidator());
        RuleForEach(query => query.Sorts).SetValidator(new LinkedRecordSortValidator());

        When(query => query.Filter is not null, () =>
        {
            RuleFor(query => query.Filter!).SetValidator(new LinkedRecordFilterValidator());
        });
    }
}

public sealed class LinkedRecordJoinValidator : AbstractValidator<LinkedRecordJoin>
{
    public LinkedRecordJoinValidator()
    {
        RuleFor(join => join.TableLogicalName).NotEmpty();
        RuleFor(join => join.Alias).NotEmpty();
        RuleFor(join => join.FromAttributeName).NotEmpty();
        RuleFor(join => join.ToAttributeName).NotEmpty();

        When(join => join.Filter is not null, () =>
        {
            RuleFor(join => join.Filter!).SetValidator(new LinkedRecordFilterValidator());
        });
    }
}

public sealed class LinkedRecordFilterValidator : AbstractValidator<LinkedRecordFilter>
{
    public LinkedRecordFilterValidator()
    {
        RuleFor(filter => filter.Conditions).NotNull();
        RuleFor(filter => filter.Filters).NotNull();
        RuleForEach(filter => filter.Conditions).SetValidator(new LinkedRecordConditionValidator());
        RuleForEach(filter => filter.Filters).SetValidator(this);
    }
}

public sealed class LinkedRecordConditionValidator : AbstractValidator<LinkedRecordCondition>
{
    public LinkedRecordConditionValidator()
    {
        RuleFor(condition => condition.ScopeName).NotEmpty();
        RuleFor(condition => condition.ColumnLogicalName).NotEmpty();
        RuleFor(condition => condition.Values).NotNull();

        When(
            condition => condition.Operator == ConditionOperator.Null || condition.Operator == ConditionOperator.NotNull,
            () => RuleFor(condition => condition.Values.Count).Equal(0));

        When(
            condition => condition.Operator == ConditionOperator.In,
            () => RuleFor(condition => condition.Values.Count).GreaterThan(0));

        When(
            condition => condition.Operator != ConditionOperator.Null
                && condition.Operator != ConditionOperator.NotNull
                && condition.Operator != ConditionOperator.In,
            () => RuleFor(condition => condition.Values.Count).Equal(1));
    }
}

public sealed class LinkedRecordSortValidator : AbstractValidator<LinkedRecordSort>
{
    public LinkedRecordSortValidator()
    {
        RuleFor(sort => sort.ScopeName).NotEmpty();
        RuleFor(sort => sort.ColumnLogicalName).NotEmpty();
    }
}
