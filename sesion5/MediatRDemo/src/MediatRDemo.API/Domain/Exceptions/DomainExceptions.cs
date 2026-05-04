namespace MediatRDemo.API.Domain.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid id)
        : base($"Usuario con ID '{id}' no encontrado.") { }
}

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"Ya existe un usuario con el email '{email}'.") { }
}
