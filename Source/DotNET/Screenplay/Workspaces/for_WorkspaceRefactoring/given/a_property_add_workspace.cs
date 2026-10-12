// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_property_add_workspace : a_property_chain_workspace
{
    protected WorkspaceSyntaxEntry Owner(string name, uint? generation = null) => WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => (entry.Node is CommandSyntax or EventSyntax or ReadModelSyntax) && WorkspaceReferenceBindings.Name(entry.Node) == name && (generation is null || (entry.Node is EventSyntax @event && @event.Generation == generation)));
    protected WorkspaceSyntaxEntry Production(string name = "Registered") => WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ProducesSyntax production && production.Event == name);
    protected WorkspacePropertyRequest AddRequest(string owner = "Register", uint? generation = null) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Action = "add",
        Owner = Owner(owner, generation).Handle,
        ExpectedOwnerName = owner,
        Name = "priority",
        Type = "String",
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
    protected WorkspacePropertyHop AddHop(string owner, string name = "priority", uint? generation = null) => new(Owner(owner, generation).Handle, Name: name);
    protected WorkspacePropertyHop MappingHop(string production = "Registered", string value = "priority") => new(Production(production).Handle, Value: value);
}
