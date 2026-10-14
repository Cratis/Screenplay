// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_explicit_reaction_destinations : given.a_reducer_identity_model
{
    const string Reaction = "\n    slice Automation Follow\n      reaction Follow\n        when Recorded\n          produces Recorded\n            for resource";

    [Fact] void should_compare_an_explicit_occurrence_property() => Warnings(Model + Reaction).Length.ShouldEqual(1);
    [Fact] void should_compare_literal_text_destinations() => Warnings(Model[..Model.IndexOf("    slice StateChange", StringComparison.Ordinal)] + Reaction.Replace("for resource", "for \"fixed\"", StringComparison.Ordinal)).Single().Message.ShouldContain("identity String");
    [Fact] void should_retain_unknown_explicit_destinations() => Warnings(Model + Reaction.Replace("for resource", "for missing", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_compare_explicit_refusal_destinations() => Warnings(Model + "\n    slice Automation Follow\n      reaction Follow\n        when Recorded\n          invokes Record\n            resource = resource\n            on refused\n              produces Recorded\n                for resource").Length.ShouldEqual(1);
    [Fact] void should_retain_unknown_inherited_refusal_destinations() => Warnings(Model + "\n    slice Automation Follow\n      reaction Follow\n        when Recorded\n          invokes Record\n            resource = resource\n            on refused\n              produces Recorded").ShouldBeEmpty();
}
