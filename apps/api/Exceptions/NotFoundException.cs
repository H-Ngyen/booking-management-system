namespace API.Exceptions;

public class NotFoundException : ApiException
{
    public override int StatusCode => 404;
    public NotFoundException() : base("Not Found", "NOT_FOUND") { }
    public NotFoundException(string message, string code = "NOT_FOUND") : base(message, code) { }
}
