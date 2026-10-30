// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticCommandBodyExecution(ISemanticBodyExecutionProvider? provider, SemanticRuleBodyExecution? bodies)
{
    internal static readonly SemanticCommandBodyExecution None = new(null, null);
    internal bool HasProvider => provider is not null;

    internal SemanticCommandBodyResult Handle(SemanticCommand command, SemanticCommandContext context, CancellationToken cancellationToken)
    {
        var handler = command.Handler!;
        if (handler.ResultVersion != 1 ||
            bodies?.FindBody(handler.RequirementId, SemanticImplementationRole.CommandHandler) is not { } body ||
            body.Requirement.Source.SemanticId != command.Id || body.Requirement.Owner.Kind != SemanticKind.Command ||
            body.Requirement.Owner.Name != command.Name || body.Requirement.Member is not null || body.Requirement.IsAmbiguous ||
            body.Requirement.RequirementId != ImplementationRequirementIdentity.Create(command.Id, SemanticImplementationRole.CommandHandler, null) ||
            string.IsNullOrWhiteSpace(body.Requirement.Language) || body.Requirement.AttachmentResolution != SemanticAttachmentResolution.Resolved ||
            body.Requirement.ContextVersion != 1 || body.Requirement.ResultVersion != 1 ||
            !SemanticRuleBodyExecution.MatchesHash(body))
        {
            return SemanticCommandBodyResult.Unsupported("No resolved, content-verified result-v1 handler is available.");
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
}
