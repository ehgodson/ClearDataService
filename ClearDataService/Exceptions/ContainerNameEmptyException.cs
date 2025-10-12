namespace Clear.DataService.Exceptions;

public class ContainerNameEmptyException : BaseException
{
    public ContainerNameEmptyException() : base("Container name cannot be empty")
    { }
}