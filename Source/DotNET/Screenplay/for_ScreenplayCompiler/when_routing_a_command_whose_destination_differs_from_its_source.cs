// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_routing_a_command_whose_destination_differs_from_its_source : given.a_compiler
{
    const string Prefix = "concept Id : Uuid\neventsource A\n  identifier Id\n  stream S\nmodule M\n  feature F\n    slice StateChange S\n      command C\n";

    [Theory]
    [InlineData("id Uuid identifier", "", 1)]
    [InlineData("id Uuid identifier", "\n        produces event Recorded", 1)]
    [InlineData("id Id identifier\n        other String", "\n        produces event Recorded\n          for other", 1)]
    [InlineData("id Id identifier", "\n        produces Recorded\n      event Recorded", 0)]
    [InlineData("id Id generated identifier", "\n        produces Recorded\n      event Recorded", 0)]
    [InlineData("id Id identifier", "\n        produces event Recorded", 0)]
    [InlineData("id Id identifier", "\n        produces event Recorded\n          for id", 0)]
    void should_pin_error_severity_and_not_duplicate_implicit_identifier_errors(string properties, string production, int count)
    {
        var result = _compiler.Compile(Prefix + "        " + properties + "\n        stream A.S" + production);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0504" && diagnostic.Severity == DiagnosticSeverity.Error).ShouldEqual(count);
    }

    [Fact]
    void should_refuse_untyped_allocation_only_for_known_nonuuid_sources()
    {
        var text = Prefix.Replace("Id : Uuid", "Id : String") + "        id Id identifier\n        stream A.S\n        produces Recorded\n      event Recorded";
        _compiler.Compile(text).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504" && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    void should_leave_unrouted_commands_and_unknown_imported_types_unaffected()
    {
        _compiler.Compile(Prefix + "        id Uuid identifier").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeFalse();
        _compiler.Compile("import Contracts.Unknown\n" + Prefix + "        id Unknown identifier\n        stream A.S\n        produces event Recorded").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeFalse();
    }
}
