// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_typed_read_path_arguments : given.a_protected_read_model
{
    [Fact]
    void should_name_the_command_and_path_of_an_unknown_read_path()
    {
        var diagnostic = Bind(Source.Replace("View.value", "unknown.value", StringComparison.Ordinal)).Diagnostics.First(value => value.Code == DiagnosticCodes.UnknownReadPath);
        diagnostic.Arguments.Count.ShouldEqual(3);
        diagnostic.Arguments["path"].ShouldEqual(DiagnosticArgument.Text("unknown.value"));
        diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("unknown"));
        diagnostic.Arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
        diagnostic.Message.ShouldEqual("Read path 'unknown.value' does not resolve to a declared read and view property.");
    }

    [Fact]
    void should_name_the_command_path_and_read_of_an_ambiguous_read_path()
    {
        var diagnostic = Bind(Source.Replace("reads View by id", "reads View as first by id\n        reads View as second by id", StringComparison.Ordinal)).Diagnostics.First(value => value.Code == DiagnosticCodes.AmbiguousReadPath);
        diagnostic.Arguments.Count.ShouldEqual(3);
        diagnostic.Arguments["path"].ShouldEqual(DiagnosticArgument.Text("View.value"));
        diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("View"));
        diagnostic.Arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
        diagnostic.Message.ShouldEqual("Read path 'View.value' must use an alias because 'View' is read more than once.");
    }
}
