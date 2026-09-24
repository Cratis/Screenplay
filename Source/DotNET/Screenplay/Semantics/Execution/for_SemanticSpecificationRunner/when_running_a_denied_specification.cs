// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_a_denied_specification : Specification
{
    const string Source =
        """
        policy NeedsEditor
          require authenticated and role "Editor"
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                quantity Int
                authorize NeedsEditor
                validate
                  require quantity > 0
              specification GuestIsDenied
                given caller
                  role "Guest"
                when PlaceOrder
                  quantity = 0
                then denied
              specification EditorFailsValidation
                given caller
                  authenticated
                  role "Editor"
                when PlaceOrder
                  quantity = 0
                then error
        """;

    SemanticSpecificationRun _denied;
    SemanticSpecificationRun _validation;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        const string key = "caller-fixture";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Orders.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _denied = runner.Run(plan, plan.Specifications.Values.Single(value => value.Name == "GuestIsDenied").Id);
        _validation = runner.Run(plan, plan.Specifications.Values.Single(value => value.Name == "EditorFailsValidation").Id);
    }

    [Fact] void should_match_an_explicit_denial() => _denied.Passed.ShouldBeTrue();
    [Fact] void should_reject_before_validation() => ((SemanticRejected)_denied.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_match_a_separate_validation_error() => _validation.Passed.ShouldBeTrue();
    [Fact] void should_not_conflate_validation_with_denial() => ((SemanticRejected)_validation.Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
}
