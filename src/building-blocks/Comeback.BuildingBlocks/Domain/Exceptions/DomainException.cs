namespace Comeback.BuildingBlocks.Domain.Exceptions;

public abstract class DomainException : Exception
{
    /// <summary>Machine-readable error code; clients map it to a localized message.</summary>
    public string? Code { get; }

    protected DomainException(string message, string? code = null) : base(message) => Code = code;
}
