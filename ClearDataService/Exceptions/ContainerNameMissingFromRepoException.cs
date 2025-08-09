using System.Text;

namespace ClearDataService.Exceptions;

public class ContainerNameMissingFromRepoException(params List<Type> repoTypes) 
    : BaseException(GetMessage(repoTypes))
{
    private static string GetMessage(List<Type> repoTypes)
    {
        StringBuilder sb = new();

        sb.Append("Container name is missing from the following repositories:");
        sb.Append(string.Join(",", repoTypes.Select(x => x.Name)));

        return sb.ToString();
    }
}