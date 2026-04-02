using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class GetRowByIdQueryValidator : AbstractValidator<GetRowByIdQuery>
{
    public GetRowByIdQueryValidator()
    {
        RuleFor(query => query.TableLogicalName).NotEmpty();
        RuleFor(query => query.Id).NotEqual(Guid.Empty);
    }
}
