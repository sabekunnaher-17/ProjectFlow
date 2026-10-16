namespace ProjectFlow.Core.Common;

public enum FailureKind { Validation, NotFound, Forbidden, Conflict }
public sealed class DomainException(string message, FailureKind kind = FailureKind.Validation) : Exception(message)
{
    public FailureKind Kind { get; } = kind;
}
