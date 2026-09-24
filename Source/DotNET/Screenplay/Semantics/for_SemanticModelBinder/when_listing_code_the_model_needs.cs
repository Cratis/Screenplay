// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_listing_code_the_model_needs : given.a_semantic_binder
{
    const string Source =
        """
        concept Label : String
          validate csharp
            ```
            return true;
            ```
        policy Access
          ```csharp
          return true;
          ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              event OrderPlaced
                orderId Uuid
              command PlaceOrder
                orderId Uuid identifier
                handler
                  file Handlers/PlaceOrder.cs
                validate csharp
                  ```
                  return true;
                  ```
            slice StateView Orders
              readmodel OrderSummary
                orderId Uuid
              query OrderById => OrderSummary?
                by orderId Uuid
                performer
                  file Queries/OrderById.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_not_admit_an_executable_model() => _result.Value.ShouldBeNull();
    [Fact] void should_preserve_play0268_for_remaining_unsupported_attachments() => (_result.Diagnostics.Count(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax) >= 2).ShouldBeTrue();
    [Fact] void should_list_every_attachment() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.CommandHandler);
    [Fact] void should_list_command_validation() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.CommandValidation);
    [Fact] void should_list_concept_validation() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.ConceptValidation);
    [Fact] void should_list_policy_predicate() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.PolicyPredicate);
    [Fact] void should_list_query_performer() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.QueryPerformer);
    [Fact] void should_list_five_in_source_order() => _result.ImplementationRequirements.Select(value => value.Role).ShouldEqual([

        SemanticImplementationRole.ConceptValidation,
        SemanticImplementationRole.PolicyPredicate,
        SemanticImplementationRole.CommandHandler,
        SemanticImplementationRole.CommandValidation,
        SemanticImplementationRole.QueryPerformer
    ]);
    [Fact] void should_map_each_attachment_to_a_real_document() => _result.ImplementationRequirements.All(value => value.Source.Span.Document.IsSet).ShouldBeTrue();
    [Fact] void should_hash_inline_content_but_not_file_paths() => _result.ImplementationRequirements.Select(value => (value.File is null, value.ContentHash.Length, value.AttachmentResolution)).ShouldEqual([
        (true, 64, SemanticAttachmentResolution.Resolved),
        (true, 64, SemanticAttachmentResolution.Resolved),
        (false, 0, SemanticAttachmentResolution.UnresolvedFile),
        (true, 64, SemanticAttachmentResolution.Resolved),
        (false, 0, SemanticAttachmentResolution.UnresolvedFile)
    ]);
    [Fact] void should_name_role_contracts_and_capabilities() => _result.ImplementationRequirements.All(value => value.RequirementId.Length == 64 && value.ContextVersion == 1 && value.ResultVersion == 1 && value.RequiredCapability == (value.Role is SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.PolicyPredicate ? "pure" : "provider-defined")).ShouldBeTrue();
    [Fact] void should_not_list_code_on_a_portable_model() => Bind("module Orders\n  feature Ordering\n    slice StateChange PlaceOrder\n      command PlaceOrder").ImplementationRequirements.ShouldBeEmpty();
}
