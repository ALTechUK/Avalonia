using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out MessageImportance outputImportance);
            var writtenFilePaths = new List<string>();

            CopyFailRetryDelayMs = CopyFailRetryDelayMs == 0 ? 100 : CopyFailRetryDelayMs;
            OutputPath ??= AssemblyFile;
            RefOutputPath ??= RefAssemblyFile;
            var outputPdb = GetPdbPath(OutputPath);
            var input = AssemblyFile;
            var refInput = RefOutputPath;
            var inputPdb = GetPdbPath(input);
            // Make a copy and delete the original file to prevent MSBuild from thinking that everything is OK
            if (OriginalCopyPath != null)
            {
                var originalCopyPathRef = Path.ChangeExtension(OriginalCopyPath, ".ref.dll");

                MultipleTryRun(() => File.Copy(AssemblyFile, OriginalCopyPath, true));
                writtenFilePaths.Add(OriginalCopyPath);
                input = OriginalCopyPath;
                MultipleTryRun(() => File.Delete(AssemblyFile));

                if (File.Exists(inputPdb))
                {
                    var copyPdb = GetPdbPath(OriginalCopyPath);
                    MultipleTryRun(() => File.Copy(inputPdb, copyPdb, true));
                    writtenFilePaths.Add(copyPdb);
                    MultipleTryRun(() => File.Delete(inputPdb));
                    inputPdb = copyPdb;
                }
                
                if (!string.IsNullOrWhiteSpace(RefAssemblyFile) && File.Exists(RefAssemblyFile))
                {
                    // We also copy ref assembly just for case if needed later for testing.
                    // But do not remove the original one, as MSBuild actually complains about it with multi-thread compiling.
                    MultipleTryRun(() => File.Copy(RefAssemblyFile, originalCopyPathRef, true));
                    writtenFilePaths.Add(originalCopyPathRef);
                    refInput = originalCopyPathRef;
                }
            }

            var msg = $"CompileAvaloniaXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{OutputPath}";
            BuildEngine.LogMessage(msg, outputImportance < MessageImportance.Low ? MessageImportance.High : outputImportance);

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine,
                input, OutputPath,
                refInput, RefOutputPath,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(),
                ProjectDirectory, VerifyIl, DefaultCompileBindings, outputImportance,
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null, SkipXamlCompilation, DebuggerLaunch);
            if (!res.Success)
            {
                WrittenFilePaths = writtenFilePaths.ToArray();
                return false;
            }

            if (!res.WrittenFile)
            {
                MultipleTryRun(() => File.Copy(input, OutputPath, true));
                if (File.Exists(inputPdb))
                    MultipleTryRun(() => File.Copy(inputPdb, outputPdb, true));
            }
            else if (!string.IsNullOrWhiteSpace(RefOutputPath) && File.Exists(RefOutputPath))
                writtenFilePaths.Add(RefOutputPath);

            writtenFilePaths.Add(OutputPath);
            if (File.Exists(outputPdb))
                writtenFilePaths.Add(outputPdb);

            WrittenFilePaths = writtenFilePaths.ToArray();
            return true;
        }

        /// <summary>
        /// Try running some work and if an exception is raised wait and try a couple more times
        /// </summary>
        /// <remarks>
        /// Sometimes a copy action can fail so we work around that by just waiting a bit for any other build to finish.
        /// 
        /// This happens regularly if the same project is built more than once (perhaps due to publishing to multiple outputs)
        /// the most common offender is the copy inputPdb -> copyPdb (by default copy to original.pdb) since the pdb is still
        /// in use by some part of msbuild? so a small delay gets around this
        /// </remarks>
        void MultipleTryRun(Action work)
        {
            int retryCount = 3;
            do
            {
                try
                { 
                    work();
                    break;
                }
                catch
                {
                    Thread.Sleep(CopyFailRetryDelayMs); //wait just a bit
                    if (retryCount-- == 0)
                        throw;
                }
            }
            while (retryCount > 0);
        }

        string GetPdbPath(string p)
        {
            var d = Path.GetDirectoryName(p);
            var f = Path.GetFileNameWithoutExtension(p);
            var rv = f + ".pdb";
            if (d != null)
                rv = Path.Combine(d, rv);
            return rv;
        }
        
        [Required]
        public string AssemblyFile { get; set; }
        [Required]
        public string ReferencesFilePath { get; set; }
        [Required]
        public string OriginalCopyPath { get; set; }
        [Required]
        public string ProjectDirectory { get; set; }
        
        public string RefAssemblyFile { get; set; }
        public string RefOutputPath { get; set; }
        
        public string OutputPath { get; set; }

        public bool VerifyIl { get; set; }

        public bool DefaultCompileBindings { get; set; }
        
        public bool SkipXamlCompilation { get; set; }
        
        public string AssemblyOriginatorKeyFile { get; set; }
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        public string ReportImportance { get; set; }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool DebuggerLaunch { get; set; }

        public int CopyFailRetryDelayMs { get; set; }
        
        [Output]
        public string[] WrittenFilePaths { get; private set; } = Array.Empty<string>();
    }
}
