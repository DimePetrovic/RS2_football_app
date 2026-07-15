namespace Comeback.BuildingBlocks.Domain.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, string? code = null) : base(message, code) { }
}
