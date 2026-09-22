// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_a_printer_omits_a_structural_value : Specification
{
    ApplicationSyntax _intended;
    CompilationResult<ApplicationSyntax> _parsed;
    string _printed;
    bool _equal;

    void Establish()
    {
        var location = SourceLocation.Start;
        var rule = new ValidationRuleSyntax("value", ValidationRuleKind.NotEmpty, null, null, location, new("Validate.cs", location));
        var concept = new ConceptSyntax("InvoiceNumber", "String", [], [], location, [new DeclarativeValidateSyntax([rule], location)]);
        _intended = new([], [concept], [], [], location);
    }

    void Because()
    {
        _printed = new ScreenplayPrinter().Print(_intended);
        _parsed = new ScreenplayCompiler().Compile(_printed);
        _equal = SyntaxJson.StructurallyEqual(_intended, _parsed.Value);
    }

    [Fact] void should_exercise_the_printers_omission_path() => _printed.ShouldContain("// TODO:");
    [Fact] void should_still_compile_the_printed_text() => _parsed.Success.ShouldBeTrue();
    [Fact] void should_not_confuse_successful_compilation_with_structural_equivalence() => _equal.ShouldBeFalse();
    [Fact] void should_preserve_the_intent_through_the_json_codec() => SyntaxJson.StructurallyEqual(_intended, SyntaxJson.Deserialize(SyntaxJson.Serialize(_intended))).ShouldBeTrue();
}
