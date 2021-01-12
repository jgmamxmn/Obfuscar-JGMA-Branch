#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mono.Options;
using Rollbar;
using Rollbar.DTOs;

namespace Obfuscar
{
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification =
        "Reviewed. Suppression is OK here.")]
    internal static class Program
    {
        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("Obfuscar is available at https://www.obfuscar.com");
            Console.WriteLine("(C) 2007-2018, Ryan Williams and other contributors.");
            Console.WriteLine();
            Console.WriteLine("obfuscar [Options] [project_file] [project_file]");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
            Console.WriteLine("\nAdditional features/fixes implemented by JGMA:");
            Console.WriteLine("- FIXED HideStrings. Previously, a global HideStrings=false setting didn't seem\n  to be honored.");
            Console.WriteLine("- ADDED alternative alphabet. Specify a global option of CustomAlphabetFile\n  with a value specifying a path to a text file. Characters from that text\n  file will form the basis for class/method/property names (instead of the\n  Latin alphabet or, if enabled, the Korean alphabet).");
            Console.WriteLine("- ADDED alternative KeyContainer mode - if the value of the KeyContainer\n  setting starts with the prefix reg:, then the rest of the value should be a\n  registry key path whose value specifies the key name. The point of this\n  is to all a standardized config file usable across different machines (where\n  the key names won't necessarily be the same).");
            Console.WriteLine(("- ADDED MappingFormat as an ALTERNATIVE to XmlMapping. MappingFormat\n  will override XmlMapping. valid values are 'default' (follow XmlMapping),\n  'text' (conventional text file), 'xml' (same as XmlMapping=true), or\n  'tsv' or 'json' which lay out the values in a parseable (tab-separated or\n  as a JSON array, respectively) format incl. hex values for non-ANSI chars."));

        }

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification =
            "Reviewed. Suppression is OK here.")]
        private static int Main(string[] args)
        {
            bool showHelp = false;
            bool showVersion = false;
            bool suppress = false;

            OptionSet p = new OptionSet()
                .Add("h|?|help", "Print this help information.", delegate(string v) { showHelp = v != null; })
                .Add("s|suppress", "Suppress Rollbar crash report.", delegate(string v) { suppress = v != null; })
                .Add("V|version", "Display version number of this application.",
                    delegate(string v) { showVersion = v != null; });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return 0;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return 0;
            }

            if (showVersion)
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
                return 0;
            }

            if (extra.Count < 1)
            {
                ShowHelp(p);
                return 1;
            }

            if (!suppress)
            {
                RegisterRollbar();
            }

            int start = Environment.TickCount;
            foreach (var project in extra)
            {
                try
                {
                    Console.Write("Loading project {0}...", project);
                    Obfuscator obfuscator = new Obfuscator(project);
                    Console.WriteLine("Done.");

                    obfuscator.RunRules();

                    Console.WriteLine("Completed, {0:f2} secs.", (Environment.TickCount - start) / 1000.0);
                }
                catch (ObfuscarException e)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("An error occurred during processing:");
                    Console.Error.WriteLine(e.Message);
                    if (e.InnerException != null)
                        Console.Error.WriteLine(e.InnerException.Message);
                    return 1;
                }
            }

            return 0;
        }

        private static void RegisterRollbar()
        {
            Console.WriteLine("Note that Rollbar API is enabled by default to collect crashes. If you want to opt out, please run with -s switch");
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            RollbarLocator.RollbarInstance.Configure(
                new RollbarConfig("1dd3cf880c5a46eeb4338dbea73f9620")
                {
                    Environment = "production",
                    Transform = payload =>
                    {
                        payload.Data.Person = new Person(version)
                        {
                            UserName = $"{version}"
                        };
                    }
                });

            Application.ThreadException += (sender, args) =>
            {
                RollbarLocator.RollbarInstance.Error(args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                RollbarLocator.RollbarInstance.Error(args.ExceptionObject as System.Exception);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                RollbarLocator.RollbarInstance.Error(args.Exception);
            };
        }
    }
}
