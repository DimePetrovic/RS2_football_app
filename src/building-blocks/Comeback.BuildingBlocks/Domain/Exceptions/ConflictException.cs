namespace Comeback.BuildingBlocks.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message, string? code = null) : base(message, code) { }
}
