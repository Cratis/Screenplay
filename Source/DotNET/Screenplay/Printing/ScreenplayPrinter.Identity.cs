// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    void WriteIdentity(ScreenplayWriter writer, IdentitySyntax identity)
    {
        using var anchor = writer.Anchor(identity);
        writer.Line("identity");
        using (writer.Indent())
        {
            WriteDescription(writer, identity.Description, identity);
            foreach (var detail in identity.Details)
            {
                using var detailAnchor = writer.Anchor(detail);
                var name = detail.Name == "description" ? "@description" : detail.Name;
                var declaration = $"{name} {ScreenplaySyntaxText.TypeRef(detail.Type)}";
                switch (detail.Source)
                {
                    case ClaimIdentitySourceSyntax claim:
                        writer.Line($"{declaration} from claim {StringLiteral.Quote(claim.Claim)}", claim);
                        break;
                    case QueryIdentitySourceSyntax query:
                        writer.Line($"{declaration} from query {query.Query} by {writer.Expression(query.By)}", query);
                        break;
                    case CodeIdentitySourceSyntax code:
                        writer.Line(declaration);
                        using (writer.Indent())
                        {
                            using var sourceAnchor = writer.Anchor(code);
                            WriteCodeBlock(writer, code.Code);
                        }
                        break;
                    case FileIdentitySourceSyntax file:
                        writer.Line(declaration);
                        using (writer.Indent())
                        {
                            using var sourceAnchor = writer.Anchor(file);
                            WriteFile(writer, file.File);
                        }
                        break;
                    default:
                        throw new UnsupportedSyntaxForPrinting("identity source", detail.Source.GetType().Name);
                }
            }
        }
    }
}
