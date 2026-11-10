// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static string FixtureEventSourceMessage(string? eventName) => eventName is null
            ? "An event-source assertion needs one unambiguous required scalar command destination type."
            : $"Event '{eventName}' needs one unambiguous required scalar event-source identity type. Declare a read-model projection keyed on the event source ($eventSourceId) or a producing command with a required scalar destination.";

        SemanticTypeReference? FixtureEventSourceType(SemanticTypeReference? producerType, ExpressionSyntax? source, string eventName) =>
            source is null ? producerType : producerType ?? ProjectedEventSourceType(eventName);

        SemanticTypeReference? ProjectedEventSourceType(string eventName) =>
            ProjectedEventSourceIdentity.TypeFor(syntax, eventName) is { } type ? BindTypeReference(type) : null;
    }
}
