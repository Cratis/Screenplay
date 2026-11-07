// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand.given;

public class a_screenplay_5_model : Specification
{
    protected const string Screenplay5 =
        """
        layout AppShell
          category Shell
          // Modules add their destinations here.
          navigation contributes Navigation
          content
          arrangement flow
            row
              navigation width 240
              content grow

        module Shop
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
              screen OrderList
                data OrderRow[] via query AllOrders

        """;

    protected string Root;
    protected string Document;
    protected StringWriter Output;
    protected StringWriter Error;

    void Establish()
    {
        Root = SpecFixtureDirectory.Create("migrate");
        Document = Path.Combine(Root, "application.play");
        File.WriteAllText(Document, Screenplay5);
        Output = new();
        Error = new();
    }

    void Destroy()
    {
        Output.Dispose();
        Error.Dispose();
        SpecFixtureDirectory.Delete(Root);
    }
}
