namespace API.Exceptions;

public class ConflictException : ApiException
{
    public override int StatusCode => 409;
    public ConflictException() : base("Conflict") { }
    public ConflictException(string message) : base(message) { }
}
