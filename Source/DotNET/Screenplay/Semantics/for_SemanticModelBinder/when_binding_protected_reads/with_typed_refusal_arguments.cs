// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_typed_refusal_arguments : given.a_protected_read_model
{
    const string ReducerBody = "      reducer Reduce => View\n        on Recorded\n          ```csharp\n          return context.State;\n          ```";

    [Theory]
    [InlineData("reducer", SemanticDecisionReadRefusal.Reducer)]
    [InlineData("join", SemanticDecisionReadRefusal.Join)]
    [InlineData("removal", SemanticDecisionReadRefusal.NoRemoval)]
    [InlineData("key type", SemanticDecisionReadRefusal.KeyType)]
    [InlineData("routed source", SemanticDecisionReadRefusal.NamedSourceOrStream)]
    void should_name_the_command_read_view_and_reason(string group, SemanticDecisionReadRefusal reason)
    {
        var diagnostic = Bind(Variant(group)).Diagnostics.Single(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead);
        diagnostic.Arguments.Count.ShouldEqual(4);
        diagnostic.Arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
        diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("View"));
        diagnostic.Arguments["readModel"].ShouldEqual(DiagnosticArgument.Declaration("readmodel", ["M", "F", "Lookup", "View"]));
        diagnostic.Arguments["reason"].ShouldEqual(DiagnosticArgument.Keyword(reason.ToString()));
        diagnostic.Message.ShouldEqual($"Read 'View' cannot be protected: {reason}.");
    }

    [Fact]
    void should_name_the_alias_as_the_read()
    {
        var diagnostic = Bind(Source.Replace("reads View by id", "reads View as current by id", StringComparison.Ordinal).Replace("id String", "id Int", StringComparison.Ordinal).Replace("View.value", "current.value", StringComparison.Ordinal))
            .Diagnostics.Single(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead);
        diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("current"));
        diagnostic.Arguments["readModel"].ShouldEqual(DiagnosticArgument.Declaration("readmodel", ["M", "F", "Lookup", "View"]));
    }

    static string Variant(string group) => group switch
    {
        "reducer" => Source.Replace("      projection Build => View\n        from Recorded\n          id = $eventSourceId\n          value = value\n        remove with Removed", ReducerBody, StringComparison.Ordinal),
        "join" => Source.Replace("      event Removed", "      event Removed\n      event JoinRemoved", StringComparison.Ordinal).Replace("        remove with Removed", "        remove with Removed\n        remove via join on JoinRemoved", StringComparison.Ordinal),
        "removal" => Source.Replace("        remove with Removed\n", string.Empty, StringComparison.Ordinal),
        "key type" => Source.Replace("id String", "id Int", StringComparison.Ordinal),
        _ => Source.Replace("projection Build => View", "projection Build => View\n        sequence other", StringComparison.Ordinal)
    };
}
