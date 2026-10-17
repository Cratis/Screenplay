// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_incompatible_production_mappings;

// An optional command property mapped into a required produced property is refused at the mapping.
public class with_an_optional_command_property : given.a_semantic_binder
{
    const string Source =
        """
        concept Amount : Decimal
        concept ItemId : String
        module Alpha
          feature Items
            slice StateChange ReviseItem
              command ReviseItem
                itemId ItemId identifier
                amount Amount optional
                produces ItemRevised
                  amount = amount
              event ItemRevised
                amount Amount
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source, displayPath: "Beta.play");

    [Fact] void should_fail_the_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_binding_diagnostic() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldEqual(1);
    [Fact] void should_name_the_property_and_both_types() => Reported.Message.ShouldEqual("Mapping 'amount' into event 'ItemRevised' assigns an optional 'Amount' to the required property 'amount'.");
    [Fact] void should_locate_the_mapping() => (Reported.Location.Line, Reported.Location.Column).ShouldEqual((10, 11));
    [Fact] void should_not_report_the_application_root() => Reported.Location.Line.ShouldNotEqual(1);

    Diagnostic Reported => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
}
