using AIT.Tools.VisualStudioTextTransform.Properties;
using CommandLine;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

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
            // ////////////////////////
            //"C:\Users\yangjiefeng\Documents\coding.net\pmall\pmall.net.sln" -w "C:\Users\yangjiefeng\Documents\coding.net\pmall\dddml" -c "C:\Users\yangjiefeng\Documents\coding.net\pmall\docs\drafts\proj-creation-config.json"
            //arguments = new string[] {
            //    @"C:\Users\yangjiefeng\Documents\coding.net\pmall\pmall.net.sln",
            //    //"-w",
            //    //@"C:\Users\yangjiefeng\Documents\coding.net\pmall\dddml",
            //    //"-c",
            //    //@"C:\Users\yangjiefeng\Documents\coding.net\pmall\docs\drafts\proj-creation-config.json",
            //    "-a", "Empty"
            //};
            // ////////////////////////
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

            if (options.WatchtDir != null)
            {
                FileSystemWatcherHandler fileSystemWatcherHandler = new FileSystemWatcherHandler();

                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = options.WatchtDir;//@"d:DownLoads";//args[1];

                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                watcher.Filter = "*.yaml";
                // Add event handlers.              
                watcher.Changed += new FileSystemEventHandler((s, e) => fileSystemWatcherHandler.OnChanged(solutionFileName, options, s, e));
                watcher.Created += new FileSystemEventHandler((s, e) => fileSystemWatcherHandler.OnCreated(solutionFileName, options, s, e));
                watcher.Deleted += new FileSystemEventHandler((s, e) => fileSystemWatcherHandler.OnDeleted(solutionFileName, options, s, e));
                //watcher.Renamed += new RenamedEventHandler(OnChanged);
                // Begin watching.
                watcher.EnableRaisingEvents = true;
                // ////////////////////////////////////////////////////
                while (true)
                {
                    var k = System.Console.ReadLine();
                    if (k != null && k.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return 0;
                    }
                }
            }

            var result =
                TemplateProcessor.ProcessSolution(solutionFileName, options, GetFileNamePatterns(options.AggregateName)) ? 0 : 1;
            //System.Console.ReadKey();
            return result;
        }


        //private static void UpdateAggregate(string solutionFileName, Options options, string aggregateName)
        //{
        //    var patterns = GetFileNamePatterns(aggregateName);
        //    TemplateProcessor.ProcessSolution(solutionFileName, options, patterns);
        //}

        private static Regex[] GetFileNamePatterns(string aggregateName)
        {
            if (String.IsNullOrWhiteSpace(aggregateName))
            {
                return null;
            }
            //GenerateXxxxDomain 开头的脚本；
            //GenerateAggregatesHbm.tt 脚本重新生成 hbm 映射文件；
            //GenerateAggregatesResources.tt 重新生成 RESTful API；
            //GenerateBoundedContextMetadata.tt 更新元数据文件
            //GenerateBoundedContextDomainAggregatesMetadata.tt 更新元数据文件
            //GenerateAggregatesConfig.tt 更新配置文件
            var patterns = new Regex[] {
                new Regex(String.Format("Generate{0}Domain.*\\.tt", aggregateName)),
                new Regex("GenerateAggregates.*\\.tt"),
                new Regex("GenerateBoundedContext.*\\.tt"),
                new Regex("GenerateTrees.*\\.tt"),
                new Regex(".*ForeignKeyConstraints\\.tt"),
                new Regex(".*RViews\\.tt"),
                new Regex(".*RViewNameConflictedTables\\.tt"),
                new Regex(".*StateConstraints\\.tt"),
            };
            return patterns;
        }

     

    }
}