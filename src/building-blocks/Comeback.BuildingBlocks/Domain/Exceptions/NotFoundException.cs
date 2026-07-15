namespace Comeback.BuildingBlocks.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.", "not_found") { }

    public NotFoundException(string message, string? code = null) : base(message, code) { }
}
