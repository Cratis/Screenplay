// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_concept_compliance_refusals : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    [Theory]
    [InlineData("concept Protected : Enum pii\n  one\n  two", "concept")]
    [InlineData("concept Protected : Uuid pii\neventsource Records\n  identifier Protected", "concept")]
    [InlineData("concept Protected : Uuid pii\neventsource Records\n  identifier Protected", "source")]
    [InlineData("concept Protected : Uuid secret\neventsource Records\n  stream Details\n    streamId Protected", "stream")]
    [InlineData("concept Protected : Uuid pii\nmodule M\n  feature F\n    slice StateChange S\n      command Change\n        id Protected identifier", "command")]
    [InlineData("concept Protected : String pii\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value Protected", "event")]
    [InlineData("concept Protected : String secret\n  secret scope subject\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value Protected", "event")]
    [InlineData("concept Protected : String pii\ntype Details\n  value Protected\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        values Details[]", "event")]
    void should_report_the_binders_refusal_on_the_model_and_affected_member(string source, string member)
    {
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var readiness = new AuthoringReadiness(application);
        var node = Member(application, member);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("concept compliance");
        readiness.SyntaxOnly(node).ShouldBeTrue();
        readiness.ExecutionReadiness(node)!.ShouldContain("PLAY0268");
        Bind(source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    }

    [Theory]
    [InlineData("pii", "")]
    [InlineData("secret", "\n  secret scope namespace")]
    void should_leave_admitted_markers_executable(string marker, string qualifier)
    {
        var source = $"concept Protected : String {marker}{qualifier}\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        value Protected";
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var readiness = new AuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ExecutionReadiness(application.Concepts.Single()).ShouldBeNull();
        Bind(source).Success.ShouldBeTrue();
    }

    [Fact]
    void should_not_mark_an_unrelated_command_syntax_only()
    {
        const string source = "concept Protected : String pii\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value Protected\n      command Unrelated\n        id Uuid identifier";
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var readiness = new AuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.SyntaxOnly(Member(application, "command")).ShouldBeFalse();
    }

    static SyntaxNode Member(ApplicationSyntax application, string member) => member switch
    {
        "concept" => application.Concepts.Single(),
        "source" => application.EventSources.Single(),
        "stream" => application.EventSources.Single().Streams.Single(),
        "command" => application.Modules.Single().Features.Single().Slices.Single().Commands.Single(),
        _ => EventDeclarations.In(application.Modules.Single().Features.Single().Slices.Single()).Single()
    };
}
