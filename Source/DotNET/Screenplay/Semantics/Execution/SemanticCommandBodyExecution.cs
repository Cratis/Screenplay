// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticCommandBodyExecution(ISemanticBodyExecutionProvider? provider, SemanticRuleBodyExecution? bodies)
{
    internal static readonly SemanticCommandBodyExecution None = new(null, null);
    internal bool HasProvider => provider is not null;

    internal SemanticCommandBodyResult Handle(SemanticExecutionPlan plan, SemanticCommand command, SemanticCommandContext context, CancellationToken cancellationToken)
    {
        var handler = command.Handler!;
        if (bodies?.FindBody(handler.RequirementId, SemanticImplementationRole.CommandHandler) is not { } body ||
            !IsVerified(plan, command, body))
        {
            return SemanticCommandBodyResult.Unsupported($"No resolved, content-verified result-v{handler.ResultVersion} handler is available.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        SemanticCommandBodyResult result;
        try
        {
            result = Task.Run(async () => await provider!.HandleAsync(body, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
        }
        catch (SemanticCommandBodyFaultException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SemanticCommandBodyFaultException(
                handler.RequirementId,
                exception is OperationCanceledException ? SemanticCommandBodyFaultKind.Cancelled : SemanticCommandBodyFaultKind.Faulted,
                exception);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return result ?? SemanticCommandBodyResult.Unsupported("The provider returned no handler result.");
    }

    static bool IsVerified(SemanticExecutionPlan plan, SemanticCommand command, SemanticImplementationBody body) =>
        command.Handler!.ResultVersion is 1 or 2 && HasIdentity(plan, command, body.Requirement) &&
        IsResolved(body.Requirement, command.Handler.ResultVersion) && SemanticRuleBodyExecution.MatchesHash(body);

    static bool HasIdentity(SemanticExecutionPlan plan, SemanticCommand command, SemanticImplementationRequirement requirement) =>
        requirement.Source.SemanticId == command.Id && requirement.Owner.Kind == SemanticKind.Command &&
        requirement.Owner.Name == command.Name && requirement.Member is null && !requirement.IsAmbiguous &&
        SemanticModelProvenance.Declares(plan.Model, requirement.Owner, command.Id) &&
        requirement.RequirementId == ImplementationRequirementIdentity.Create(command.Id, SemanticImplementationRole.CommandHandler, null);

    static bool IsResolved(SemanticImplementationRequirement requirement, int resultVersion) =>
        !string.IsNullOrWhiteSpace(requirement.Language) && requirement.AttachmentResolution == SemanticAttachmentResolution.Resolved &&
        requirement.ContextVersion == 1 && requirement.ResultVersion == resultVersion;
}
