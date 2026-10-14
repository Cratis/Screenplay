// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_known_command_destinations : given.a_reducer_identity_model
{
    [Fact] void should_use_the_explicit_destination_not_a_payload_name() => Warnings(Model.Replace("resource ResourceId identifier", "resource ResourceId identifier\n        month Month", StringComparison.Ordinal).Replace("for resource", "for month", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_use_the_implicit_inline_identifier() => Warnings(Model.Replace("      event Recorded\n        resource ResourceId\n", string.Empty, StringComparison.Ordinal)
        .Replace("produces Recorded\n          for resource\n          resource = resource", "produces event Recorded\n          resource ResourceId = resource", StringComparison.Ordinal)).Length.ShouldEqual(1);
    [Fact] void should_use_the_generated_identifier() => Warnings(Model.Replace("resource ResourceId identifier", "resource ResourceId generated identifier", StringComparison.Ordinal)
        .Replace("\n          for resource", string.Empty, StringComparison.Ordinal)).Length.ShouldEqual(1);
    [Fact] void should_use_the_plain_identifier_not_a_fixture_allocation_fallback() => Warnings(Model.Replace("\n          for resource", string.Empty, StringComparison.Ordinal)).Single().Message.ShouldContain("identity ResourceId");
    [Fact] void should_follow_an_explicit_nested_destination() => Warnings("type Owner\n  resource ResourceId\n" + Model.Replace("resource ResourceId identifier", "owner Owner", StringComparison.Ordinal)
        .Replace("for resource", "for owner.resource", StringComparison.Ordinal).Replace("resource = resource", "resource = owner.resource", StringComparison.Ordinal)).Length.ShouldEqual(1);
    [Fact] void should_not_require_reducer_attachment_contents() => Warnings(Model.Replace("on Recorded", "on Recorded\n        file Fold.cs", StringComparison.Ordinal)).Length.ShouldEqual(1);
}
