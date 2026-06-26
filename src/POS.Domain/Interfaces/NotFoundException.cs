namespace POS.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, Guid id ) : base($"(entity) with id '{id} was not found.") { }
}