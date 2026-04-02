using Dataverse.Emulator.Domain.Queries;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class RecordQueryValidator : AbstractValidator<RecordQuery>
{
    public RecordQueryValidator()
    {
        RuleFor(query => query.TableLogicalName).NotEmpty();
        RuleFor(query => query.Top).GreaterThan(0).When(query => query.Top.HasValue);
        RuleFor(query => query.Page!.Size).GreaterThan(0).When(query => query.Page is not null);
    }
}
