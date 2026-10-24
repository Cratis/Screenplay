// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_with_a_registered_inline_language : Specification
{
    const string Source =
        """
        policy CanApprove
          ```python
          return context.identity.id == "approver"
          ```
        """;

    ScreenplayCompiler _registered;
    ScreenplayCompiler _default;
    CompilationResult<ApplicationSyntax> _withRegistration;
    CompilationResult<ApplicationSyntax> _withoutRegistration;

    void Establish()
    {
        _registered = new(new ScreenplayLanguageRegistry(["python"]));
        _default = new();
    }

    void Because()
    {
        _withRegistration = _registered.Compile(Source);
        _withoutRegistration = _default.Compile(Source);
    }

    [Fact] void should_compile_the_registered_language_without_diagnostics() => _withRegistration.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_language_on_the_block() => Code(_withRegistration).Language.ShouldEqual("python");
    [Fact] void should_carry_the_code_it_did_not_read() => Code(_withRegistration).Code.ShouldEqual("""return context.identity.id == "approver" """.TrimEnd());

    // Registering is what opens the set - a compiler that was not told still reports it, exactly as before.
    [Fact] void should_still_reject_it_without_the_registration() => _withoutRegistration.Diagnostics.ShouldNotBeEmpty();

    // The set the language ships with is unaffected by what a consumer adds.
    [Fact] void should_keep_the_built_in_languages() =>
        ScreenplayLanguageRegistry.Default.InlineLanguages.ShouldContainOnly(ScreenplayLanguageRegistry.BuiltInInlineLanguages);

    [Theory]
    [InlineData("csharp")]
    [InlineData("java")]
    [InlineData("typescript")]
    [InlineData("react")]
    [InlineData("html")]
    [InlineData("sql")]
    public void should_parse_and_round_trip_built_in_fences_without_registration(string language)
    {
        var parsed = _default.Parse(Source.Replace("```python", $"```{language}", StringComparison.Ordinal));
        parsed.Diagnostics.ShouldBeEmpty();
        Code(parsed).Language.ShouldEqual(language);
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        printed.ShouldContain($"```{language}");
        var reparsed = _default.Parse(printed);
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
    }

    [Theory]
    [InlineData("concept Label : String")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      command C")]
    public void should_accept_java_code_validation(string owner)
    {
        var indent = owner.StartsWith("concept", StringComparison.Ordinal) ? "  " : "        ";
        var source = $"{owner}\n{indent}validate\n{indent}  ```java\n{indent}  return true;\n{indent}  ```";
        var parsed = _default.Parse(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var reparsed = _default.Parse(new ScreenplayPrinter().Print(parsed.Value!));
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
    }

    static CodeBlockSyntax Code(CompilationResult<ApplicationSyntax> result) => result.Value!.Policies.Single().Code!;
}
