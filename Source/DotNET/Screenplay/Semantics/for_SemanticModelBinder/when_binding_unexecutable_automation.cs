// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_unexecutable_automation : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        trigger Untyped
          source
        trigger Typed
          size Int
        module Billing
          feature Automation
            slice Automation Clockwork
              reaction Sweeper
                every 1 hour
                  produces Swept
                    note = "swept"
              reaction Partial
                when Typed
                  size
                  produces Swept
                    for "sweeps"
                    note = "sized"
                where size > 0 and missing == 1
              event Swept
                note String
              specification StartingWithValues
                when trigger Startup
                  size = 1
                then Swept
                  note = "swept"
            slice Translate Legacy
              capture Keyless
                append Swept
                  note = "keyless"
              capture Loose
                key id
                append Swept
                  when `status ~ "paid"`
                    note = "loose"
        """);

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_require_a_typed_trigger_value() => Reports(DiagnosticCodes.UnsupportedSemanticSyntax, "value 'source' needs a type");
    [Fact] void should_require_a_clock_reaction_to_name_its_event_source() => Reports(DiagnosticCodes.InvalidSemanticBinding, "Reaction 'Sweeper' must say with 'for'");
    [Fact] void should_refuse_a_where_its_trigger_carries_only_part_of() => Reports(DiagnosticCodes.InvalidSemanticBinding, "carries only some of: missing");
    [Fact] void should_refuse_values_for_a_built_in_trigger() => Reports(DiagnosticCodes.InvalidSemanticBinding, "'Startup' carries no values");
    [Fact] void should_require_a_capture_key() => Reports(DiagnosticCodes.UnsupportedSemanticSyntax, "Capture 'Keyless' needs a 'key'");
    [Fact] void should_refuse_a_condition_outside_the_template_grammar() => Reports(DiagnosticCodes.UnsupportedSemanticSyntax, "outside the portable template grammar");

    void Reports(string code, string fragment) =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Message.Contains(fragment, StringComparison.Ordinal)).ShouldBeTrue();
}
