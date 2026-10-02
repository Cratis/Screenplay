// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // An instant binds to its round-trip UTC form, so the canonical bytes do not depend on how it was written.
        string? BindClock(SpecificationClockSyntax clock)
        {
            UsesV6 = true;
            if (!IsoInstant().IsMatch(clock.Instant) ||
                !DateTimeOffset.TryParse(clock.Instant, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Clock '{clock.Instant}' is not an instant with a time zone, such as \"2026-10-02T09:00:00Z\".", clock.Location);
                return null;
            }

            return instant.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        }

        SemanticSpecificationTrigger? BindSpecificationTrigger(SpecificationTriggerSyntax fired)
        {
            UsesV6 = true;
            if (_triggerDeclarations.TryGetValue(fired.Trigger, out var declared))
            {
                return new(SemanticReactionTriggerKind.ApplicationTrigger, BindPropertyValues(fired.Values, declared.Properties, "specification trigger"))
                {
                    Trigger = declared.Trigger.Id
                };
            }

            if (string.Equals(fired.Trigger, Startup, StringComparison.Ordinal) || string.Equals(fired.Trigger, Shutdown, StringComparison.Ordinal))
            {
                if (fired.Values.Any())
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"The built-in trigger '{fired.Trigger}' carries no values.", fired.Location);
                }

                return new(string.Equals(fired.Trigger, Startup, StringComparison.Ordinal) ? SemanticReactionTriggerKind.Startup : SemanticReactionTriggerKind.Shutdown, []);
            }

            Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification trigger '{fired.Trigger}' is neither a declared trigger, Startup nor Shutdown.", fired.Location);
            return null;
        }

        SemanticSpecificationCapture? BindSpecificationCapture(SpecificationCaptureSyntax presented)
        {
            UsesV6 = true;
            var captures = AllSlices()
                .SelectMany(entry => entry.Slice.Captures
                    .Where(capture => capture.Name == ShortName(presented.Capture))
                    .Select(capture => SemanticAddress.ForCapture(SemanticAddress.ForSlice(_applicationIdentity, entry.Module, entry.FeaturePath, entry.Slice.Name), capture.Name)))
                .ToArray();
            if (captures.Length != 1)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification capture '{presented.Capture}' does not name one declared capture.", presented.Location);
                return null;
            }

            var record = BindCaptureRecord(presented.Record.Select(value => (value.Property, value.Source, value.Location)));
            return record is null ? null : new(documents.IdentityCatalog.ResolveSemanticAssignment(captures[0]).Id, record);
        }

        SemanticCaptureRecord? BindCaptureRecord(IEnumerable<(string Name, ExpressionSyntax Value, SourceLocation Location)> fields)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticCaptureField>();
            var valid = true;
            foreach (var (name, value, location) in fields)
            {
                var field = BindCaptureField(name, value, location);
                if (field is null)
                {
                    valid = false;
                    continue;
                }

                bound.Add(field);
            }

            return valid ? new(bound.ToImmutable()) : null;
        }

        SemanticCaptureField? BindCaptureField(string name, ExpressionSyntax value, SourceLocation location)
        {
            switch (value)
            {
                case LiteralExpressionSyntax literal:
                    return new(name, SemanticCaptureFieldKind.Value) { Value = BindLiteral(literal) };
                case ObjectExpressionSyntax record when BindCaptureRecord(record.Members.Select(member => (member.Name, member.Value, member.Location))) is { } nested:
                    return new(name, SemanticCaptureFieldKind.Record) { Record = nested };
                case ListExpressionSyntax list when list.Items.All(item => item is ObjectExpressionSyntax):
                    var records = list.Items.Cast<ObjectExpressionSyntax>()
                        .Select(item => BindCaptureRecord(item.Members.Select(member => (member.Name, member.Value, member.Location))))
                        .ToArray();
                    return records.All(item => item is not null)
                        ? new(name, SemanticCaptureFieldKind.Records) { Records = [.. records.Select(item => item!)] }
                        : null;
                case ObjectExpressionSyntax:
                    return null;
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture record field '{name}' must be a literal, an object of fields or a list of objects.", location);
                    return null;
            }
        }
    }
}
