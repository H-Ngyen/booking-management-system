namespace API.Exceptions;

public class BadRequestException : ApiException
{
    public override int StatusCode => 400;
    public BadRequestException() : base("Invalid request") { }
    public BadRequestException(string message) : base(message) { }
}