// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_refused_read_path_positions : given.a_protected_read_model
{
    [Fact]
    void should_refuse_read_paths_in_produces_when()
    {
        var result = Bind(Source.Replace("        produces Recorded\n          for id\n          value = View.value", "        produces when View.value != \"\"\n          Recorded\n            for id\n            value = value", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("produces when", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_read_paths_in_destinations()
    {
        var result = Bind(Source.Replace("for id", "for View.id", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("destination", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_read_paths_in_response_mappings()
    {
        var result = BindCommandSyntax(command => command with
        {
            Response = new RecordCommandResponseSyntax([new ResponseFieldSyntax("snapshot", null, new("View.value", command.Location), command.Location)], command.Location)
        });
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("response mappings", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_read_paths_in_stream_id_mappings()
    {
        var source = "eventsource Account\n  identifier String\n  stream Ledger\n    streamId String\n" + Source.Replace("reads View by id", "stream Account.Ledger\n          streamId = View.value\n        reads View by id", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("Stream id mapping", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_deeper_read_paths()
    {
        var result = Bind(Source.Replace("View.value", "View.nested.value", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("exactly one", StringComparison.Ordinal));
    }
}
