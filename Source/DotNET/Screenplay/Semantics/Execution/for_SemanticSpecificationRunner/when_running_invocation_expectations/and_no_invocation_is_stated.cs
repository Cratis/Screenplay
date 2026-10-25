// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_no_invocation_is_stated : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith("then no events");

    void Because() => _run = RunScenario();

    [Fact] void should_keep_the_earlier_version() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V17);
    [Fact] void should_not_compare_invocations() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_still_record_the_invocations() => _run.Invocations.Length.ShouldEqual(2);
}
