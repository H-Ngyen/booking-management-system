namespace API.Exceptions;

public abstract class ApiException : Exception
{
    public abstract int StatusCode { get; }
    public string Code { get; }
    protected ApiException(string message, string code) : base(message) => Code = code;
}
