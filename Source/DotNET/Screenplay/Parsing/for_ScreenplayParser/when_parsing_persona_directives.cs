// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_persona_directives : Specification
{
    const string Source =
        """
        persona Shopper
          goal Find things
          audience 1x
          policy
          shape round

        persona Buyer
          description "Buys things"
          goal "Checkout"
          audience Customers
          policy Admin
          position -1 2
          size 3 4
        """;

    CompilationResult<ApplicationSyntax> _result;
    string[] _messages;
    PersonaSyntax _buyer;

    void Because()
    {
        _result = new ScreenplayCompiler().Compile(Source);
        _messages = [.. _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")];
        _buyer = _result.Value!.Personas.Single(persona => persona.Name == "Buyer");
    }

    [Fact] void should_report_the_invalid_goal() => _messages.ShouldContain($"{DiagnosticCodes.InvalidPersonaDeclaration} Invalid goal 'goal Find things' - expected 'goal \"<text>\"'");
    [Fact] void should_report_the_invalid_audience() => _messages.ShouldContain($"{DiagnosticCodes.InvalidAudienceDeclaration} Invalid audience reference 'audience 1x' - expected 'audience <Name>'");
    [Fact] void should_report_the_invalid_policy() => _messages.ShouldContain($"{DiagnosticCodes.InvalidPersonaPolicyReference} Invalid policy reference 'policy' - expected 'policy <Name>'");
    [Fact] void should_report_the_unknown_directive() => _messages.ShouldContain($"{DiagnosticCodes.UnknownPersonaDirective} Unexpected 'shape' in persona body - expected description, goal, policy, audience, position or size");
    [Fact] void should_keep_the_description() => _buyer.Description.ShouldEqual("Buys things");
    [Fact] void should_keep_the_goals() => _buyer.Goals.ShouldEqual("Checkout");
    [Fact] void should_keep_the_audiences() => _buyer.Audiences.ShouldEqual("Customers");
    [Fact] void should_keep_the_policies() => _buyer.Policies.ShouldEqual("Admin");
    [Fact] void should_keep_the_x_position() => _buyer.X.ShouldEqual(-1);
    [Fact] void should_keep_the_y_position() => _buyer.Y.ShouldEqual(2);
    [Fact] void should_keep_the_width() => _buyer.Width.ShouldEqual(3);
    [Fact] void should_keep_the_height() => _buyer.Height.ShouldEqual(4);
}
