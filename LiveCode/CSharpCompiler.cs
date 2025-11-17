using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Reflection.Metadata;

namespace LiveCode;

public class CSharpCompiler
{
    public LanguageVersion languageVersion = LanguageVersion.CSharp13;

    public string assemblyName;
    public string sourceCode;

    public (string filePath, string error) Compile(string filePath)
    {
        var sourceText = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, options);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references: GetReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
            )
        );

        var result = compilation.Emit(filePath);
        if (!result.Success)
        {
            return (
                null,
                string.Join("\n",
                    result.Diagnostics
                        .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error)
                        .Select(x => $"Line {x.Location.GetLineSpan().StartLinePosition.Line + 1} => {x.GetMessage()}")
                )
            );
        }

        return (filePath, null);
    }

    static List<PortableExecutableReference> references;

    static List<PortableExecutableReference> GetReferences()
    {
        if (references is null)
        {
            var assemblies = new Dictionary<string, Assembly>();

            AddAssembly<object>(assemblies);
            AddAssembly(assemblies, Assembly.GetEntryAssembly());

            var checkedAssemblies = new HashSet<string>();

            while (checkedAssemblies.Count < assemblies.Count)
            {
                var uncheckedAssemblies = assemblies
                    .Where(x => !checkedAssemblies.Contains(x.Key))
                    .ToList();

                foreach (var (assemblyName, assembly) in uncheckedAssemblies)
                {
                    checkedAssemblies.Add(assemblyName);
                    AddAssembly(assemblies, assembly);
                }
            }

            references = assemblies.Values
                .Select(CreatePortableExecutableReference)
                .ToList();
        }

        return references;
    }

    static void AddAssembly<T>(Dictionary<string, Assembly> assemblies)
    {
        AddAssembly(assemblies, typeof(T).Assembly);
    }

    static void AddAssembly(Dictionary<string, Assembly> assemblies, Assembly assembly)
    {
        if (assembly is null)
            return;

        assemblies.TryAdd(assembly.GetName().FullName, assembly);
        foreach (var name in assembly.GetReferencedAssemblies())
            assemblies.TryAdd(name.FullName, Assembly.Load(name));
    }

    unsafe static PortableExecutableReference CreatePortableExecutableReference(Assembly @this)
    {
        if (@this.TryGetRawMetadata(out byte* blob, out int length))
        {
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((nint) blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }
        return null;
    }
}
