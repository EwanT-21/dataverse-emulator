using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListRowsQueryValidator : AbstractValidator<ListRowsQuery>
{
    public ListRowsQueryValidator()
    {
        RuleFor(query => query.Query).NotNull().SetValidator(new RecordQueryValidator());
    }
}
