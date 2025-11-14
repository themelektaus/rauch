using Microsoft.CodeAnalysis;
using Rauch.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    /// </summary>
    public List<ICommand> LoadPlugins(bool verboseLogging = false)
    {
        var commands = new List<ICommand>();

        if (!Directory.Exists(_pluginDirectory))
        {
            _logger?.Debug($"Plugin directory does not exist: {_pluginDirectory}");
            return commands;
        }

        var csFiles = Directory.GetFiles(_pluginDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.cache{Path.DirectorySeparatorChar}"))
            .ToArray();

        if (csFiles.Length == 0)
        {
            _logger?.Debug($"No plugin files found in: {_pluginDirectory}");
            return commands;
        }

        var compilationInfo = new List<(string fileName, int commandCount, bool wasCompiled)>();

        foreach (var csFile in csFiles)
        {
            try
            {
                var (pluginCommands, wasCompiled) = LoadPluginWithCache(csFile, verboseLogging);
                commands.AddRange(pluginCommands);
                compilationInfo.Add((Path.GetFileName(csFile), pluginCommands.Count, wasCompiled));
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load plugin {Path.GetFileName(csFile)}: {ex.Message}");
            }
        }

        // Show summary if verbose or if any plugin was compiled
        var needsCompilation = compilationInfo.Any(i => i.wasCompiled);
        if (verboseLogging || needsCompilation)
        {
            var message = needsCompilation
                ? $"Found {csFiles.Length} plugin file(s), compiling..."
                : $"Found {csFiles.Length} plugin file(s), loading...";
            _logger?.Info(message);

            foreach (var info in compilationInfo)
            {
                if (info.wasCompiled || verboseLogging)
                {
                    _logger?.Success($"Loaded plugin: {info.fileName} ({info.commandCount} command(s))");
                }
            }
        }

        return commands;
    }

    /// <summary>
    /// Loads a plugin using cached DLL if available and source hasn't changed
    /// Returns tuple of (commands, wasCompiled)
    /// </summary>
    private (List<ICommand> commands, bool wasCompiled) LoadPluginWithCache(string csFilePath, bool verboseLogging)
    {
        var sourceCode = File.ReadAllText(csFilePath);
        var sourceHash = ComputeHash(sourceCode);
        var fileName = Path.GetFileNameWithoutExtension(csFilePath);
        var cachedDllPath = Path.Combine(_cacheDirectory, $"{fileName}.dll");
        var cachedHashPath = Path.Combine(_cacheDirectory, $"{fileName}.hash");

        // Check if cached version exists and is up-to-date
        if (File.Exists(cachedDllPath) && File.Exists(cachedHashPath))
        {
            var cachedHash = File.ReadAllText(cachedHashPath);
            if (cachedHash == sourceHash)
            {
                // Load from cache
                if (verboseLogging)
                {
                    _logger?.Debug($"Loading {fileName} from cache");
                }
                return (LoadFromAssembly(AssemblyLoadContext.Default.LoadFromAssemblyPath(cachedDllPath)), false);
            }
        }

        // Need to compile
        _logger?.Debug($"Compiling {fileName}...");

        var commands = CompileAndLoadPlugin(cachedDllPath, sourceCode);

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
    /// </summary>
    private List<ICommand> LoadFromAssembly(Assembly assembly)
    {
        var commands = new List<ICommand>();

        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) &&
                       !t.IsInterface &&
                       !t.IsAbstract)
            .ToList();

        foreach (var type in commandTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(type) as ICommand;
                if (instance != null)
                {
                    commands.Add(instance);
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
    /// Automatically injects required using statements if missing
    /// </summary>
    private List<ICommand> CompileAndLoadPlugin(string filePath, string sourceCode)
    {
        var commands = new List<ICommand>();

        // Inject required using statements if missing
        sourceCode = EnsureRequiredUsings(sourceCode);

        // Read source code
        var compiler = new LiveCode.CSharpCompiler { sourceCode = sourceCode };
        var compilerResult = compiler.Compile(filePath);

        if (compilerResult.HasErrors())
        {
            var errors = string.Join("\n", compilerResult.errors.Select(x => $"[{x.line}] {x.message}"));
            throw new InvalidOperationException($"Compilation failed:\n{errors}");
        }

        var assemblyReference = LiveCode.AssemblyReference.Create(File.ReadAllBytes(filePath));
        return LoadFromAssembly(assemblyReference.Assembly);
    }

    /// <summary>
    /// Ensures that the required using statements and namespace are present in the source code
    /// Automatically injects missing using statements and namespace if needed
    /// </summary>
    private string EnsureRequiredUsings(string sourceCode)
    {
        var requiredUsings = new[]
        {
            "using System;",
            "using System.Threading;",
            "using System.Threading.Tasks;",
            "using Rauch.Commands;",
            "using Rauch.Core;",
            "using Rauch.Core.Attributes;"
        };

        var missingUsings = new List<string>();

        foreach (var usingStatement in requiredUsings)
        {
            if (!sourceCode.Contains(usingStatement))
            {
                missingUsings.Add(usingStatement);
            }
        }

        // Check if namespace is missing
        bool needsNamespace = !sourceCode.Contains("namespace Rauch.Plugins") &&
                              !sourceCode.Contains("namespace ") &&
                              !sourceCode.Contains("namespace\r") &&
                              !sourceCode.Contains("namespace\n");

        // Build the injection block
        var injectionParts = new List<string>();

        if (missingUsings.Count > 0)
        {
            injectionParts.Add(string.Join(Environment.NewLine, missingUsings));
            _logger?.Debug($"Auto-injected {missingUsings.Count} missing using statement(s)");
        }

        if (needsNamespace)
        {
            if (injectionParts.Count > 0)
            {
                injectionParts.Add(""); // Empty line between usings and namespace
            }
            injectionParts.Add("namespace Rauch.Plugins;");
            injectionParts.Add(""); // Empty line after namespace
            _logger?.Debug("Auto-injected namespace Rauch.Plugins");
        }

        // If there are any injections, prepend them to the source code
        if (injectionParts.Count > 0)
        {
            var injectionBlock = string.Join(Environment.NewLine, injectionParts) + Environment.NewLine;
            sourceCode = injectionBlock + sourceCode;
        }

        return sourceCode;
    }
}
