namespace Hospital.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.")
    {
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

public class WardFullException : DomainException
{
    public WardFullException(string message) : base(message)
    {
    }
}
