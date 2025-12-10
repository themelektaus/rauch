using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rauch.Commands;

[Name("help")]
[Description("Show help text")]
public class Help(IEnumerable<ICommand> availableCommands) : ICommand
{
    readonly IEnumerable<ICommand> _availableCommands = availableCommands;

    public enum Match { None, Any, All }

    public class CommandInfo
    {
        public string name;
        public string description;
        public bool isPlugin;
        public Match match;
        public CommandInfo parent;
        public List<CommandInfo> children;
    }

    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        WriteTitleLine(logger);

        var commandInfos = GetFilteredCommandInfos(args);

        if (commandInfos.Count == 0)
        {
            logger?.Warning("No commands found matching the specified search terms.");
            return Task.CompletedTask;
        }

        foreach (var commandInfo in commandInfos)
        {
            logger?.Write($"  {commandInfo.name,-15} ", newLine: false, color: ConsoleColor.Yellow);
            logger?.Write(commandInfo.description);

            foreach (var command in commandInfo.children ?? [])
            {
                logger?.Write($"    └─ ", newLine: false);
                logger?.Write($"{command.name,-13} ", newLine: false, color: ConsoleColor.DarkYellow);
                logger?.Write(command.description);
            }

            logger?.Write();
        }

        return Task.CompletedTask;
    }

    public static void WriteTitleLine(ILogger logger)
    {
        SoundPlayer.Play("Help");
        logger?.Write();
        logger?.Write(" >_ ", newLine: false, color: ConsoleColor.DarkCyan);
        logger?.Write("rauch", color: ConsoleColor.Cyan);
        logger?.Write();

    }

    public List<CommandInfo> GetCommandInfos(string[] args)
    {
        var commandInfos = new List<CommandInfo>();

        foreach (var command in _availableCommands)
        {
            var groupName = CommandLoader.GetGroupName(command);

            if (groupName is null)
            {
                continue;
            }

            var groupInfo = commandInfos.FirstOrDefault(g => g.name == groupName);

            if (groupInfo is null)
            {
                groupInfo = new()
                {
                    name = groupName,
                    description = "",
                    isPlugin = CommandLoader.IsPlugin(command),
                    match = Filter(args, groupName),
                    children = []
                };

                commandInfos.Add(groupInfo);
            }

            var metadata = CommandMetadata.Get(command);

            groupInfo.children.Add(new()
            {
                name = metadata.Name,
                description = metadata.Description,
                isPlugin = CommandLoader.IsPlugin(command),
                match = Filter(args, metadata.Keywords),
                parent = groupInfo
            });
        }

        foreach (var command in _availableCommands)
        {
            var groupName = CommandLoader.GetGroupName(command);

            if (groupName is not null)
            {
                continue;
            }

            var metadata = CommandMetadata.Get(command);

            var commandInfo = commandInfos.FirstOrDefault(g => g.name == metadata.Name);

            if (commandInfo is null)
            {
                commandInfo = new()
                {
                    name = metadata.Name,
                    description = metadata.Description,
                    isPlugin = CommandLoader.IsPlugin(command),
                    match = Filter(args, metadata.Name)
                };

                commandInfos.Add(commandInfo);
            }
        }

        return commandInfos;
    }

    public List<CommandInfo> GetFilteredCommandInfos(string[] args)
    {
        

        var commandInfos = GetCommandInfos(args);
        
        var childrenEnumeration = commandInfos
            .Where(x => x.match == Match.All)
            .SelectMany(x => x.children ?? [x]);

        childrenEnumeration = childrenEnumeration.Concat(
            commandInfos
                .SelectMany(x => x.children ?? [])
                .Where(x => (x.parent.match != Match.None && x.match != Match.None) || x.match == Match.All)
        );

        var children = childrenEnumeration.ToList();

        var filteredCommandInfos = new List<CommandInfo>();

        foreach (var child in children)
        {
            if (child.parent is null)
            {
                filteredCommandInfos.Add(child);
            }
        }

        var groups = children.Select(x => x.parent).Where(x => x is not null).Distinct().ToList();

        foreach (var group in groups)
        {
            filteredCommandInfos.Add(group);
            group.children.RemoveAll(x => !children.Contains(x));
        }

        return filteredCommandInfos;
    }

    static List<string> GetSearchTerms(string[] args)
    {
        return [.. args.Where(x => !string.IsNullOrWhiteSpace(x))];
    }

    static Match Filter(string[] args, string term)
    {
        var searchTerms = GetSearchTerms(args);

        if (searchTerms.Count == 0 || searchTerms.All(x => term.Contains(x, StringComparison.OrdinalIgnoreCase)))
        {
            return Match.All;
        }

        if (searchTerms.Any(x => term.Contains(x, StringComparison.OrdinalIgnoreCase)))
        {
            return Match.Any;
        }

        return Match.None;
    }
}
