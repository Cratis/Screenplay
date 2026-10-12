// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool;

sealed class ModelCheckOptions
{
    internal string Target { get; set; } = string.Empty;
    internal string? Scope { get; set; }
    internal CompletenessChecks Checks { get; set; } = CompletenessChecks.None;
    internal string? OpenQuestions { get; set; }
    internal bool WarnAsError { get; set; }
    internal bool NoColor { get; set; }
    internal bool IsFile { get; set; }

    internal static ModelCheckOptions? Parse(string[] arguments, TextWriter error) => new ModelCheckArguments(arguments, error).Parse();
}

sealed class ModelCheckArguments(string[] arguments, TextWriter error)
{
    readonly ModelCheckOptions _options = new();
    readonly Dictionary<string, Func<ModelCheckArguments, bool>> _readers = new(StringComparer.Ordinal)
    {
        [CliCommandCatalog.Scope.Name] = reader => reader.Scope(),
        [CliCommandCatalog.Checks.Name] = reader => reader.Checks(),
        [CliCommandCatalog.OpenQuestions.Name] = reader => reader.OpenQuestions(),
        [CliCommandCatalog.WarnAsError.Name] = reader => reader.WarnAsError(),
        [CliCommandCatalog.NoColor.Name] = reader => reader.NoColor()
    };
    int _index;
    bool _hasTarget;

    internal ModelCheckOptions? Parse()
    {
        for (_index = 0; _index < arguments.Length; _index++)
        {
            if (!Read(arguments[_index])) return null;
        }

        if (!_hasTarget) _options.Target = Directory.GetCurrentDirectory();
        return _options;
    }

    bool Read(string argument) => _readers.TryGetValue(argument, out var read) ? read(this) : Target(argument);

    bool Target(string argument)
    {
        if (argument.StartsWith('-')) return Unexpected(argument);
        if (_hasTarget) return Unexpected(argument);
        _options.Target = argument;
        _hasTarget = true;
        return true;
    }

    bool Unexpected(string argument)
    {
        error.WriteLine($"Unexpected argument '{argument}'.");
        return false;
    }

    bool Scope()
    {
        const string message = "--scope requires one module, feature or slice address.";
        if (_options.Scope is not null) return Fail(message);
        if (!TakeValue(out var value)) return Fail(message);
        if (value.StartsWith('-')) return Fail(message);
        if (string.IsNullOrWhiteSpace(value)) return Fail(message);
        _options.Scope = value;
        return true;
    }

    bool Checks()
    {
        const string message = "--check requires recognized completeness check names, diagnostic codes, or all.";
        if (!TakeValue(out var value)) return Fail(message);
        if (!CompletenessChecks.TryParse(value, out var selected)) return Fail(message);
        _options.Checks = new(_options.Checks.Selected.Union(selected.Selected));
        return true;
    }

    bool OpenQuestions()
    {
        if (!TakeValue(out var value)) return Fail(ModelOpenQuestions.InvalidMode);
        if (!ModelOpenQuestions.TrySetMode(_options, value)) return Fail(ModelOpenQuestions.InvalidMode);
        return true;
    }

    bool TakeValue(out string value)
    {
        value = string.Empty;
        if (_index + 1 == arguments.Length) return false;
        value = arguments[++_index];
        return true;
    }

    bool WarnAsError()
    {
        _options.WarnAsError = true;
        return true;
    }

    bool NoColor()
    {
        _options.NoColor = true;
        return true;
    }

    bool Fail(string message)
    {
        error.WriteLine(message);
        return false;
    }
}
