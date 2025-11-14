using System.Reflection;

namespace LiveCode;

public class AssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
{
    public AssemblyLoadContext() : base(true)
    {

    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }
}
