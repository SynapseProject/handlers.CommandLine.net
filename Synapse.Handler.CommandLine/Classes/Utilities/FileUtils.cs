using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Synapse.Handlers.CommandLine
{
    public class FileUtils
    {
        public static String GetUNCPath(String localPath)
        {
            return GetUNCPath(null, localPath);
        }

        public static String GetUNCPath(String server, String localPath)
        {
            String unc = null;
            if (!String.IsNullOrWhiteSpace(localPath))
            {
                String svr = String.IsNullOrWhiteSpace(server) ? "127.0.0.1" : server;
                String share = localPath.Substring(0, 1) + "$";
                String path = localPath.Substring(2);
                unc = @"\\" + svr + @"\" + share + @"\" + path;
            }

            return unc;
        }

        public static String GetTempFile(String extension = null)
        {
            String file = Path.GetRandomFileName();
            if (extension == null)
                return file;
            else
                return file.Replace(".", "") + "." + extension;
        }

        public static String GetTempFileFull(String server, String localPath, String extension)
        {
            String file = null;
            String path = localPath;

            if (!String.IsNullOrWhiteSpace(server))
                path = GetUNCPath(server, localPath);

            if (path != null)
            {
                // file = path + @"\" + GetTempFile(extension);
                file = Path.Combine(path, GetTempFile(extension));
            }
            return file;
        }
    }
}
