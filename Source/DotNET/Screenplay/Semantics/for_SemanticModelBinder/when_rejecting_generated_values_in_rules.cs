// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_generated_values_in_rules : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        name String\n";

    [Theory]
    [InlineData("id not empty")]
    [InlineData("id.value not empty")]
    [InlineData("id rule Check\n            file Rules/Check.cs")]
    [InlineData("require id == \"11111111-1111-1111-1111-111111111111\"\n            message \"No\"")]
    [InlineData("require name == id\n            message \"No\"")]
    [InlineData("require name != \"\" and id != \"11111111-1111-1111-1111-111111111111\"\n            message \"No\"")]
    void should_refuse_pre_generation_validation_references_with_an_actionable_binding_diagnostic(string rule)
    {
        var result = Bind(Prefix + "        validate\n          " + rule);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273" && diagnostic.Message.Contains("Generated property 'id'", StringComparison.Ordinal) && diagnostic.Message.Contains("after validation", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("id", "command")]
    [InlineData("subject", "command")]
    [InlineData("id", "module")]
    [InlineData("subject", "feature")]
    [InlineData("id", "composed")]
    void should_refuse_policy_references_including_inherited_and_composed_authorization(string target, string scope)
    {
        var authorization = scope switch { "command" => "        authorize Access\n", "composed" => "        authorize Staff and Access\n", _ => string.Empty };
        var source = "concept Id : Uuid\npolicy Access\n  require claim \"id\" matches " + target + "\npolicy Staff\n  require authenticated\nmodule M\n" +
            (scope == "module" ? "  authorize Access\n" : string.Empty) + "  feature F\n" +
            (scope == "feature" ? "    authorize Access\n" : string.Empty) + "    slice StateChange S\n      command C\n        id Id generated identifier\n" +
            authorization;
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273" && diagnostic.Message.Contains("authorization policy 'Access'", StringComparison.Ordinal) && diagnostic.Message.Contains("after validation", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("not empty")]
    [InlineData("rule Check\n      file Rules/Check.cs")]
    [InlineData("```csharp\n    return true;\n    ```")]
    void should_refuse_generated_concepts_with_any_rules(string rule)
    {
        var result = Bind("concept Id : Uuid\n  validate\n    " + rule + "\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("concept 'Id'", StringComparison.Ordinal) && diagnostic.Message.Contains("rules", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Uuid")]
    [InlineData("Id?")]
    [InlineData("Id[]")]
    [InlineData("Name")]
    void should_refuse_unadmitted_generated_types_even_in_a_parsed_tree(string type)
    {
        var result = Bind("concept Id : Uuid\nconcept Name : String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id " + type + " generated");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
    }

    [Theory]
    [InlineData("type Detail\n  id Id generated")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      event Created\n        id Id generated")]
    void should_not_silently_drop_generated_on_noncommand_properties(string declaration)
    {
        var result = Bind("concept Id : Uuid\n" + declaration);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0482").ShouldBeTrue();
    }

    [Fact]
    void should_admit_unrelated_policies_and_input_only_rules_and_requirements()
    {
        var result = Bind("policy Access\n  require authenticated and claim \"name\" matches name\n" + Prefix + "        authorize Access\n        validate\n          name not empty\n          require name != \"\"\n            message \"Name needed\"");
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_publish_only_request_inputs_in_opaque_pre_generation_contexts()
    {
        var result = Bind("policy Access\n  ```csharp\n  return true;\n  ```\n" + Prefix + "        authorize Access\n        validate\n          ```csharp\n          yield break;\n          ```\n          name rule Check\n            ```csharp\n            return true;\n            ```");
        result.Success.ShouldBeTrue();
        var policy = result.TypedContextDescriptors.Single(descriptor => descriptor.Role == SemanticImplementationRole.PolicyPredicate);
        policy.Members.Single(member => member.Name == "Subject").Source.Kind.ShouldEqual(SemanticContextSourceKinds.Unavailable);
        foreach (var descriptor in result.TypedContextDescriptors)
        {
            foreach (var member in descriptor.Members.Where(member => member.Type.Kind == SemanticContextTypeKinds.Shape))
            {
                member.Type.Properties.Select(property => property.Name).ShouldContainOnly("name");
            }
        }
    }
}
