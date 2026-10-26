// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_an_attached_command_handler;

public class and_the_inline_body_is_constrained : given.a_semantic_binder
{
    internal const string Prefix = "module M\n  feature F\n    slice StateChange S\n      event Chosen\n        value String\n      command Choose\n";
    internal const string Body = "return new Chosen(context.Command.value);";
    internal const string Source = Prefix + "        value String\n        produces Chosen\n        handler\n          ```csharp\n          " + Body + "\n          ```\n";
    CompilationResult<SemanticCompilation> _result;
    SemanticImplementationRequirement _requirement;

    void Because()
    {
        _result = Bind(Source);
        Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _requirement = _result.ImplementationRequirements.Single();
    }

    [Fact] void should_select_v23() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_link_the_handler() => _result.Value!.Model.Application.Modules[0].Features[0].Slices[0].Commands[0].Handler!.RequirementId.ShouldEqual(_requirement.RequirementId);
    [Fact] void should_be_resolved() => _requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
    [Fact] void should_hash_the_original_body() => _requirement.ContentHash.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Body))).ToLowerInvariant());
    [Fact] void should_keep_the_body_span() => _requirement.BodySpan.HasValue.ShouldBeTrue();
    [Fact] void should_not_report_pending_implementation() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DescribedCommandNotImplemented).ShouldBeFalse();
    [Fact]
    void should_keep_the_same_model_bytes_as_pending_intent()
    {
        var pending = Bind(Source.Replace("          ```csharp\n          " + Body + "\n          ```", "          implementation", StringComparison.Ordinal));
        SemanticModelSerializer.Serialize(_result.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(pending.Value!.Model));
    }
    [Fact]
    void should_admit_a_foreign_language_without_interpreting_it() => Bind(Source.Replace("```csharp", "```java", StringComparison.Ordinal)).Success.ShouldBeTrue();
    [Fact]
    void should_admit_a_wrapped_body_with_authoring_hints()
    {
        const string wrapped = Prefix + "        produces Chosen\n        handler\n          implementation\n            hint \"Intent and body are distinct\"\n            ```csharp\n            return null;\n            ```\n";
        var result = Bind(wrapped);
        result.Success.ShouldBeTrue();
        result.ImplementationRequirements.Single().Hints.ShouldBeEmpty();
    }
    [Fact]
    void should_keep_described_commands_beside_attached_commands() => Bind(Source + "      command Later\n        produces Chosen\n        handler\n          implementation\n").Success.ShouldBeTrue();
}
