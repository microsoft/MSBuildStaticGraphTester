using System.IO;
using System.Threading;

internal static class FileUtils
{
    public static void DeleteDirectoryWithRetry(string cacheRoot)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, true);
            }

            if (Directory.Exists(cacheRoot))
            {
                Thread.Sleep(500);
            }
            else
            {
                break;
            }
        }
    }
}