// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelTest.when_testing_read_model_member_types;

public class and_the_members_use_top_level_composites : given.a_model
{
    int _exit;
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), """
        type Child
          value String
        module M
          feature F
            slice StateView View
              readmodel Parent
                parentId String key
                children Child[]
              query ById => Parent optional
                by parentId String
              specification NonemptyCompositeCollection
                given readmodel Parent
                  parentId = "parent"
                  children = [{"value":"child"}]
                when query ById
                  parentId = "parent"
                then result
                  parentId = "parent"
                  children = [{"value":"child"}]
        """);

    void Because()
    {
        _exit = ModelTest.Run([Root, "--format", "json"], Output, Error);
        using var document = JsonDocument.Parse(Output.ToString());
        _report = document.RootElement.Clone();
    }

    [Fact] void should_compile_bind_and_execute_successfully() => _exit.ShouldEqual(0);
    [Fact] void should_execute_a_real_nonempty_composite_scenario() => _report.GetProperty("executed").GetInt32().ShouldEqual(1);
    [Fact] void should_pass_the_nonempty_collection_comparison() => _report.GetProperty("passed").GetInt32().ShouldEqual(1);
    [Fact] void should_not_treat_an_unsupported_scenario_as_a_pass() => _report.GetProperty("unsupported").GetInt32().ShouldEqual(0);
    [Fact] void should_not_report_stderr() => Error.ToString().ShouldBeEmpty();
}
