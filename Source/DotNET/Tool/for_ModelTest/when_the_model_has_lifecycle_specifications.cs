// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_the_model_has_lifecycle_specifications : given.a_model
{
    int _exit;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), File.ReadAllText(ConformanceDocument()));
    void Because() => _exit = ModelTest.Run([Root], Output, Error);

    static string ConformanceDocument([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "Screenplay", "Compiler", "Conformance", "lifecycles.play"));

    [Fact] void should_pass() => _exit.ShouldEqual(0);
    [Fact] void should_run_the_state_machine_specifications() => Output.ToString().ShouldContain("passed: InvoiceLifecycle.SendingStartsTheCollection");
    [Fact] void should_run_the_workflow_specifications() => Output.ToString().ShouldContain("passed: Collect.NoPaymentIsWrittenOff");
}
