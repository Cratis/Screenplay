// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides equivalent first-class and deprecated Studio text-rule source forms.
/// </summary>
public static class TextRulesCorpus
{
    const string Source = """
        concept Reference : String
          validate
            contains "-"
            not contains " "
            starts with "INV-"
            ends with "-NOK"
        module Billing
          feature Invoices
            slice StateChange Choose
              command ChooseReference
                reference Reference
                returns reference
              specification AcceptedReference
                when ChooseReference
                  reference = "INV-1-NOK"
                then returns "INV-1-NOK"
              specification RefusedWhitespace
                when ChooseReference
                  reference = "INV- 1-NOK"
                then error
        """;

    /// <summary>
    /// Gets source forms whose executable bytes and reference verdicts are identical.
    /// Non-lookaround contains and starts-with patterns stay unchanged, so only legacy lookaround
    /// spellings are substituted in the Studio form.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TextRules"))).ToImmutableArray();
            var studio = Source.Replace("not contains \" \"", "matches \"^(?![\\s\\S]* )\"", StringComparison.Ordinal)
                .Replace("ends with \"-NOK\"", "matches \"-NOK(?![\\s\\S])\"", StringComparison.Ordinal);

            return [Form("first-class", Source, catalog), Form("studio", studio, catalog)];
        }
    }

    static CanonicalCorpusSourceForm Form(string name, string source, ImmutableArray<byte> catalog) => new()
    {
        Name = name,
        IdentityCatalogBytes = catalog,
        Documents = [new() { StableKey = "application", DisplayPath = "application.play", Bytes = [.. Encoding.UTF8.GetBytes(source)] }]
    };
}
