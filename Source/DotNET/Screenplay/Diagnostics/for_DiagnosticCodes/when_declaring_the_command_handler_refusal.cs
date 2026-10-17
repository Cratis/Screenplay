// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticCodes;

public class when_declaring_the_command_handler_refusal : Specification
{
    CompilerContractCatalog.DiagnosticFact _descriptor;

    void Because() => _descriptor = CompilerContractCatalog.Diagnostics.Single(fact => fact.Code == DiagnosticCodes.CommandHandlerRequiresAttachment);

    [Fact] void should_have_its_own_stable_code() => _descriptor.Code.ShouldEqual("PLAY0800");
    [Fact] void should_report_an_error() => _descriptor.Severities.ShouldContainOnly(DiagnosticSeverity.Error);
    [Fact] void should_describe_the_attachment_requirement() => _descriptor.Title.ShouldEqual("A command handler requires a constrained implementation attachment.");
    [Fact] void should_declare_one_argument() => _descriptor.Arguments.Count.ShouldEqual(1);
    [Fact] void should_name_the_command_subject() => _descriptor.Arguments.Single().Name.ShouldEqual("subject");
    [Fact] void should_use_a_declaration_argument() => _descriptor.Arguments.Single().Kind.ShouldEqual("declaration");
    [Fact] void should_require_the_subject() => _descriptor.Arguments.Single().Optional.ShouldBeFalse();
}
