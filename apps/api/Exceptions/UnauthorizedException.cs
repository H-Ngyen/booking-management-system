namespace API.Exceptions;

public class UnauthorizedException : ApiException
{
    public override int StatusCode => 401;
    public UnauthorizedException() : base("Unauthorized Access") { }
    public UnauthorizedException(string message) : base(message) { }
}