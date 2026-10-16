// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_described_command;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_keeping_handler_identity_across_edits : given.a_semantic_binder
{
    [Theory]
    [InlineData("First hint", "Changed hint")]
    [InlineData("      command Choose", "      command Choose\n        description \"Business intent\"")]
    [InlineData("        produces Chosen", "        produces Chosen\n        produces Chosen")]
    [InlineData("        produces Chosen\n", "")]
    void should_keep_identity_independent_of_intent_and_payload(string original, string replacement)
    {
        const string source = and_it_produces_unconditionally.Source;
        var before = Bind(source).ImplementationRequirements.Single();
        var edited = source.Replace(original, replacement, StringComparison.Ordinal);
        if (replacement.Length == 0) edited = edited.Replace("            hint \"Second hint\"", "            hint \"Second hint\"\n            file Choose.cs", StringComparison.Ordinal);
        Bind(edited).ImplementationRequirements.Single().RequirementId.ShouldEqual(before.RequirementId);
    }

    [Fact]
    void should_leave_legacy_bytes_and_version_unchanged()
    {
        const string Source = "module M\n  feature F\n    slice StateChange S\n      event Chosen\n      command Choose\n        produces Chosen\n";
        var before = Bind(Source).Value!.Model;
        var after = Bind(Source.Replace("      command Choose", "      command Choose\n        description \"Business intent\"", StringComparison.Ordinal)).Value!.Model;
        before.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        SemanticModelSerializer.Serialize(after).ShouldEqual(SemanticModelSerializer.Serialize(before));
    }
}
