// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_marking_event_subjects : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("Uuid", "")]
    [InlineData("String", "")]
    [InlineData("CustomerId", "concept CustomerId : Uuid\n")]
    [InlineData("CustomerId", "concept CustomerId : String\n")]
    [InlineData("CustomerId", "concept CustomerId : Int\n")]
    void should_accept_the_stream_identity_type_set(string type, string concepts)
    {
        var result = _compiler.Compile(concepts + Prefix + $"      event Changed\n        customerId {type} subject");
        result.Success.ShouldBeTrue();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties.Single().IsSubject.ShouldBeTrue();
    }

    [Theory]
    [InlineData("String optional")]
    [InlineData("String?")]
    [InlineData("Uuid[]")]
    [InlineData("Int")]
    [InlineData("Decimal")]
    [InlineData("Bool")]
    [InlineData("Date")]
    [InlineData("DateTime")]
    [InlineData("Value", "concept Value : Decimal\n")]
    [InlineData("Value", "concept Value : Bool\n")]
    [InlineData("Value", "concept Value : Date\n")]
    [InlineData("Value", "concept Value : DateTime\n")]
    [InlineData("Value", "concept Value : Enum\n  one\n")]
    [InlineData("Value", "type Value\n  key String\n")]
    void should_refuse_nonidentity_targets(string type, string declarations = "") =>
        _compiler.Compile(declarations + Prefix + $"      event Changed\n        customerId {type} subject").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSubjectType).ShouldBeTrue();

    [Theory]
    [InlineData("pii")]
    [InlineData("personal")]
    [InlineData("secret")]
    [InlineData("@pii")]
    [InlineData("sensitive")]
    [InlineData("@sensitive")]
    void should_refuse_plaintext_protected_keys(string marker) =>
        _compiler.Compile($"concept CustomerId : String {marker}\n" + Prefix + "      event Changed\n        customerId CustomerId subject").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ProtectedSubjectType && diagnostic.Message.Contains("surrogate", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_name_the_first_mark_on_each_extra_mark()
    {
        var result = _compiler.Compile(Prefix + "      event Changed\n        customerId Uuid subject\n        personId Uuid subject\n        employeeId String subject");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateEventSubject).ShouldEqual(2);
        result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateEventSubject).All(diagnostic => diagnostic.Message.Contains("customerId", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("command C\n        customerId String subject", DiagnosticCodes.InvalidSubjectOwner)]
    [InlineData("readmodel R\n        customerId String subject", DiagnosticCodes.ReadModelSubjectNotSupported)]
    [InlineData("command C\n        customerId String\n        returns\n          key String subject = customerId", DiagnosticCodes.InvalidSubjectOwner)]
    void should_explain_wrong_owners(string body, string code) =>
        _compiler.Compile(Prefix + "      " + body).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Fact]
    void should_isolate_the_subject_refusal_on_an_otherwise_valid_operation()
    {
        var result = _compiler.Compile("system Mailer\n" + Prefix + "      operation Send\n        uses Mailer\n        customerId Uuid subject\n        execute");
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSubjectOwner);
    }

    [Theory]
    [InlineData("trigger T\n  customerId Uuid subject")]
    [InlineData("trigger T\n  customerId Uuid\nmodule M\n  feature F\n    slice Automation S\n      reaction R\n        when T\n          customerId Uuid subject")]
    void should_refuse_trigger_and_reaction_data(string source)
    {
        var result = _compiler.Compile(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSubjectOwner);
        result.Diagnostics.Single().Message.ShouldContain("not trigger or reaction data properties (decision 0008");
    }

    [Fact]
    void should_keep_subject_as_a_trigger_property_name() => _compiler.Compile("trigger T\n  subject Uuid").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_already_refuse_subject_on_query_arguments() =>
        _compiler.Parse(Prefix + "      query Q => String\n        by customerId Uuid subject").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidQueryParameter).ShouldBeTrue();

    [Fact]
    void should_already_refuse_property_shapes_in_captures() =>
        _compiler.Parse(Prefix + "      capture Import\n        customerId Uuid subject").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownCaptureDirective).ShouldBeTrue();

    [Fact]
    void should_refuse_type_properties() => _compiler.Compile("type T\n  key String subject").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSubjectOwner).ShouldBeTrue();

    [Theory]
    [InlineData("subject optional")]
    [InlineData("subject generated")]
    [InlineData("subject identifier")]
    [InlineData("subject subject")]
    void should_explain_bad_modifier_order(string modifiers) =>
        _compiler.Parse(Prefix + $"      event Changed\n        customerId Uuid {modifiers}").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSubjectModifierOrder).ShouldBeTrue();

    [Fact]
    void should_roundtrip_standalone_inline_and_contextual_names()
    {
        const string source = Prefix + "      event Changed\n        subject String\n        customerId Uuid subject\n      command Change\n        customerId Uuid\n        produces event Corrected\n          subject String = \"note\"\n          customerId Uuid subject = customerId";
        var result = _compiler.Parse(source);
        result.Success.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(result.Value!);
        printed.ShouldContain("customerId Uuid subject = customerId");
        printed.ShouldContain("subject String");
        SyntaxJson.StructurallyEqual(result.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
        SyntaxJson.Serialize(result.Value!).GetRawText().ShouldContain("\"isSubject\":true");
    }
}
