// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_validation_on_a_property_named_file : given.a_compiler
{
    CompilationResult<ApplicationSyntax>[] _rules;
    CompilationResult<ApplicationSyntax> _include;
    CompilationResult<ApplicationSyntax> _mixed;

    void Because()
    {
        _rules = [.. new[]
        {
            "file not empty",
            "file min 1.5",
            "file matches \"a/b.cs\"",
            "file not empty\tmessage \"Required\"",
            "file not empty\n          other not empty"
        }.Select(rule => _compiler.Parse(Source(rule)))];
        _include = _compiler.Parse(Source("file Rules/Pay file.cs"));
        _mixed = _compiler.Parse(Source("file Rules/Pay.cs\n          other not empty"));
    }

    [Fact] void should_keep_declarative_rules_without_diagnostics() => _rules.SelectMany(result => result.Diagnostics).ShouldBeEmpty();
    [Fact] void should_not_turn_a_property_rule_into_an_include() => _rules.Select(Validation).ShouldEachConformTo(validation => validation is DeclarativeValidateSyntax);
    [Fact] void should_keep_the_file_property_subject() => _rules.Select(Validation).OfType<DeclarativeValidateSyntax>().Select(validation => validation.Rules.First().Property).ShouldEqual(["file", "file", "file", "file", "file"]);
    [Fact] void should_keep_the_other_rule() => ((DeclarativeValidateSyntax)Validation(_rules[^1])).Rules.Count().ShouldEqual(2);
    [Fact] void should_still_accept_a_sole_path_shaped_include() => _include.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_include_path() => ((FileValidateSyntax)Validation(_include)).File.Path.ShouldEqual("Rules/Pay file.cs");
    [Fact] void should_not_claim_an_include_with_another_child() => Validation(_mixed).ShouldBeOfExactType<DeclarativeValidateSyntax>();
    [Fact] void should_still_parse_the_rule_after_a_refused_include() => ((DeclarativeValidateSyntax)Validation(_mixed)).Rules.Single().Property.ShouldEqual("other");

    static ValidateSyntax Validation(CompilationResult<ApplicationSyntax> result) => result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();

    static string Source(string body) =>
        "module M\n  feature F\n    slice StateChange S\n      command Pay\n        file String\n        other String\n        validate\n          " + body;
}
