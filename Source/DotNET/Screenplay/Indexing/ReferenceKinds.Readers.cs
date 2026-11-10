// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Indexing;

internal static partial class ReferenceKinds
{
    static IEnumerable<(string Name, string[] Kinds, string Role)> NamedType(string name, string[] kinds, string role) =>
        ConceptSyntax.PrimitiveTypes.Contains(name, StringComparer.Ordinal) ? [] : One(name, kinds, role);

    static IEnumerable<(string Name, string[] Kinds, string Role)> TypeReference(TypeRefSyntax value, SyntaxNode? owner)
    {
        (string[] Kinds, string Role) reference = owner switch
        {
            IdentityDetailSyntax => (["ReadModel", "Concept", "Type"], "identityDetailType"),
            QuerySyntax query when ReferenceEquals(query.ReturnType, value) => (["ReadModel", "Concept", "Type"], "queryResult"),
            ScreenSyntax => (["ReadModel"], "dataReadModel"),
            _ => (["Concept", "Type"], "type")
        };
        return NamedType(value.Name, reference.Kinds, reference.Role);
    }

    static IEnumerable<(string Name, string[] Kinds, string Role)> Production(ProducesSyntax value, SyntaxNode? owner)
    {
        if (value.InlineOperation is not null) yield return (value.Event, ["Operation"], "declares");
        else if (value.InlineEvent is not null) yield return (value.Event, ["Event"], "declares");
        yield return (value.Event, ["Event", "Operation"], "produces");
    }

    static IEnumerable<(string Name, string[] Kinds, string Role)> CaptureSetting(CaptureSourceSettingSyntax value, SyntaxNode? owner) =>
        value.Name == CaptureEventsSource.FromSetting && owner is CaptureSyntax { Source: { } source } && CaptureEventsSource.IsEvents(source)
            ? One(value.Value, ["Event"], "captureFrom") : [];

    static IEnumerable<(string Name, string[] Kinds, string Role)> Interaction(string target, string kind, string role, SyntaxNode? owner) =>
        owner is BehaviorSyntax behavior && behavior.Parameters.Any(parameter => parameter.Name == target) ? [] : One(target, [kind], role);

    static IEnumerable<(string Name, string[] Kinds, string Role)> LifecycleExpectation(LifecycleExpectationSyntax value, SyntaxNode? owner) => value.Expectation switch
    {
        LifecycleExpectationKind.Sends => One(value.Target, ["Command"], "lifecycleSends"),
        LifecycleExpectationKind.Starts => One(value.Target, ["Workflow"], "lifecycleStarts"),
        _ => [] // State and node names are local to the lifecycle, not declaration references.
    };

    static IEnumerable<(string Name, string[] Kinds, string Role)> ToolbarTarget(ToolbarItemSyntax value, SyntaxNode? owner) => value.Kind switch
    {
        ToolbarItemKind.Action => One(value.Target, ["Command", "Behavior"], "toolbarAction"),
        ToolbarItemKind.Navigate => One(value.Target, ["Screen"], "toolbarNavigate"),
        ToolbarItemKind.Dialog => One(value.Target, ["DialogTemplate"], "toolbarDialog"),
        _ => One(value.Target, ["Command", "Behavior", "Screen", "DialogTemplate"], "toolbar")
    };
}
