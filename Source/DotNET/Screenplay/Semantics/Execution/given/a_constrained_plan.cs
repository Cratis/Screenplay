// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_constrained_plan : Specification
{
    protected const string ViolatedEventMessage = "Constraint 'OnePaymentPerAttempt' is violated: the event source already has the constrained event.";
    protected const string ViolatedValueMessage = "Constraint 'ProjectCodeIsUnique' is violated: another event source already holds the constrained value.";
    protected const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    protected const string Second = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
    protected const string Third = "9b2f4e1a-3c5d-4e6f-8a7b-1c2d3e4f5a6b";

    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                code String?
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  code = code
              event ProjectRegistered
                projectId Uuid
                code String?
              constraint ProjectCodeIsUnique
                unique code on ProjectRegistered
              specification CodeCollidesAcrossEventSources
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  code = "ALPHA"
                when RegisterProject
                  for "7c9e6679-7425-40de-944b-e07fc1f90ae7"
                  projectId = "7c9e6679-7425-40de-944b-e07fc1f90ae7"
                  code = "ALPHA"
                then error "Constraint 'ProjectCodeIsUnique' is violated: another event source already holds the constrained value."
              specification ReclaimingItsOwnCode
                given ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  code = "ALPHA"
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  code = "ALPHA"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  code = "ALPHA"
          feature Payments
            slice StateChange RecordPayment
              command RecordPayment
                paymentId Uuid identifier
                produces PaymentRecorded
                  for paymentId
                  paymentId = paymentId
              event PaymentRecorded
                paymentId Uuid
              constraint OnePaymentPerAttempt
                unique event PaymentRecorded
              specification RecordingAPaymentTwice
                given PaymentRecorded
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when RecordPayment
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then error "Constraint 'OnePaymentPerAttempt' is violated: the event source already has the constrained event."
            slice StateChange ReversePayment
              command ReversePayment
                paymentId Uuid identifier
                produces PaymentReversed
                  for paymentId
                  paymentId = paymentId
              event PaymentReversed
                paymentId Uuid
        """;

    protected ExecutableSemanticModel _model;
    protected SemanticExecutionPlan _plan;

    void Establish()
    {
        const string StableKey = "constrained-vector";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Constrained.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _model = compilation.Value!.Model;
        _plan = SemanticExecutionPlan.Compile(_model).Plan!;
    }

    protected SemanticExecutionResult RegisterProject(SemanticWorld world, string projectId, string? code) =>
        new SemanticEvaluator().Execute(
            _plan,
            world,
            SemanticExecutionRequest.Create(
                Command("RegisterProject").Id,
                [Value("RegisterProject", "projectId", SemanticValue.Text(projectId)), Value("RegisterProject", "code", Text(code))],
                []));

    protected SemanticExecutionResult RecordPayment(SemanticWorld world, string paymentId) =>
        new SemanticEvaluator().Execute(
            _plan,
            world,
            SemanticExecutionRequest.Create(Command("RecordPayment").Id, [Value("RecordPayment", "paymentId", SemanticValue.Text(paymentId))], []));

    protected SemanticFact ProjectRegistered(string projectId, string? code) =>
        Fact("ProjectRegistered", projectId, ("projectId", SemanticValue.Text(projectId)), ("code", Text(code)));

    protected SemanticFact PaymentRecorded(string paymentId) =>
        Fact("PaymentRecorded", paymentId, ("paymentId", SemanticValue.Text(paymentId)));

    protected SemanticFact PaymentReversed(string paymentId) =>
        Fact("PaymentReversed", paymentId, ("paymentId", SemanticValue.Text(paymentId)));

    protected SemanticWorld World(params SemanticFact[] facts) => SemanticWorld.Create([.. facts], []);

    // The language cannot yet declare release or ignore casing, so these contract features are set on the bound model.
    protected void Change(string constraint, Func<SemanticConstraint, SemanticConstraint> change)
    {
        var application = _model.Application with
        {
            Modules = [.. _model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Constraints = [.. slice.Constraints.Select(value => value.Name == constraint ? change(value) : value)]
                    })]
                })]
            })]
        };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        _plan = SemanticExecutionPlan.Compile(_model).Plan!;
    }

    protected SemanticEventContract Event(string name) => _plan.Events.Values.Single(_ => _.Name == name);

    SemanticCommand Command(string name) => _plan.Commands.Values.Single(_ => _.Name == name);

    SemanticPropertyValue Value(string command, string property, SemanticValue value) =>
        new(Command(command).Properties.Single(_ => _.Name == property).Id, value);

    SemanticFact Fact(string @event, string eventSource, params (string Property, SemanticValue Value)[] values)
    {
        var contract = Event(@event);
        return new(
            contract.Id,
            SemanticValue.Text(eventSource),
            [.. values.Select(_ => new SemanticPropertyValue(contract.Properties.Single(property => property.Name == _.Property).Id, _.Value))]);
    }

    static SemanticValue Text(string? value) => value is null ? SemanticValue.Null : SemanticValue.Text(value);
}
