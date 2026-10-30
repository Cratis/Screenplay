// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelProvenance;

public class when_recording_compilations : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticAddress _owner = null!;
    SemanticId _command;
    bool _compiledDeclares;
    bool _copyDeclares;
    bool _otherApplicationDeclares;
    bool _conflictedDeclares;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("handlers"), "handlers", "orders.play", a_command_handler.Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        _model = compilation.Value.Model;
        var requirement = compilation.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.CommandHandler);
        _owner = requirement.Owner;
        _command = requirement.Source.SemanticId;
    }

    void Because()
    {
        _compiledDeclares = SemanticModelProvenance.Declares(_model, _owner, _command);
        _copyDeclares = SemanticModelProvenance.Declares(_model with { }, _owner, _command);
        _otherApplicationDeclares = SemanticModelProvenance.Declares(_model, other_application.Of(_owner), _command);
        SemanticModelProvenance.Record(_model, ApplicationIdentity.Create("Billing"), new Dictionary<SemanticAddress, SemanticId>());
        _conflictedDeclares = SemanticModelProvenance.Declares(_model, _owner, _command);
    }

    [Fact] void should_declare_the_compiled_command() => _compiledDeclares.ShouldBeTrue();
    [Fact] void should_not_bind_a_model_copy() => _copyDeclares.ShouldBeFalse();
    [Fact] void should_not_declare_another_application() => _otherApplicationDeclares.ShouldBeFalse();
    [Fact] void should_fail_closed_after_a_conflicting_record() => _conflictedDeclares.ShouldBeFalse();
}
