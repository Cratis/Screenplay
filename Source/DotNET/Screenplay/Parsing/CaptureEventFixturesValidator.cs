// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Parsing;

internal static class CaptureEventFixturesValidator
{
    internal static void Validate(ApplicationSyntax application, SliceSyntax slice, DeclarationScope scope, AuthoringProductionResolver events, ParserContext context)
    {
        var captures = slice.Captures.Where(capture => capture.Source is { } source && CaptureEventsSource.IsEvents(source)).ToArray();
        foreach (var capture in captures)
        {
            if (capture.Key?.StartsWith('$') == true)
            {
                context.Error(DiagnosticCodes.CaptureEventsKeyNotRequired, $"Capture '{capture.Name}' key '{capture.Key}' must be a required property of every source event; context keys are not supported for captures over events.", capture.Location);
                continue;
            }
            foreach (var setting in CaptureEventsSource.Events(capture.Source!))
            {
                if (events.Resolve(setting.Value, scope.Segments).Declaration?.Node is not EventSyntax { Visibility: EventVisibility.Public, Origin: not null } contract) continue;
                if (contract.Properties.Any(property => string.Equals(property.Name, capture.Key, StringComparison.Ordinal) && !property.Type.IsOptional)) continue;
                context.Error(DiagnosticCodes.CaptureEventsKeyNotRequired, $"Capture '{capture.Name}' key '{capture.Key}' must be a required property of every source event; '{contract.Name}' does not declare it as required.", capture.Location);
                break;
            }
        }
        var declarations = ScreenplayValidator.ScopedSlices(application).SelectMany(item => item.Slice.Captures).ToArray();
        foreach (var specification in slice.Specifications)
        {
            var records = specification.GivenCaptures.Concat(specification.WhenCapture is { } when ? [when] : []);
            foreach (var record in records)
            {
                var matches = declarations.Where(capture => string.Equals(capture.Name, record.Capture.Split('.').Last(), StringComparison.Ordinal)).ToArray();
                if (matches is not [var matched] || matched.Source is not { } source || !CaptureEventsSource.IsEvents(source)) continue;
                context.Error(DiagnosticCodes.CaptureEventsRecordFixture, $"Capture '{record.Capture}' consumes events; use 'given <Event>' or 'when append <Event>' rather than a capture record.", record.Location);
            }
        }
    }
}
