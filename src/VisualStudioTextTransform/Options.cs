using CommandLine;

namespace AIT.Tools.VisualStudioTextTransform
{
    /// <summary>
    /// A options class for the Command-Line parser library.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// An overwrite for the TargetDir msbuild variable.
        /// </summary>
        [Option('t', "targetdir", DefaultValue = null, Required = false, HelpText = "Set a custom TargetDir reference.")]
        public string TargetDir
        {
            get;
            set;
        }

        /// <summary>
        /// Watch Dir.
        /// </summary>
        [Option('w', "watchdir", DefaultValue = null, Required = false, HelpText = "Watch a directory.")]
        public string WatchtDir
        {
            get;
            set;
        }

        /// <summary>
        /// Watch Dir.
        /// </summary>
        [Option('a', "aggregatename", DefaultValue = null, Required = false, HelpText = "Aggregate name.")]
        public string AggregateName
        {
            get;
            set;
        }


    }
}