namespace Clear.DataService.Exceptions;

public class CosmosJsonSerializerNullException : BaseException
{
    public CosmosJsonSerializerNullException() : base($"Json serializer for cosmos db is null")
    { }
}