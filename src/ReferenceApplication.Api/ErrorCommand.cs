using FluentValidation;
using ServiceComponents.Api.Mediator;

namespace ReferenceApplication.Api
{
    public class ErrorCommand : Command
    {
        public int Code { get; }
        public string Message { get; }

        public ErrorCommand(int code, string message) : base(default)
        {
            Code = code;
            Message = message;
        }
    }

    public class ErrorCommandValidator : AbstractValidator<ErrorCommand>
    {
        public ErrorCommandValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Error code is mandatory");
        }
    }
}
