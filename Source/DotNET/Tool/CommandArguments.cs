// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool;

// Splits arguments into positional values and the valued options a command declares; each option at most once.
sealed record CommandArguments(IReadOnlyList<string> Positional, IReadOnlyDictionary<string, string> Options)
{
    internal string? Option(CliOption option) => Options.TryGetValue(option.Name, out var value) ? value : null;

    internal static CommandArguments? Parse(string[] args, CliCommand command)
    {
        var positional = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(argument);
                continue;
            }

            if (!command.Options.Any(option => option.Name == argument) || options.ContainsKey(argument) ||
                ++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(args[index]))
            {
                return null;
            }

            options[argument] = args[index];
        }

        return new(positional, options);
    }
}
