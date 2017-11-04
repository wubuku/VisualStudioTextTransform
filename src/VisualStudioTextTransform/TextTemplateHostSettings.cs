using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace AIT.Tools.VisualStudioTextTransform
{
    public class TextTemplateHostSettings
    {
        //private static IList<Assembly> loadedAssemblies = new List<Assembly>();

        static TextTemplateHostSettings()
        {
            //IList<string> assemblyLoadFromPaths = new string[] { 
            //    @"C:\Users\yangjiefeng\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\vb3egovy.yav\T4Toolbox.dll",
            //    @"C:\Users\yangjiefeng\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\vb3egovy.yav\T4Toolbox.DirectiveProcessors.dll",
            //    @"C:\Users\yangjiefeng\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\vb3egovy.yav\T4Toolbox.VisualStudio.dll",
            //};
            //foreach (var p in assemblyLoadFromPaths)
            //{
            //    loadedAssemblies.Add(Assembly.LoadFrom(p));
            //}
        }

        private static TextTemplateHostSettings defaultInstance = new TextTemplateHostSettings();

        public static TextTemplateHostSettings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        public IList<string> IncludePaths
        {
            get 
            {
                return new string[] {
                    @"C:\Users\yangjiefeng\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\vb3egovy.yav\Include",
                    //@"C:\Users\yangjiefeng\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\vb3egovy.yav",
                };
            }
        }

        //public IList<Assembly> LoadedAssemblies
        //{
        //    get 
        //    {
        //        return loadedAssemblies;
        //    }
        //}

        //public Type GetType(string tn)
        //{
        //    Type t = null;
        //    foreach (var ass in LoadedAssemblies)
        //    {
        //        t = ass.GetType(tn);
        //        if (t != null)
        //        {
        //            break;
        //        }
        //    }
        //    return t;
        //}
        
    }
}
