// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_DescriptionParser;

public class when_describing_additional_declarations
{
    [Theory]
    [InlineData("concept Value : String\n  description \"Value intent\"", "ConceptSyntax")]
    [InlineData("policy Access\n  description \"Value intent\"\n  require authenticated", "PolicySyntax")]
    [InlineData("module M\n  form F for C\n    description \"Value intent\"\n  feature X\n    slice StateChange S\n      command C", "FormSyntax")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      event E\n      constraint C\n        description \"Value intent\"\n        unique event E", "UniqueEventConstraintSyntax")]
    [InlineData("module M\n  feature F\n    slice StateView S\n      event E\n      projection P\n        description \"Value intent\"\n        from E", "ProjectionSyntax")]
    [InlineData("module M\n  feature F\n    slice StateView S\n      screen Home\n        description \"Value intent\"", "ScreenSyntax")]
    public void should_parse_and_print_the_description_without_changing_other_syntax(string source, string kind)
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Compile(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var visitor = new Descriptions();
        visitor.VisitApplication(parsed.Value!);
        visitor.Nodes.Single(node => node.GetType().Name == kind).GetType().GetProperty("Description")!.GetValue(visitor.Nodes.Single(node => node.GetType().Name == kind)).ShouldEqual("Value intent");
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        SyntaxJson.StructurallyEqual(parsed.Value!, compiler.Compile(printed).Value!).ShouldBeTrue();
    }

    [Fact]
    public void should_keep_a_bare_description_enum_value_and_escape_it_when_printing()
    {
        var parsed = new ScreenplayCompiler().Compile("concept State : Enum\n  description\n  open");
        parsed.Diagnostics.ShouldBeEmpty();
        parsed.Value!.Concepts.Single().Values.ShouldContainOnly("description", "open");
        new ScreenplayPrinter().Print(parsed.Value).ShouldContain("@description");
    }

    [Theory]
    [InlineData("policy Access\n  description \"Only intent\"", DiagnosticCodes.PolicyWithoutRequirement)]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      constraint C\n        description \"Only intent\"", DiagnosticCodes.ConstraintWithoutRule)]
    public void should_not_treat_a_description_as_an_implementation_or_rule(string source, string code) => new ScreenplayCompiler().Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    sealed class Descriptions : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
