// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_module_with_same_named_unrelated_declarations : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "type Orders\n  broken\nconcept Orders : String\n  broken\neventsource Orders\n  stream Special\n    broken\nmodule Orders\n  feature F\n    slice StateChange Clean\n      event Added\nmodule Outside\n  feature F\n    slice StateChange Bad\n      command Use\n        value Orders\n        stream Orders.Special\n        broken");
    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "Orders"], Output, Error);

    [Fact] void should_pass_the_scoped_check() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_only_the_module_hierarchy() => Output.ToString().ShouldContain("Scope Orders: 4 declaration(s), 0 direct dependent declaration(s), 0 diagnostic(s)");
    [Fact] void should_report_no_affected_scopes() => Output.ToString().ShouldContain("Affected scopes: none");
    [Fact] void should_not_hide_the_whole_application_failure() => Output.ToString().Contains("Whole application: 0 error(s)", StringComparison.Ordinal).ShouldBeFalse();
}
