// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_named_rule_intent
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check severity warning message \"Invalid\"\n";
    const string Suffix = "\n          label not empty\n        hint String\n        implementation String\n      event Next\n        label String";

    [Theory]
    [InlineData("implementation\n              hint \" \"")]
    [InlineData("implementation\n              hint \"Keep\"\n                file A.cs")]
    [InlineData("implementation\n              hint \"Keep\"\n              implementation")]
    [InlineData("implementation\n              file A.cs\n              file B.cs")]
    [InlineData("implementation\n              file A.cs\n              ```csharp\n              return true;\n              ```")]
    [InlineData("implementation\n              hint \"Keep\"\n            implementation")]
    [InlineData("file A.cs\n            implementation")]
    [InlineData("implementation\n              file A.cs\n            file B.cs")]
    public void should_reject_malformed_wrappers_and_recover_siblings(string body)
    {
        var parsed = new ScreenplayCompiler().Parse(Prefix + "            " + body + Suffix);
        parsed.Success.ShouldBeFalse();
        var slice = parsed.Value!.Modules.Single().Features.Single().Slices.Single();
        slice.Events.Single().Name.ShouldEqual("Next");
        var command = slice.Commands.Single();
        ((DeclarativeValidateSyntax)command.Validations.Single()).Rules.Count().ShouldEqual(2);
        command.Properties.Select(property => property.Name).ShouldEqual(["label", "hint", "implementation"]);
    }

    [Theory]
    [InlineData("\u0085", true)]
    [InlineData("\u00a0", true)]
    [InlineData("\uFEFF", false)]
    public void should_use_the_shared_hint_whitespace_predicate(string separator, bool valid)
    {
        var parsed = new ScreenplayCompiler().Parse(Prefix + "            implementation\n              hint" + separator + "\"Keep\"" + Suffix);
        parsed.Success.ShouldEqual(valid);
    }

    [Fact]
    public void should_reject_new_wrappers_on_concepts_and_builtins()
    {
        new ScreenplayCompiler().Parse("concept Label : String\n  validate\n    rule Check\n      implementation\n        hint \"Keep\"").Success.ShouldBeFalse();
        new ScreenplayCompiler().Parse(Prefix.Replace("label rule Check severity warning message \"Invalid\"", "label not empty", StringComparison.Ordinal) + "            implementation\n              hint \"Keep\"").Success.ShouldBeFalse();
    }

    [Fact]
    public void should_visit_payload_and_hint_once_and_preserve_comments()
    {
        const string source = Prefix + "            implementation // wrapper\n              // guidance\n              hint \"Keep\\ncriteria\" // hint\n              file Rules/Check.cs // file" + Suffix;
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Success.ShouldBeTrue();
        var walker = new CountingWalker();
        walker.VisitApplication(parsed.Value!);
        walker.Nodes.OfType<ImplementationSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<ImplementationHintSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<FileReferenceSyntax>().Count().ShouldEqual(1);
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        foreach (var comment in new[] { "// wrapper", "// guidance", "// hint", "// file" }) printed.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
        var reparsed = new ScreenplayCompiler().Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
    }

    [Fact]
    public void should_preserve_custom_registered_fences_without_reading_code()
    {
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["python"]));
        const string source = Prefix + "            implementation\n              hint \"Keep\"\n              ```python\n              # command Fake\n              return lambda hint: hint\n              ```" + Suffix;
        var parsed = compiler.Parse(source);
        parsed.Success.ShouldBeTrue();
        var walker = new CountingWalker();
        walker.VisitApplication(parsed.Value!);
        walker.Nodes.OfType<CodeBlockSyntax>().Single().Code.ShouldEqual("# command Fake\nreturn lambda hint: hint");
        SyntaxJson.StructurallyEqual(parsed.Value!, compiler.Parse(new ScreenplayPrinter().Print(parsed.Value!)).Value!).ShouldBeTrue();
    }

    [Fact]
    public void should_preserve_old_constructor_deconstruction_and_json_defaults()
    {
        var rule = new ValidationRuleSyntax("label", ValidationRuleKind.Rule, new PathExpressionSyntax("Check", SourceLocation.Start), null, SourceLocation.Start);
        var (property, kind, value, message, location, file, code) = rule;
        property.ShouldEqual("label");
        kind.ShouldEqual(ValidationRuleKind.Rule);
        value.ShouldEqual(rule.Value);
        message.ShouldBeNull();
        location.ShouldEqual(SourceLocation.Start);
        file.ShouldBeNull();
        code.ShouldBeNull();
        rule.Implementation.ShouldBeNull();
        var json = SyntaxJson.Serialize(rule).GetRawText();
        var old = json.Replace(",\"implementation\":null", string.Empty, StringComparison.Ordinal);
        ((ValidationRuleSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(old))).Implementation.ShouldBeNull();
        Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(json.Replace("\"implementation\":null", "\"unknown\":null", StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    sealed class CountingWalker : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
