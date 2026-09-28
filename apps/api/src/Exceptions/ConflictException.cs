namespace API.Exceptions;

public class ConflictException : ApiException
{
    public override int StatusCode => 409;
    public ConflictException() : base("Conflict", "CONFLICT") { }
    public ConflictException(string message, string code = "CONFLICT") : base(message, code) { }
}
