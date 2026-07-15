namespace Comeback.BuildingBlocks.Domain.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access to this resource is forbidden.", string? code = null)
        : base(message, code) { }
}
