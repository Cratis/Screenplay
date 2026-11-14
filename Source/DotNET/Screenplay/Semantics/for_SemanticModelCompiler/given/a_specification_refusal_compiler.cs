// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler.given;

public class a_specification_refusal_compiler : Specification
{
    protected const string Source = """
        module M
          feature F
            slice StateChange Recording
              event Recorded
                amount Int
              command Record
                id String identifier
                amount Int
                produces Recorded
                  for id
                  amount = amount
              specification RecordingWorks
                when Record amount = 10
                  id = "record"
                then Recorded amount = 10
            slice StateChange Confirming
              event Confirmed
                amount Int
              command Confirm
                id String identifier
                amount Int
                produces Confirmed
                  for id
                  amount = amount
              specification ConfirmationWorks
                when Confirm amount = 20
                  id = "confirm"
                then Confirmed amount = 20
        """;

    protected static CompilationResult<SemanticCompilation> Compile(string source, string applicationName = "App", SemanticIdentityCatalog? identities = null)
    {
        var catalog = identities ?? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        return new SemanticModelCompiler().Compile(applicationName, SemanticDocumentSet.Create([document], catalog));
    }

    protected static SemanticSlice Recording(SemanticCompilation compilation) => compilation.Model.Application.Modules.Single().Features.Single().Slices[0];

    protected static SemanticSlice Confirming(SemanticCompilation compilation) => compilation.Model.Application.Modules.Single().Features.Single().Slices[1];
}
