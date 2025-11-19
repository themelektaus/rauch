using Microsoft.CodeAnalysis;
using System.Security.Cryptography;

namespace Rauch.Core;

/// <summary>
/// Loads and compiles plugin commands from .cs files at runtime
/// Caches compiled assemblies to improve loading performance
/// </summary>
public class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly string _cacheDirectory;
    private readonly ILogger _logger;

    static IEnumerable<string> EnumerateRequiredUsings()
    {
        // Returns usings from auto-generated Usings.g.cs (generated during build from Usings.cs)
        return Usings.Namespaces.Select(x => $"using {x};");
    }

    public PluginLoader(string pluginDirectory, ILogger logger = null)
    {
        _pluginDirectory = pluginDirectory;
        _cacheDirectory = Path.Combine(pluginDirectory, ".cache");
        _logger = logger;

        // Ensure cache directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <summary>
    /// Loads all .cs files from the plugin directory and compiles them into commands
    /// Uses cached assemblies when source hasn't changed
    /// Supports both root-level plugins and command groups
    /// </summary>
    List<ICommand> LoadPlugins(bool verboseLogging = false)
    {
        var commands = new List<ICommand>();

        if (!Directory.Exists(_pluginDirectory))
        {
            _logger?.Debug($"Plugin directory does not exist: {_pluginDirectory}");
            return commands;
        }

        var compilationInfo = new List<(string name, int commandCount, bool wasCompiled)>();

        // 1. Load root-level plugins (*.cs files in root)
        var rootFiles = Directory.GetFiles(_pluginDirectory, "*.cs", SearchOption.TopDirectoryOnly);
        foreach (var csFile in rootFiles)
        {
            try
            {
                var (pluginCommands, wasCompiled) = LoadPluginWithCache(csFile, verboseLogging);
                commands.AddRange(pluginCommands);
                compilationInfo.Add((
                    name: Path.GetFileNameWithoutExtension(csFile),
                    commandCount: pluginCommands.Count,
                    wasCompiled
                ));
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load plugin {Path.GetFileName(csFile)}: {ex.Message}");
            }
        }

        // 2. Load plugin groups
        var subdirectories = Directory.GetDirectories(_pluginDirectory)
            .Where(d => !d.EndsWith($"{Path.DirectorySeparatorChar}.cache"))
            .ToArray();

        foreach (var subdir in subdirectories)
        {
            try
            {
                // Load all .cs files in the group together
                var groupFiles = Directory.GetFiles(subdir, "*.cs", SearchOption.TopDirectoryOnly);
                var (groupCommands, wasCompiled) = LoadPluginGroupWithCache(subdir, groupFiles, verboseLogging);
                commands.AddRange(groupCommands);

                compilationInfo.Add((
                    name: Path.GetFileName(subdir),
                    commandCount: groupCommands.Select(x => x.GetCommandCount()).Sum(),
                    wasCompiled
                ));
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load plugin group {Path.GetFileName(subdir)}: {ex.Message}");
            }
        }

        // Show summary if verbose or if any plugin was compiled
        var needsCompilation = compilationInfo.Any(i => i.wasCompiled);
        if (verboseLogging || needsCompilation)
        {
            var totalFiles = rootFiles.Length + subdirectories.Length;
            var message = needsCompilation
                ? $"Found {totalFiles} file(s), compiling..."
                : $"Found {totalFiles} file(s), loading...";
            _logger?.Info(message);

            foreach (var info in compilationInfo)
            {
                if (info.wasCompiled || verboseLogging)
                {
                    _logger?.Success($"Loaded plugin: {info.name} ({info.commandCount} command(s))");
                }
            }
        }

        return commands;
    }

    public void LoadPluginsInto(List<ICommand> commands, bool verboseLogging = false)
    {
        var pluginCommands = LoadPlugins(verboseLogging);

        foreach (var pluginCommand in pluginCommands)
        {
            if (pluginCommand is ICommandGroup pluginCommandGroup)
            {
                var n = pluginCommand.GetType().Namespace.Split('.').Last();

                var commandGroup = commands.FirstOrDefault(c => c is ICommandGroup && c.GetType().Namespace.Split('.').Last() == n) as ICommandGroup;
                if (commandGroup is not null)
                {
                    commandGroup.AddSubCommandsFromOtherGroup(pluginCommandGroup);
                    continue;
                }
            }

            commands.Add(pluginCommand);
        }
    }

    /// <summary>
    /// Loads a plugin using cached DLL if available and source hasn't changed
    /// Returns tuple of (commands, wasCompiled)
    /// </summary>
    private (List<ICommand> commands, bool wasCompiled) LoadPluginWithCache(string csFilePath, bool verboseLogging)
    {
        var sourceCode = File.ReadAllText(csFilePath);
        var sourceHash = ComputeHash(sourceCode);
        var name = Path.GetFileNameWithoutExtension(csFilePath);
        var cachedDllPath = Path.Combine(_cacheDirectory, $"{name}.dll");
        var cachedHashPath = Path.Combine(_cacheDirectory, $"{name}.hash");

        // Check if cached version exists and is up-to-date
        if (File.Exists(cachedDllPath) && File.Exists(cachedHashPath))
        {
            var cachedHash = File.ReadAllText(cachedHashPath);
            if (cachedHash == sourceHash)
            {
                // Load from cache
                if (verboseLogging)
                {
                    _logger?.Debug($"Loading {name} from cache");
                }

                return (LoadFromAssembly<ICommand>(File.ReadAllBytes(cachedDllPath)), false);
            }
        }

        // Need to compile
        _logger?.Debug($"Compiling {name}...");

        var commands = CompileAndLoadPlugin(name, cachedDllPath, sourceCode);

        // Save hash for future comparisons
        File.WriteAllText(cachedHashPath, sourceHash);

        return (commands, true);
    }

    /// <summary>
    /// Loads a plugin group (multiple .cs files) using cached DLL if available
    /// Returns tuple of (commands, wasCompiled)
    /// </summary>
    private (List<ICommandGroup> commandGroups, bool wasCompiled) LoadPluginGroupWithCache(string groupDir, string[] csFiles, bool verboseLogging)
    {
        var groupName = Path.GetFileName(groupDir);
        var cachedDllPath = Path.Combine(_cacheDirectory, $"{groupName}.dll");
        var cachedHashPath = Path.Combine(_cacheDirectory, $"{groupName}.hash");

        // Compute combined hash of all source files
        var combinedSource = string.Join("\n", csFiles.Select(f => File.ReadAllText(f)));
        var sourceHash = ComputeHash(combinedSource);

        // Check if cached version exists and is up-to-date
        if (File.Exists(cachedDllPath) && File.Exists(cachedHashPath))
        {
            var cachedHash = File.ReadAllText(cachedHashPath);
            if (cachedHash == sourceHash)
            {
                // Load from cache
                if (verboseLogging)
                {
                    _logger?.Debug($"Loading {groupName} from cache");
                }

                return (LoadFromAssembly<ICommandGroup>(File.ReadAllBytes(cachedDllPath), isGroup: true), false);
            }
        }

        // Need to compile
        _logger?.Debug($"Compiling {groupName}...");

        var commands = CompileAndLoadPluginGroup(groupName, cachedDllPath, csFiles);

        // Save hash for future comparisons
        File.WriteAllText(cachedHashPath, sourceHash);

        return (commands, true);
    }

    /// <summary>
    /// Computes SHA256 hash of the source code
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Loads commands from a pre-compiled assembly
    /// Uses duck-typing to find types with ExecuteAsync method and CommandAttribute
    /// </summary>
    /// <param name="isGroup">If true, only loads the command group, not subcommands</param>
    private List<T> LoadFromAssembly<T>(byte[] rawAssembly, bool isGroup = false) where T : ICommand
    {
        var commands = new List<T>();

        var assembly = LiveCode.AssemblyReference.Create(rawAssembly).Assembly;

        // Find all types with [Command] attribute
        var commandTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract &&
            t.GetCustomAttributesData().Any(a => a.AttributeType.Name == "CommandAttribute")
        ).ToList();

        foreach (var type in commandTypes)
        {
            try
            {
                // For plugin groups, only load class marked with IsGroup = true
                if (isGroup)
                {
                    var commandAttr = type.GetCustomAttributesData()
                        .FirstOrDefault(a => a.AttributeType.Name == "CommandAttribute");

                    var isGroupAttr = commandAttr?.NamedArguments
                        .FirstOrDefault(a => a.MemberName == "IsGroup");

                    if (isGroupAttr == null || !(isGroupAttr.Value.TypedValue.Value is bool isGroupValue) || !isGroupValue)
                    {
                        // Skip non-group commands
                        continue;
                    }
                }

                // Check if it has ExecuteAsync method (duck-typing)
                var executeMethod = type.GetMethod("ExecuteAsync", new[]
                {
                    typeof(string[]),
                    typeof(IServiceProvider),
                    typeof(CancellationToken)
                });

                if (executeMethod != null)
                {
                    var instance = (T) Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        commands.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Could not instantiate {type.Name}: {ex.Message}");
            }
        }

        return commands;
    }

    /// <summary>
    /// Compiles a single .cs file and extracts ICommand implementations
    /// Saves the compiled assembly to disk for caching
    /// Automatically injects required using statements and duck-typed interfaces if missing
    /// </summary>
    private List<ICommand> CompileAndLoadPlugin(string name, string filePath, string sourceCode)
    {
        var commands = new List<ICommand>();

        // Inject required using statements if missing
        sourceCode = EnsureRequiredUsings(sourceCode);

        // Read source code
        var compiler = new LiveCode.CSharpCompiler
        {
            assemblyName = $"Plugin_{name}",
            sourceCode = sourceCode
        };
        var compilerResult = compiler.Compile(filePath);

        if (compilerResult.error is not null)
        {
            throw new InvalidOperationException($"Compilation failed:\n{compilerResult.error}");
        }

        return LoadFromAssembly<ICommand>(File.ReadAllBytes(filePath));
    }

    /// <summary>
    /// Compiles multiple .cs files (plugin group) and extracts ICommand implementations
    /// Saves the compiled assembly to disk for caching
    /// Automatically injects required using statements if missing
    /// Combines all files into a single namespace
    /// </summary>
    private List<ICommandGroup> CompileAndLoadPluginGroup(string groupName, string outputPath, string[] csFiles)
    {
        var allUsings = new HashSet<string>();
        var allClassDefinitions = new List<string>();
        string commonNamespace = null;

        // Process each file: extract usings, namespace, and class definitions
        foreach (var csFile in csFiles)
        {
            var source = File.ReadAllText(csFile);
            var lines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            var usings = new List<string>();
            var classContent = new List<string>();
            var insideNamespace = false;

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // Extract using statements (only before namespace declaration)
                if (!insideNamespace && trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    allUsings.Add(trimmed);
                    continue;
                }

                // Extract namespace
                if (trimmed.StartsWith("namespace "))
                {
                    if (commonNamespace == null)
                    {
                        commonNamespace = trimmed.TrimEnd(';');
                    }
                    insideNamespace = true;
                    continue;
                }

                // Skip empty lines before first content
                if (!insideNamespace && string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // Add class content (everything inside namespace)
                if (insideNamespace || !string.IsNullOrWhiteSpace(trimmed))
                {
                    classContent.Add(line);
                }
            }

            allClassDefinitions.Add(string.Join(Environment.NewLine, classContent));
        }

        // Add required usings
        foreach (var u in EnumerateRequiredUsings())
        {
            allUsings.Add(u);
        }

        // Build combined source
        var combinedSource = new System.Text.StringBuilder();

        // Add all usings
        foreach (var u in allUsings.OrderBy(x => x))
        {
            combinedSource.AppendLine(u);
        }
        combinedSource.AppendLine();

        // Add namespace with block-scoped syntax
        var namespaceName = (commonNamespace ?? "namespace Rauch.Plugins.Generated")
            .TrimEnd(';')
            .Replace("namespace ", "")
            .Trim();

        combinedSource.AppendLine($"namespace {namespaceName}");
        combinedSource.AppendLine("{");

        // Add all class definitions
        combinedSource.AppendLine(string.Join(Environment.NewLine + Environment.NewLine, allClassDefinitions));

        combinedSource.AppendLine("}");

        // Compile all files together
        var sourceCodeStr = combinedSource.ToString();

        // Debug: Save combined source for inspection
        var debugPath = Path.Combine(_cacheDirectory, $"{groupName}_debug.cs");
        File.WriteAllText(debugPath, sourceCodeStr);
        _logger?.Debug($"Saved combined source to: {debugPath}");

        var compiler = new LiveCode.CSharpCompiler
        {
            assemblyName = $"PluginGroup_{groupName}",
            sourceCode = sourceCodeStr
        };
        var compilerResult = compiler.Compile(outputPath);

        if (compilerResult.error is not null)
        {
            throw new InvalidOperationException($"Compilation failed:\n{compilerResult.error}");
        }

        return LoadFromAssembly<ICommandGroup>(File.ReadAllBytes(outputPath), isGroup: true);
    }

    /// <summary>
    /// Ensures that the required using statements and namespace are present in the source code
    /// Automatically injects missing using statements and namespace if needed
    /// Inserts usings BEFORE any namespace declaration
    /// </summary>
    private string EnsureRequiredUsings(string sourceCode)
    {
        var missingUsings = new List<string>();

        foreach (var usingStatement in EnumerateRequiredUsings())
        {
            if (!sourceCode.Contains(usingStatement))
            {
                missingUsings.Add(usingStatement);
            }
        }

        if (missingUsings.Count == 0)
        {
            return sourceCode;
        }

        _logger?.Debug($"Auto-injected {missingUsings.Count} missing using statement(s)");

        // Find namespace declaration
        var lines = sourceCode.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var namespaceIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("namespace "))
            {
                namespaceIndex = i;
                break;
            }
        }

        if (namespaceIndex >= 0)
        {
            // Insert usings before namespace
            var beforeNamespace = lines.Take(namespaceIndex).ToList();
            var fromNamespace = lines.Skip(namespaceIndex).ToList();

            beforeNamespace.AddRange(missingUsings);
            beforeNamespace.Add(""); // Empty line before namespace

            return string.Join(Environment.NewLine, beforeNamespace.Concat(fromNamespace));
        }

        // No namespace - prepend usings
        return string.Join(Environment.NewLine, missingUsings) + Environment.NewLine + Environment.NewLine + sourceCode;
    }
}
