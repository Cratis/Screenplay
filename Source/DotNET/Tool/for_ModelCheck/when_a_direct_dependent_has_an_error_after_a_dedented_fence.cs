// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_a_direct_dependent_has_an_error_after_a_dedented_fence : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "consumer.play"), "module Other\n  feature F\n    slice StateChange Use\n      command Consume\n        id String identifier\n        value String\n        produces CleanEvent\n          for id\n          value = value\n        description\n          ```text\nDedented body\n```\n        broken");
    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean"], Output, Error);

    [Fact] void should_fail_the_scoped_check() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_error_after_the_fence() => Output.ToString().ShouldContain("consumer.play(14,9): error");
    [Fact] void should_report_the_affected_scope() => Output.ToString().ShouldContain("Affected scopes: Other.F.Use");
}
