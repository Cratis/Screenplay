// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_with_a_registered_inline_language : Specification
{
    const string Source =
        """
        policy CanApprove
          python
            ```
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

    static CodeBlockSyntax Code(CompilationResult<ApplicationSyntax> result) => result.Value!.Policies.Single().Code!;
}
