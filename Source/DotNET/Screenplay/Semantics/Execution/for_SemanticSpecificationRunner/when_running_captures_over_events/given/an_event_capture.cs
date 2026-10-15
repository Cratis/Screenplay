// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events.given;

public class an_event_capture : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    protected SemanticSpecificationRun _run;

    protected void RunCapture(string rules, string steps)
    {
        CompileCapture(rules, steps);
        _run = Run("S");
    }

    protected void CompileCapture(string rules, string steps)
    {
        const string Header = """
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                    status String
                  event Labelled from "carrier"
                    id String
                    label String
                  event Translated
                    id String
                    status String
                  event LabelRecorded
                    id String
                    status String
                    label String
                  event Added
                  event Modified
                  event Removed
                  event Stamped
                    occurred DateTime
                  capture Feed
                    source events
                      from Arrived
                      from Labelled
                    key id
            """;
        Compile(Header + "\n" + Indent(rules, 8) + "\n      specification S\n" + Indent(steps, 8) + "\n");
    }

    protected void ReplaceSpecification(SemanticSpecification specification)
    {
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = _plan.Model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification] }] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application)).Plan!;
    }

    static string Indent(string source, int spaces) => string.Join('\n', source.Split('\n').Select(line => new string(' ', spaces) + line));
}
