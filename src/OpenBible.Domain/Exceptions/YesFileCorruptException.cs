namespace OpenBible.Domain.Exceptions;

public sealed class YesFileCorruptException : Exception
{
    public YesFileCorruptException(string message) : base(message)
    {
    }

    public YesFileCorruptException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
