namespace API.Exceptions;

public class UnauthorizedException : ApiException
{
    public override int StatusCode => 401;
    public UnauthorizedException() : base("Unauthorized Access", "UNAUTHORIZED") { }
    public UnauthorizedException(string message, string code = "UNAUTHORIZED") : base(message, code) { }
}
