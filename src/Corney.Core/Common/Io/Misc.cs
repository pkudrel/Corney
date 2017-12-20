using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace Common.Io
{
    public static class Misc
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static readonly UTF8Encoding _noBom = new UTF8Encoding(false);

        public static void RemoveFilesFromFolder(string path, string pattern)
        {
            var dir = new DirectoryInfo(path);

            foreach (var fi in dir.GetFiles(pattern))
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }
        }


        public static void RenameFile(string src, string dst)
        {
            File.Move(src, dst);
        }


        public static string ReadFileWithRetry(string path, int numberOfRetries = 6, int delayOnRetry = 500)
        {
            var result = string.Empty;
            for (var i = 1; i <= numberOfRetries; ++i)
                try
                {
                    result = File.ReadAllText(path);
                    break; // When done we can break loop
                }
                catch (IOException)
                {
                    _log.Debug($"Unable to read file: {path}; Try number: {i} ");
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    if (i == numberOfRetries) // Last one, (re)throw exception and exit
                        throw;

                    Thread.Sleep(delayOnRetry);
                }
            return result;
        }

        public static void CreateDirIfNotExist(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void ClearFolder(string path)
        {
            var dir = new DirectoryInfo(path);

            foreach (var fi in dir.GetFiles())
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }

            foreach (var di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        public static void RemoveFolder(string path)
        {
            if (Directory.Exists(path))
            {
                var dir = new DirectoryInfo(path);

                foreach (var fi in dir.GetFiles())
                {
                    fi.IsReadOnly = false;
                    fi.Delete();
                }

                foreach (var di in dir.GetDirectories())
                {
                    ClearFolder(di.FullName);
                    di.Delete();
                }
                dir.Delete();
            }
        }

        public static void RemoveFile(string path)
        {
            var fileInfo = new FileInfo(path);


            fileInfo.Delete();
        }

        public static string[] CleanEmptyLines(string[] lines)
        {
            return lines.Where(x => x.Trim().Length > 0).ToArray();
        }

        public static string[] CleanComments(string[] lines)
        {
            return lines.Where(x => !x.Trim().StartsWith("#")).ToArray();
        }

        public static void WriteJson(string path, object obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(path, json, _noBom);
        }

        public static T ReadJson<T>(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T ReadJsonSafe<T>(string path)
        {
            if (!File.Exists(path))
            {
                _log.Error($"File not found: {path}");
                return default(T);
            }
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}