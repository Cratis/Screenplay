// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_generation_rule_attachment
{
    [Fact]
    void should_attach_an_admitted_provider_defined_string_generator()
    {
        var result = Bind(Inline());
        result.Success.ShouldBeTrue();
        var requirement = result.ImplementationRequirements.Single();
        requirement.Role.ShouldEqual(SemanticImplementationRole.ValueGeneration);
        requirement.Member.ShouldEqual("TenantOrder");
        requirement.RequiredCapability.ShouldEqual("provider-defined");
        requirement.ContextVersion.ShouldEqual(1u);
        requirement.ResultVersion.ShouldEqual(1u);
        requirement.Language.ShouldEqual("csharp");
        requirement.BodySpan.HasValue.ShouldBeTrue();
        requirement.BodyLines.ShouldNotBeEmpty();
        result.TypedContextDescriptors.Single().IsWrapperReady.ShouldBeTrue();
    }

    [Fact]
    void should_keep_identity_and_content_hash_when_the_file_moves()
    {
        var first = Bind(FileSource("Generators/First.cs"), "Generators/First.cs").ImplementationRequirements.Single();
        var moved = Bind(FileSource("Generators/Moved.cs"), "Generators/Moved.cs").ImplementationRequirements.Single();
        first.RequirementId.ShouldEqual(moved.RequirementId);
        first.ContentHash.ShouldEqual(moved.ContentHash);
        first.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        first.BodySpan.HasValue.ShouldBeTrue();
    }

    [Fact]
    void should_pin_body_edits_without_changing_requirement_identity()
    {
        var first = Bind(Inline()).ImplementationRequirements.Single();
        var changed = Bind(Inline("return other;")).ImplementationRequirements.Single();
        first.RequirementId.ShouldEqual(changed.RequirementId);
        first.ContentHash.ShouldNotEqual(changed.ContentHash);
    }

    [Fact]
    void should_use_rule_name_in_the_requirement_identity()
    {
        var first = Bind(Inline()).ImplementationRequirements.Single();
        var changed = Bind(Inline().Replace("rule TenantOrder", "rule Other", StringComparison.Ordinal)).ImplementationRequirements.Single();
        first.RequirementId.ShouldNotEqual(changed.RequirementId);
    }

    [Fact]
    void should_not_confuse_concept_provenance_with_a_generation_body()
    {
        var result = Bind("concept TenantOrderId : String\n  file Provenance.cs\n  generate rule TenantOrder");
        result.ImplementationRequirements.ShouldBeEmpty();
        result.Success.ShouldBeFalse();
    }

    [Fact]
    void should_load_only_the_nested_generation_implementation_reference()
    {
        var source = FileSource("Generators/First.cs").Replace("  generate", "  file Provenance.cs\n  generate", StringComparison.Ordinal);
        var documents = Documents(source, null);
        AttachmentFiles.References(documents.Documents).Select(file => file.Path).ShouldEqual(["Generators/First.cs"]);
    }

    [Fact]
    void should_describe_only_now_and_tenant_with_no_model_input()
    {
        var requirement = Bind(Inline()).ImplementationRequirements.Single();
        var model = Bind(Inline()).Value!.Model;
        var descriptor = SemanticTypedContextCatalog.Create(model.Application, [requirement], true).Single();
        descriptor.Members.Select(member => member.Name).ShouldEqual(["Now", "Tenant"]);
        descriptor.Members.Select(member => member.Type.RuntimeToken).ShouldEqual([SemanticContextRuntimeTokens.DateTime, SemanticContextRuntimeTokens.TenantId]);
        descriptor.Types.ShouldBeEmpty();
        descriptor.Role.ShouldEqual(SemanticImplementationRole.ValueGeneration);
    }

    [Fact]
    void should_refuse_an_unknown_context_version()
    {
        var requirement = Bind(Inline()).ImplementationRequirements.Single();
        var application = Bind(Inline()).Value!.Model.Application;
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [requirement with { ContextVersion = 2 }], true)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_a_role_with_an_unresolved_owner()
    {
        var requirement = Bind(Inline()).ImplementationRequirements.Single();
        var application = Bind("concept Other : String").Value!.Model.Application;
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [requirement], true)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    static string Inline(string body = "return value;") => $"concept TenantOrderId : String\n  generate rule TenantOrder\n    ```csharp\n    {body}\n    ```";
    static string FileSource(string path) => $"concept TenantOrderId : String\n  generate rule TenantOrder\n    file {path}";
    static CompilationResult<SemanticCompilation> Bind(string source, string? file = null) => new SemanticModelBinder().Bind("Projects", new ScreenplayCompiler().Parse(source).Value!, Documents(source, file));
    static SemanticDocumentSet Documents(string source, string? file)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string Key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", source);
        var contents = file is null ? null : ImmutableDictionary<string, string>.Empty.Add(file, "return value;");

        return SemanticDocumentSet.Create([document], catalog, contents);
    }
}
