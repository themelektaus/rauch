using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Standalone;

[Command("sum", "Adds the specified numbers", Parameters = "<number1> <number2> ...")]
[MinArguments(1)]
[NumericArguments]
public class Sum : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();
        int sum = 0;
        bool hasError = false;

        foreach (var arg in args)
        {
            if (int.TryParse(arg, out int number))
            {
                sum += number;
            }
            else
            {
                logger?.Error($"Error: '{arg}' is not a valid number.");
                hasError = true;
            }
        }

        if (!hasError)
        {
            logger?.Success(sum.ToString());
        }

        return Task.CompletedTask;
    }
}
