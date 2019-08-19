using AIT.Tools.VisualStudioTextTransform.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace AIT.Tools.VisualStudioTextTransform
{
    public class FileSystemWatcherHandler
    {

        private readonly MemoryCache _memCache;
        private readonly CacheItemPolicy _cacheItemPolicy;
        private const int CacheTimeMilliseconds = 600;

        public FileSystemWatcherHandler()
        {
            _memCache = MemoryCache.Default;
            _cacheItemPolicy = new CacheItemPolicy()
            {
                RemovedCallback = OnRemovedFromCache
            };
        }

         // Handle cache item expiring 
        private void OnRemovedFromCache(CacheEntryRemovedArguments args)
        {
            if (args.RemovedReason != CacheEntryRemovedReason.Expired) return;

            var val = (Tuple<string, string, Options, FileSystemEventArgs>)args.CacheItem.Value;
            //Console.WriteLine($"Let's now respond to the event {e.ChangeType} on {e.FullPath}");
            string solutionFileName = val.Item1;
            string aggregateName = val.Item2;
            Options options = val.Item3;
            FileSystemEventArgs e = val.Item4;
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (options != null && options.ConfigFile != null)
                {
                    UpdateAggregateScripts(options);
                }
                UpdateAggregate(solutionFileName, aggregateName);
            }
            else if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (options != null && options.ConfigFile != null)
                {
                    UpdateAggregateScripts(options);
                    UpdateAggregate(solutionFileName, aggregateName);
                }
            }

        }

        private static void UpdateAggregateScripts(Options options)
        {
            var projToolFileName = Settings.Default.ProjectToolsFileName;
            if (String.IsNullOrWhiteSpace(projToolFileName)) 
            {
                Console.WriteLine("ProjectToolsFileName is null!");
                return; 
            }
            if (!File.Exists(projToolFileName))
            {
                Console.WriteLine("ProjectToolsFileName NOT Exists!");
                return;
            }
            var configFile = options.ConfigFile;
            Process p = new Process();
            p.StartInfo.FileName = projToolFileName;
            p.StartInfo.Arguments = String.Format(" {0} -u A", configFile);
            SetAndStartProcess(p);
        }

        //// Add file event to cache (won't add if already there so assured of only one occurance)
        //private void OnChanged(object source, FileSystemEventArgs e)
        //{
        //    _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(CacheTimeMilliseconds);
        //    _memCache.AddOrGetExisting(e.Name, e, _cacheItemPolicy);
        //}

        /// <summary>
        /// Add file event to cache (won't add if already there so assured of only one occurance)
        /// </summary>
        /// <param name="solutionFileName"></param>
        /// <param name="options"></param>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnChanged(string solutionFileName, Options options, object source, FileSystemEventArgs e)
        {
            var aggregateName = GetAggregateName(e);
            MemCacheAdd(solutionFileName, aggregateName, options, e);
        }


        public void OnCreated(string solutionFileName, Options options, object source, FileSystemEventArgs e)
        {
            var aggregateName = GetAggregateName(e);
            MemCacheAdd(solutionFileName, aggregateName, options, e);
        }

        public void OnDeleted(string solutionFileName, Options options, object source, FileSystemEventArgs e)
        {
            var aggregateName = GetAggregateName(e);
            MemCacheAdd(solutionFileName, aggregateName, options, e);
        }

        public void OnRenamed(string solutionFileName, Options options, object source, RenamedEventArgs e)
        {
        }

        private void MemCacheAdd(string solutionFileName, string aggregateName, Options options, FileSystemEventArgs e)
        {
            var val = new Tuple<string, string, Options, FileSystemEventArgs>(solutionFileName, aggregateName, options, e);
            _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(CacheTimeMilliseconds);
            _memCache.AddOrGetExisting(e.Name, val, _cacheItemPolicy);
        }

        private static string GetAggregateName(FileSystemEventArgs e)
        {
            var fp = e.FullPath;
            var fn = Path.GetFileName(fp);
            var aggregateName = fn.Substring(0, fn.LastIndexOf("."));
            return aggregateName;
        }

        private static void UpdateAggregate(string solutionFileName, string aggregateName)
        {
            Process p = new Process();
            p.StartInfo.FileName = typeof(Program).Assembly.Location;//"VisualStudioTextTransform.exe";
            p.StartInfo.Arguments = String.Format(" {0} -a {1}", solutionFileName, aggregateName);
            SetAndStartProcess(p);
        }

        private static void SetAndStartProcess(Process p)
        {
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            p.EnableRaisingEvents = true;

            p.Exited += new EventHandler(p_Exited);
            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            p.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            p.Start();
            p.StandardInput.WriteLine("");
            p.StandardInput.WriteLine("");

            //开始异步读取输出
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            //调用WaitForExit会等待Exited事件完成后再继续往下执行。
            p.WaitForExit();
            p.Close();
        }

        // //////////////////////////////

        static void p_OutputDataReceived(Object sender, DataReceivedEventArgs e)
        {
            //这里是正常的输出
            Console.WriteLine(e.Data);

        }

        static void p_ErrorDataReceived(Object sender, DataReceivedEventArgs e)
        {
            //这里得到的是错误信息
            Console.WriteLine(e.Data);

        }

        static void p_Exited(Object sender, EventArgs e)
        {
            Console.WriteLine("finish");
        }

        // //////////////////////////////

    }
}
