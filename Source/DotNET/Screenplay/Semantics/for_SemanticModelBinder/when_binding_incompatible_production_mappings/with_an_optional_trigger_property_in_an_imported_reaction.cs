// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_incompatible_production_mappings;

public class with_an_optional_trigger_property_in_an_imported_reaction : Specification
{
    const string Application =
        """
        domain Acme

        concept ItemId : String
        concept Amount : Decimal

        import "Alpha.play"
        import "Beta.play"
        """;
    const string Alpha =
        """
        module Alpha
          feature Items
            slice StateChange ReviseItem
              command ReviseItem
                itemId ItemId identifier
                amount Amount
                produces ItemRevised
                  amount = amount

              event ItemRevised
                amount Amount

              specification Revising
                when ReviseItem
                  itemId = "i1"
                  amount = 1.5
                then ItemRevised
                  for "i1"
                  amount = 1.5
        """;
    const string Beta =
        """
        module Beta
          feature Alerts
            slice Automation RaiseAlert
              event Sampled
                amount Amount optional

              event AlertRaised
                amount Amount

              reaction RaiseOnSample
                when Sampled
                  amount
                  produces AlertRaised
                    amount = amount
        """;

    SemanticDocumentSet _documents;
    CompilationResult<SemanticCompilation> _result;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme"));
        _documents = SemanticDocumentSet.Create(
            [
                SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Application),
                SemanticSourceDocument.Create(catalog.ResolveDocument("alpha"), "alpha", "Alpha.play", Alpha),
                SemanticSourceDocument.Create(catalog.ResolveDocument("beta"), "beta", "Beta.play", Beta)
            ],
            catalog);
    }

    void Because() => _result = new SemanticModelCompiler().Compile("Acme", _documents);

    [Fact] void should_fail_the_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_binding_diagnostic() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldEqual(1);
    [Fact] void should_name_the_property_and_both_types() => Reported.Message.ShouldEqual("Mapping 'amount' into event 'AlertRaised' assigns an optional 'Amount' to the required property 'amount'.");
    [Fact] void should_locate_the_reaction_file() => Reported.Location.Path.ShouldEqual("Beta.play");
    [Fact] void should_locate_the_mapping_line_and_column() => (Reported.Location.Line, Reported.Location.Column).ShouldEqual((14, 13));
    [Fact] void should_not_report_the_unrelated_file_root() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Location == new SourceLocation(1, 1, "Alpha.play")).ShouldBeFalse();

    Diagnostic Reported => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
}
