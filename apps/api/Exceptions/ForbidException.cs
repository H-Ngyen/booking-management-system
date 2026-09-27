namespace API.Exceptions;

public class ForbidException : ApiException
{
    public override int StatusCode => 403;
    public ForbidException() : base("Access forbidden") { }
    public ForbidException(string message) : base(message) { }
}