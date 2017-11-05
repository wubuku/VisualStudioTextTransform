﻿using System;
using System.Diagnostics;
using AIT.Tools.VisualStudioTextTransform.Properties;
using CommandLine;

namespace AIT.Tools.VisualStudioTextTransform
{
    /// <summary>
    /// /
    /// </summary>
    public static class Program
    {
        private static readonly TraceSource Source = new TraceSource("AIT.Tools.VisualStudioTextTransform");

        /// <summary>
        /// /
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [STAThread]
        public static int Main(string[] arguments)
        {
            //System.Console.WriteLine(typeof(Uri).Assembly.Location); Console.ReadKey(); return 0;
            try
            {
                var result = ExecuteMain(arguments);
                //System.Threading.Thread.Sleep(10 * 1000);
                return result;

            }
            catch (Exception e)
            {
                Source.TraceEvent(TraceEventType.Critical, 1, Resources.Program_Main_Application_crashed_with___0_, e);
                return 1;
            }
        }

        private static int ExecuteMain(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                throw new ArgumentException(Resources.Program_Main_you_must_provide_a_solution_file);
            }
            var solutionFileName = arguments[0];
            var opts = new string[arguments.Length - 1];
            Array.Copy(arguments, 1, opts, 0, arguments.Length - 1);
            var options = new Options();
            Parser.Default.ParseArguments(opts, options);

            var result =
                TemplateProcessor.ProcessSolution(solutionFileName, options) ? 0 : 1;
            System.Console.ReadKey();
            return result;
        }

    }
}