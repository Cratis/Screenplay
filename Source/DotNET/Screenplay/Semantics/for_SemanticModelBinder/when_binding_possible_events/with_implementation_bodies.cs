// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_implementation_bodies : given.a_possible_events_binder
{
    [Theory]
    [InlineData("          ```csharp\n          return null;\n          ```")]
    [InlineData("          implementation\n            ```csharp\n            return null;\n            ```")]
    void should_bind_inline_and_wrapped_bodies(string body)
    {
        var result = BindPossible(Source.Replace("          implementation\n            hint \"Return only the chosen events\"\n", body, StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        result.ImplementationRequirements.Single().ResultVersion.ShouldEqual(2u);
    }

    [Fact]
    void should_bind_a_host_resolved_file()
    {
        var result = BindPossible(Source.Replace("          implementation\n            hint \"Return only the chosen events\"\n", "          implementation\n            file Handler.cs", StringComparison.Ordinal), ImmutableDictionary<string, string>.Empty.Add("Handler.cs", "return null;"));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        result.ImplementationRequirements.Single().ResultVersion.ShouldEqual(2u);
    }

    [Theory]
    [InlineData("          file Handler.cs")]
    [InlineData("          ```\n          return null;\n          ```")]
    [InlineData("          ```csharp\n          ```")]
    void should_retain_v1_body_admission_constraints(string body)
    {
        var result = BindPossible(Source.Replace("          implementation\n            hint \"Return only the chosen events\"\n", body, StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.CommandHandlerRequiresAttachment);
    }

    [Fact]
    void should_keep_generated_identifier_destinations()
    {
        var result = BindPossible("concept Id : Uuid\n" + Source.Replace("id Uuid identifier", "id Id generated identifier", StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        ((SemanticResolvedExpression)Command(result.Value!).Handler!.PossibleEvents.First().Destination!).Target.ShouldEqual(Command(result.Value!).Properties.Single(property => property.IsIdentifier).Id);
    }

    [Fact]
    void should_preserve_the_legacy_candidate_version_and_result_contract()
    {
        var legacy = Prefix + Properties + "        handler\n          implementation\n        produces Created\n          for id";
        var result = BindPossible(legacy);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.SemanticVersion.ShouldEqual(SemanticVersion.V23);
        Command(result.Value!).Handler!.ResultVersion.ShouldEqual(1u);
        Command(result.Value!).Handler!.PossibleEvents.ShouldBeEmpty();
        result.ImplementationRequirements.Single().ResultVersion.ShouldEqual(1u);
    }
}
