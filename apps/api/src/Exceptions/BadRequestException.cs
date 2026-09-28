namespace API.Exceptions;

public class BadRequestException : ApiException
{
    public override int StatusCode => 400;
    public BadRequestException() : base("Invalid request", "BAD_REQUEST") { }
    public BadRequestException(string message, string code = "BAD_REQUEST") : base(message, code) { }
}
