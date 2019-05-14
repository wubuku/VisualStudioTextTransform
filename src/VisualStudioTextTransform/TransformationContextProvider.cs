// 
// FROM: namespace T4Toolbox.VisualStudio.TransformationContextProvider
//
namespace AIT.Tools.VisualStudioTextTransform
{
    // <copyright file="TransformationContextProvider.cs" company="Oleg Sych">
    //  Copyright © Oleg Sych. All Rights Reserved.
    // </copyright>
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TextTemplating.VSHost;
    using T4Toolbox;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections.Generic;
    using Dddml.T4.ProjectTools;

    internal class TransformationContextProvider : MarshalByRefObject, ITransformationContextProvider
    {
        private readonly IServiceProvider serviceProvider;

        internal TransformationContextProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        string ITransformationContextProvider.GetMetadataValue(object hierarchyObject, string fileName, string metadataName)
        {
            uint fileItemId;
            var hierarchy = (IVsHierarchy)hierarchyObject;
            ErrorHandler.ThrowOnFailure(hierarchy.ParseCanonicalName(fileName, out fileItemId));

            // Try getting metadata from the file itself
            string metadataValue;
            var propertyStorage = (IVsBuildPropertyStorage)hierarchyObject;
            if (ErrorHandler.Succeeded(propertyStorage.GetItemAttribute(fileItemId, metadataName, out metadataValue)))
            {
                return metadataValue;
            }

            // Try getting metadata from the parent
            object parentItemId;
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(fileItemId, (int)__VSHPROPID.VSHPROPID_Parent, out parentItemId));
            if (ErrorHandler.Succeeded(propertyStorage.GetItemAttribute((uint)(int)parentItemId, metadataName, out metadataValue)))
            {
                return metadataValue;
            }

            return string.Empty;
        }

        string ITransformationContextProvider.GetPropertyValue(object hierarchy, string propertyName)
        {
            var propertyStorage = (IVsBuildPropertyStorage)hierarchy;

            string propertyValue;
            if (ErrorHandler.Failed(propertyStorage.GetPropertyValue(propertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out propertyValue)))
            {
                // Property does not exist. Return an empty string.
                propertyValue = string.Empty;
            }

            return propertyValue;
        }

        /// <summary>
        /// Writes generated content to output files and deletes the old files that were not regenerated.
        /// </summary>
        /// <param name="inputFile">
        /// Full path to the template file.
        /// </param>
        /// <param name="outputFiles">
        /// A collection of <see cref="OutputFile"/> objects produced by the template.
        /// </param>
        void ITransformationContextProvider.UpdateOutputFiles(string inputFile, OutputFile[] outputFiles)
        {
            if (inputFile == null)
            {
                throw new ArgumentNullException("inputFile");
            }

            if (!Path.IsPathRooted(inputFile))
            {
                throw new ArgumentException("Input File Path Must Be Absolute", "inputFile");
            }

            if (outputFiles == null)
            {
                throw new ArgumentNullException("outputFiles");
            }

            foreach (OutputFile newOutput in outputFiles)
            {
                if (newOutput == null)
                {
                    throw new ArgumentException("Output File Is Null", "outputFiles");
                }
            }
            //System.Console.WriteLine(outputFiles.Count());

            // Validate the output files immediately. Exceptions will be reported by the templating service.
            // var manager = new OutputFileManager(this.serviceProvider, inputFile, outputFiles);
            // manager.Validate();

            // Store the actual output file name
            OutputFile defaultOutput = outputFiles.FirstOrDefault(output => string.IsNullOrEmpty(output.File));
            if (defaultOutput != null)
            {
                defaultOutput.File = Path.GetFileName(
                    Path.GetFileNameWithoutExtension(inputFile) + "." + this.GetTransformationOutputExtensionFromHost()
                    );
            }

            var manager = GetOutputFileManager(inputFile, outputFiles);
            InvokeOutputFileManagerValidate(manager);

            InvokeOutputFileManagerDoWork(manager);
            
            // //////////////////
            AddItemsToProject(inputFile, outputFiles);
            var removedFileNames = RemoveNotGeneratedItemsFromProject(inputFile, outputFiles);   
            // //////////////////

            //    // Wait for the default output file to be generated
            //    var watcher = new FileSystemWatcher();
            //    watcher.Path = Path.GetDirectoryName(inputFile);
            //    watcher.Filter = Path.GetFileNameWithoutExtension(inputFile) + "*." + this.GetTransformationOutputExtensionFromHost();

            //    FileSystemEventHandler runManager = (sender, args) =>
            //    {
            //        watcher.Dispose();

            //        // Store the actual output file name
            //        OutputFile defaultOutput = outputFiles.FirstOrDefault(output => string.IsNullOrEmpty(output.File));
            //        if (defaultOutput != null)
            //        {
            //            defaultOutput.File = Path.GetFileName(args.FullPath);
            //        }

            //        // Finish updating the output files on the UI thread
            //        ThreadHelper.Generic.BeginInvoke(manager.DoWork);
            //    };

            //    watcher.Created += runManager;
            //    watcher.Changed += runManager;
            //    watcher.EnableRaisingEvents = true;
        }

        private IList<string> RemoveNotGeneratedItemsFromProject(string templateFileName, OutputFile[] outputFiles)
        {
            Debug.Assert(templateFileName.StartsWith(ProjectDirectory, StringComparison.OrdinalIgnoreCase), "Template file-name is not within the project directory.");
            if (String.IsNullOrWhiteSpace(ProjectDirectory))
            {
                throw new NullReferenceException("ProjectDirectory");
            }
            var excludedFileNames = new List<string>();
            foreach (var outputFile in outputFiles)
            {
                var outputFileName = outputFile.File;
                var includedFileName = GetIncludedFileName(templateFileName, outputFileName);
                excludedFileNames.Add(includedFileName);
            }
            var dependentUponFileName = GetDependentUponTemplateFileName(templateFileName);
            var removedFileNames = MSBuildProjectFileUtils.RemoveItemsDependentUpon(ProjectFullPath, dependentUponFileName, excludedFileNames);
            foreach (var fn in removedFileNames)
            {
                var fp = Path.Combine(ProjectDirectory, fn);
                if (File.Exists(fp))
                {
                    File.Delete(fp);
                }
            }
            return removedFileNames;
        }

        internal string ProjectFullPath { get; set; }

        internal string ProjectDirectory
        {
            get {return Path.GetDirectoryName(ProjectFullPath);}
        }

        private void AddItemsToProject(string templateFileName, OutputFile[] outputFiles)
        {
            Debug.Assert(templateFileName.StartsWith(ProjectDirectory, StringComparison.OrdinalIgnoreCase), "Template file-name is not within the project directory.");
            if (String.IsNullOrWhiteSpace(ProjectDirectory))
            {
                throw new NullReferenceException("ProjectDirectory");
            }
            bool isFirstFile = true;
            foreach(var outputFile in outputFiles)
            {
                var outputFileName = outputFile.File;
                var includedFileName = GetIncludedFileName(templateFileName, outputFileName);
                var dependentUponFileName = GetDependentUponTemplateFileName(templateFileName);
                if (String.Equals(outputFile.ItemType, "EmbeddedResource", StringComparison.InvariantCultureIgnoreCase))
                {
                    MSBuildProjectFileUtils.IncludeEmbeddedResourceIfNotIncluded(ProjectFullPath, includedFileName, dependentUponFileName);
                }
                else
                {
                    if (IsCompileFile(outputFileName))
                    {
                        MSBuildProjectFileUtils.IncludeCompileIfNotIncluded(ProjectFullPath, includedFileName, dependentUponFileName);
                    }
                    else
                    {
                        if (isFirstFile && IsTxtFile(outputFileName) && outputFiles.Length > 1)
                        {
                            continue;
                        }
                        MSBuildProjectFileUtils.IncludeContentIfNotIncluded(ProjectFullPath, includedFileName, dependentUponFileName);
                    }
                }
                // /////////////////
                isFirstFile = false;
            }
        }

        private string GetDependentUponTemplateFileName(string templateFile)
        {
            var templateFileRelativePath = templateFile.Substring(ProjectDirectory.Length);
            return TrimDirectorySeparatorChar(templateFileRelativePath);
        }

        private string GetIncludedFileName(string templateFile, string outputFile)
        {
            var templateFileRelativePath = templateFile.Substring(ProjectDirectory.Length);
            var templateFileRelativeDir = TrimDirectorySeparatorChar(Path.GetDirectoryName(templateFileRelativePath));
            return Path.Combine(templateFileRelativeDir, outputFile);
        }

        private static string TrimDirectorySeparatorChar(string path)
        {
            if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path = path.Substring(1);
            }
            return path;
        }

        private static bool IsTxtFile(string filename)
        {
            var idx = filename.LastIndexOf(".");
            if (idx >= 0)
            {
                var ext = filename.Substring(idx + 1).ToLowerInvariant();
                if (ext == "txt")
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCompileFile(string filename)
        {
            var idx = filename.LastIndexOf(".");
            if (idx >= 0)
            {
                var ext = filename.Substring(idx + 1).ToLowerInvariant();
                if (ext == "vb" || ext == "cs")// || ext == "java"
                {
                    return true;
                }
            }
            return false;
        }

        private void InvokeOutputFileManagerDoWork(object manager)
        {
            var method = manager.GetType().GetMethod("DoWork");
            method.Invoke(manager, new object[] { });
        }

        private void InvokeOutputFileManagerValidate(object manager)
        {
            var method = manager.GetType().GetMethod("Validate");
            method.Invoke(manager, new object[] { });
        }

        private object GetOutputFileManager(string inputFile, OutputFile[] outputFiles)
        {
            var managerType = typeof(T4Toolbox.VisualStudio.ScriptFileGenerator).Assembly.GetType("T4Toolbox.VisualStudio.OutputFileManager");
            ConstructorInfo ctr = managerType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null,
                new Type[] { typeof(IServiceProvider), typeof(string), typeof(OutputFile[]) }, new ParameterModifier[0]);
            var manager = ctr.Invoke(new object[] { this.serviceProvider, inputFile, outputFiles });
            return manager;
        }

        /*
        /// <summary>
        /// Adds the <see cref="ITransformationContextProvider"/> service in the specified <paramref name="container"/>.
        /// </summary>
        /// <param name="container">
        /// An <see cref="IServiceContainer"/> object that will be providing the <see cref="ITransformationContextProvider"/> service.
        /// </param>
        internal static void Register(IServiceContainer container)
        {
            container.AddService(typeof(ITransformationContextProvider), CreateService, promote: true);
        }

        private static object CreateService(IServiceContainer container, Type serviceType)
        {
            if (serviceType == typeof(ITransformationContextProvider))
            {
                return new TransformationContextProvider(container);
            }

            return null;
        }
        */

        private string GetTransformationOutputExtensionFromHost()
        {
            var components = (ITextTemplatingComponents)this.serviceProvider.GetService(typeof(STextTemplating));
            var callback = components.Callback as TextTemplatingCallback; // Callback can be passed to ITextTemplating.ProcessTemplate by user code.
            if (callback == null)
            {
                throw new InvalidOperationException("A TextTemplatingCallback is expected from ITextTemplatingComponents.Callback.");
            }

            if (callback.Extension == null)
            {
                throw new InvalidOperationException("TextTemplatingCallback was not initialized (Extension is null).");
            }

            return callback.Extension.TrimStart('.');
        }
    }
}
