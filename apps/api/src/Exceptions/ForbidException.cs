namespace API.Exceptions;

public class ForbidException : ApiException
{
    public override int StatusCode => 403;
    public ForbidException() : base("Access forbidden", "FORBIDDEN") { }
    public ForbidException(string message, string code = "FORBIDDEN") : base(message, code) { }
}
