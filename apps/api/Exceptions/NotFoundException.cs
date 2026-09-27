namespace API.Exceptions;

public class NotFoundException : ApiException
{
    public override int StatusCode => 404;
    public NotFoundException() : base("Not Found") { }
    public NotFoundException(string message) : base(message) { }
}