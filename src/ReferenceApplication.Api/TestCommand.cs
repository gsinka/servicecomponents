using System;
using FluentValidation;
using ServiceComponents.Api.Mediator;

namespace ReferenceApplication.Api
{
    public class TestCommand : Command
    {
        public string Data { get; }

        public TestCommand(string data) : base(default)
        {
            Data = data;
        }
    }

    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Data).NotEmpty().WithMessage("Data cannot be empty");
        }
    }
}
