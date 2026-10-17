// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.given;

public class a_reference_batch : Specification
{
    protected const string Left = "module Left\n  feature Helper\n  feature Consumer\n    depends on Helper\n";
    protected const string Right = "module Right\n  feature Helper\n  feature Consumer\n";
    protected const string Shipping = """
        slice StateChange ShipOrder
          command ShipOrder
            orderId OrderId identifier
            carrier Carrier
            produces event OrderShipped
              carrier Carrier = carrier
          constraint PublishOnce
            unique event OrderShipped
            unique event OrderRecalled
            message "Already shipped"
          event OrderPlaced
          event OrderPaid
          event OrderRecalled
          specification Shipping
            given OrderPlaced
              for "o1"
            given OrderPaid
              for "o1"
            when ShipOrder
              orderId = "o1"
              carrier = "c"
            then OrderShipped
              for "o1"
        """;
    protected ScreenplayWorkspace Workspace = null!;

    protected static string ChangedShipping => Shipping.Replace("    unique event OrderRecalled\n", string.Empty, StringComparison.Ordinal)
        .Replace("  event OrderRecalled", "  event OrderRecalled\n  event OrderLost", StringComparison.Ordinal)
        .Replace("    when ShipOrder", "    given OrderLost\n      for \"o1\"\n    when ShipOrder", StringComparison.Ordinal);

    protected void ShippingWorkspace() => Configure(
        ("application.play", "domain Acme\nconcept OrderId : String\nconcept Carrier : String\nimport \"Orders/Orders.play\"\n"),
        ("Orders/Orders.play", "module Orders\n  feature Fulfilment\n    import \"Ship.play\"\n"),
        ("Orders/Ship.play", Shipping));

    protected void Configure(params (string Path, string Source)[] sources) => Workspace = Create(sources);

    protected static ScreenplayWorkspace Create(params (string Path, string Source)[] sources) => ScreenplayWorkspace.Create("Acme",
        [.. sources.Select(source => WorkspaceDocument.Create(source.Path.Replace('/', '-'), PortablePlayPath.Parse(source.Path), Encoding.UTF8.GetBytes(source.Source)))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));

    protected static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAuthoringReferencePolicy policy, params (string Path, string Source)[] replacements)
    {
        var candidate = Create([.. workspace.Documents.Select(document => (document.Path.Value, replacements.FirstOrDefault(replacement => replacement.Path == document.Path.Value).Source ?? document.Text))]);
        var index = WorkspaceSyntaxIndex.Create(candidate);
        return workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveExactSource,
            ReferencePolicy = policy,
            Documents = [.. replacements.Select(replacement =>
            {
                var document = workspace.Documents.Single(document => document.Path.Value == replacement.Path);
                var root = index.Entries.Single(entry => entry.Handle.Document == document.Id && entry.Parent is null);
                return new ReplaceWorkspaceSyntaxDocument(document.Id, (ApplicationSyntax)root.Node) { SourceWitness = replacement.Source };
            })]
        });
    }
}
