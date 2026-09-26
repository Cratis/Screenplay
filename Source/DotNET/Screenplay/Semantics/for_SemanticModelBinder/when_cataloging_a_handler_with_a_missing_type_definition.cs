// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_cataloging_a_handler_with_a_missing_type_definition : given.a_semantic_binder
{
    SemanticTypedContextDescriptor _descriptor;

    void Because()
    {
        var result = Bind("""
            concept Code : String
            module Billing
              feature Accounts
                slice StateChange Commands
                  command Deposit
                    code Code
                    validate csharp
                      ```csharp
                      return true;
                      ```
            """);
        var application = result.Value!.Model.Application with { Concepts = [] };
        var requirement = result.ImplementationRequirements.Single() with { Role = SemanticImplementationRole.CommandHandler };
        _descriptor = SemanticTypedContextCatalog.Create(application, [requirement], false).Single() with { ModelRevision = result.Value.Model.Revision };
    }

    [Fact] void should_mark_the_incomplete_shape_as_not_renderable() => _descriptor.IsWrapperReady.ShouldBeFalse();
    [Fact] void should_keep_the_unresolved_reference_visible() => _descriptor.Members[0].Type.Properties[0].Type.Kind.ShouldEqual(SemanticTypeReferenceKind.Concept);
    [Fact] void should_not_publish_a_partial_type_table() => _descriptor.Types.ShouldBeEmpty();
}
