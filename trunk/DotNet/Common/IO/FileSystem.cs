using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Common.IO
{
    public interface IApplyFileOperation
    {
        object InitFileOperation(object initialState, TextWriter log);
        object ExecuteFileOperation(string filePath, object state, TextWriter log);
        void FinalizeFileOperation(object initialState, object finalExecutionState, TextWriter log);
    }

    public static partial class FS
    {
        public static FileStream OpenRead(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static FileStream OpenWrite(string filePath)
        {
            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public static FileStream OpenReadWrite(string filePath)
        {
            return new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public static void ApplyFileOperation(IApplyFileOperation fileOperator, string path, bool recursive, object initialState, TextWriter log)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException("path");

                if (!Path.IsPathRooted(path))  // Relative path
                {
                    try
                    {
                        path = Path.GetFullPath(path);
                    }
                    catch
                    {
                        string pathDir = Path.GetDirectoryName(path),
                               pathFile = Path.GetFileName(path);
                        if (pathDir.Length == 0)  // No directory in path
                            pathDir = Environment.CurrentDirectory;
                        path = Path.Combine(pathDir, pathFile);
                    }
                }

                object state = fileOperator.InitFileOperation(initialState, log);
                state = ExecuteFileOperation(fileOperator, path, recursive, state, log);
                fileOperator.FinalizeFileOperation(initialState, state, log);
            }
            catch (Exception ex)
            {
                log.WriteLine(ex.ToString());
            }
            finally
            {
                log.Flush();
            }
        }

        private static object ExecuteFileOperation(IApplyFileOperation fileOperator, string path, bool recursive, object state, TextWriter log)
        {
            string pathDir = Path.GetDirectoryName(path),
                   pathFile = Path.GetFileName(path);

            if (string.IsNullOrEmpty(pathFile))
                pathFile = "*";

            string[] filePaths = Directory.GetFiles(pathDir, pathFile);

            log.WriteLine(
                "{0}: {1} files in {2}",
                path,
                filePaths.Length,
                pathDir);

            Array.Sort(filePaths);

            foreach (string filePath in filePaths)
            {
                try
                {
                    state = fileOperator.ExecuteFileOperation(filePath, state, log);
                }
                catch (FileNotFoundException)
                {
                    log.WriteLine(
                        "File {0} was listed, but is no longer available.",
                        filePath);
                }
                catch (Exception ex)
                {
                    log.WriteLine(
                        "File {0}: {1}",
                        filePath,
                        ex.ToString());
                }
            }

            if (recursive)
            {
                string[] dirPaths = Directory.GetDirectories(pathDir, pathFile);

                log.WriteLine(
                    "{0}: {1} folders in {2}",
                    path,
                    dirPaths.Length,
                    pathDir);

                Array.Sort(dirPaths);

                foreach (string dirPath in dirPaths)
                {
                    try
                    {
                        state = ExecuteFileOperation(fileOperator, Path.Combine(dirPath, "*"), recursive, state, log);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        log.WriteLine(
                            "Folder {0} was listed, but is no longer available.",
                            dirPath);
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine(
                            "Folder {0}: {1}",
                            dirPath,
                            ex.ToString());
                    }
                }
            }

            return state;
        }
    }
}
