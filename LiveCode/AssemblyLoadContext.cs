using System.Reflection;

namespace LiveCode;

public sealed class AssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
{
    public AssemblyLoadContext() : base(true)
    {

    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }
}
