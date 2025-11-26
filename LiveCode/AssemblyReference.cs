using System.Reflection;
using System.Runtime.CompilerServices;

namespace LiveCode;

public sealed class AssemblyReference
{
    public Assembly Assembly { get; init; }
    public WeakReference Reference { get; init; }

    AssemblyReference()
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static AssemblyReference Create(byte[] rawAssembly)
    {
        using var stream = new MemoryStream(rawAssembly);

        var context = new AssemblyLoadContext();

        return new()
        {
            Assembly = context.LoadFromStream(stream),
            Reference = new WeakReference(context)
        };
    }
}
