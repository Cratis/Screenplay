// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser.when_parsing_a_module;

public class and_it_depends_on_modules_and_features : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile("module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Timesheets.Approval\n    depends on Runs\n  feature Runs\nmodule Timesheets\n  feature Approval\n");

    [Fact] void should_accept_the_declarations() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_module_target() => SyntaxJson.Serialize(_result.Value!).GetProperty("modules")[0].GetProperty("dependsOn")[0].GetProperty("target").GetString().ShouldEqual("Timesheets");
    [Fact] void should_keep_the_feature_targets_in_authored_order() => SyntaxJson.Serialize(_result.Value!).GetProperty("modules")[0].GetProperty("features")[0].GetProperty("dependsOn").EnumerateArray().Select(node => node.GetProperty("target").GetString()).ShouldEqual(["Timesheets.Approval", "Runs"]);
}
