
namespace FileForge.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(Guid id) : base($"Resource with ID '{id}' was not found.") {}
    public NotFoundException(string resourceName, object key) : base($"{resourceName} with key '{key}' was not found.") {}
}
