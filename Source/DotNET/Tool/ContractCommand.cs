// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool;

static class ContractCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 0 && (args.Length != 2 || args[0] != CliCommandCatalog.Output.Name || !CliCommandCatalog.Contract.Options.Contains(CliCommandCatalog.Output) || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith('-')))
        {
            error.WriteLine($"Usage: {CliCommandCatalog.Contract.Usage}");
            return 2;
        }

        try
        {
            // Generate completely before opening any output file. An unclassified fact never publishes partial JSON.
            var json = ScreenplayContract.Serialize();
            if (args.Length == 0)
            {
                output.Write(json);
            }
            else
            {
                WriteWhenChanged(args[1], json);
            }

            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidScreenplayContract)
        {
            error.WriteLine($"Could not write the Screenplay contract: {exception.Message}");
            return 2;
        }
    }

    // Leave an identical file untouched so its timestamp does not invalidate downstream incremental builds.
    static void WriteWhenChanged(string path, string json)
    {
        var encoding = new UTF8Encoding(false);
        var bytes = encoding.GetBytes(json);
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
        {
            return;
        }

        File.WriteAllBytes(path, bytes);
    }
}
