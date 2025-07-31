namespace Stockly.Exceptions;

public abstract class ApiApplicationException : Exception
{
    protected ApiApplicationException(string message, params (string Key, object Value)[] expectedValues)
        : base(message)
    {
        ExpectedValues = expectedValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public Dictionary<string, object>? ExpectedValues { get; }
}