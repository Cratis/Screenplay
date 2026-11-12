// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    static readonly HashSet<string> _operationInputKeywords = new(StringComparer.Ordinal) { "uses", "description", "for", "tag", "generation", "origin", "documentation", "namespace", "sequence", "correlation", "causation", "causedBy", "occurred" };

    static readonly HashSet<string> _abilityInputKeywords = new(StringComparer.Ordinal) { "description", "outcome", "implementation", "file" };

    static readonly HashSet<string> _emptyKeywords = new(StringComparer.Ordinal);

    void WriteSystem(ScreenplayWriter writer, SystemSyntax system)
    {
        using var anchor = writer.Anchor(system);
        writer.Line($"system {system.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, system.Description, system);
            if (system.Extension is { } extension) writer.Line($"extension {StringLiteral.Quote(extension.Reference)}{(extension.Version is null ? string.Empty : $" version {extension.Version}")}", extension);
            foreach (var setting in system.Settings) WriteSystemSetting(writer, setting);
            if (system.Activation is { } activation) WriteSystemActivation(writer, activation);
            foreach (var ability in system.Abilities) WriteAbility(writer, ability);
        }
    }

    void WriteSecret(ScreenplayWriter writer, SecretSyntax secret)
    {
        using var anchor = writer.Anchor(secret);
        writer.Line($"secret {secret.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, secret.Description, secret);
        }
    }

    void WriteSystemSetting(ScreenplayWriter writer, SystemSettingSyntax setting)
    {
        if (setting.Secret is not null)
        {
            writer.Line($"setting {setting.Name} from secret {setting.Secret}", setting);
            return;
        }

        var value = string.Equals(setting.ValueKind, "string", StringComparison.Ordinal) ? StringLiteral.Quote(setting.Value ?? string.Empty) : setting.Value;
        writer.Line($"setting {setting.Name} = {value}", setting);
    }

    void WriteSystemActivation(ScreenplayWriter writer, SystemActivationSyntax activation)
    {
        if (activation.Always)
        {
            writer.Line("activation always", activation);
            return;
        }

        var idle = activation.IdleAmount is { } amount ? $" idle after {amount} {activation.IdleUnit}{(amount == 1 ? string.Empty : "s")}" : string.Empty;
        writer.Line($"activation on demand{idle}", activation);
    }

    void WriteAbility(ScreenplayWriter writer, AbilitySyntax ability)
    {
        using var anchor = writer.Anchor(ability);
        writer.Line(ability.EffectClass is null ? $"ability {ability.Name}" : $"ability {ability.Name} {ability.EffectClass}");
        using (writer.Indent())
        {
            WriteDescription(writer, ability.Description, ability);
            if (ability.AnswersLater) writer.DirectiveLine("answers later", ability, "answersLater");
            WriteProperties(writer, ability.Inputs, _abilityInputKeywords);
            if (ability.Returns.Any())
            {
                writer.Line("returns");
                using (writer.Indent()) WriteProperties(writer, ability.Returns, _emptyKeywords);
            }
            foreach (var outcome in ability.Outcomes)
            {
                using var outcomeAnchor = writer.Anchor(outcome);
                writer.Line($"outcome {outcome.Name}");
                using (writer.Indent()) WriteDescription(writer, outcome.Description, outcome);
            }
            WriteAbilityImplementation(writer, ability);
        }
    }

    void WriteAbilityImplementation(ScreenplayWriter writer, AbilitySyntax ability)
    {
        if (ability.Implementation is { } implementation)
        {
            using var implementationAnchor = writer.Anchor(implementation);
            writer.Line("implementation");
            using (writer.Indent())
            {
                foreach (var hint in implementation.Hints)
                {
                    ImplementationInvariants.Validate(hint);
                    writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
                }
                WriteFile(writer, ability.File);
                if (ability.Code is not null) WriteCodeBlock(writer, ability.Code);
            }
        }
        else
        {
            WriteFile(writer, ability.File);
            if (ability.Code is not null) WriteCodeBlock(writer, ability.Code);
        }
    }

    void WriteOperation(ScreenplayWriter writer, OperationSyntax operation)
    {
        using var anchor = writer.Anchor(operation);
        writer.Line($"operation {operation.Name}");
        using (writer.Indent()) WriteOperationBody(writer, operation);
    }

    void WriteOperationBody(ScreenplayWriter writer, OperationSyntax operation, IEnumerable<PropertyMappingSyntax>? mappings = null)
    {
        using var anchor = writer.Anchor(operation);
        WriteDescription(writer, operation.Description, operation);
        writer.DirectiveLine($"uses {operation.Uses}", operation, "uses");
        if (mappings is null)
        {
            WriteProperties(writer, operation.Inputs, _operationInputKeywords);
        }
        else
        {
            foreach (var (input, mapping) in operation.Inputs.Zip(mappings))
            {
                using var inputAnchor = writer.Anchor(input);
                writer.Assignment($"{ReservedWords.Escape(input.Name, _operationInputKeywords)} {ScreenplaySyntaxText.TypeRef(input.Type)}", mapping.Source, mapping);
            }
        }
        if (operation.Execute is not null) WriteOperationPhase(writer, "execute", operation.Execute);
        if (operation.Compensate is not null) WriteOperationPhase(writer, "compensate", operation.Compensate);
    }

    void WriteOperationPhase(ScreenplayWriter writer, string name, OperationPhaseSyntax phase)
    {
        OperationInvariants.Validate(phase);
        using var anchor = writer.Anchor(phase);
        writer.Line(name);
        using (writer.Indent())
        {
            WriteDescription(writer, phase.Description, phase);
            foreach (var invocation in phase.Invocations) WriteInvocation(writer, invocation);
            if (phase.Implementation is { } implementation)
            {
                using var implementationAnchor = writer.Anchor(implementation);
                writer.Line("implementation");
                using (writer.Indent())
                {
                    foreach (var hint in implementation.Hints)
                    {
                        ImplementationInvariants.Validate(hint);
                        writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
                    }
                    WriteFile(writer, phase.File);
                    if (phase.Code is not null) WriteCodeBlock(writer, phase.Code);
                }
            }
            else
            {
                WriteFile(writer, phase.File);
                if (phase.Code is not null) WriteCodeBlock(writer, phase.Code);
            }
        }
    }
}
