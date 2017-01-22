using Synchronizer.ApplicationLogic;
using System.IO;

namespace Synchronizer.PresentationLogic
{
    public static class ValidationMethods
    {
        public static bool IsValidFileSize(long value)
        {
            return value >= 0;
        }

        public static bool IsValidBlockSize(int value)
        {
            return value > 0 && value != 0;
        }

        public static bool IsValidLoggingFileSize(long value)
        {
            return value > 0;
        }

        public static bool FileInfoTryParse(string filePath, out FileInfo value)
        {
            filePath = PathHelper.ChangePathToDefaultPath(filePath, true);

            if (File.Exists(filePath))
            {
                value = new FileInfo(filePath);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static bool DirectoryInfoTryParse(string filePath, out DirectoryInfo value)
        {
            filePath = PathHelper.ChangePathToDefaultPath(filePath, false);

            if (Directory.Exists(filePath))
            {
                value = new DirectoryInfo(filePath);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static bool YesNoTryParse(string input, out bool value)
        {
            string inputLower = input.ToLower().Trim();

            if (inputLower == "y")
            {
                value = true;
                return true;
            }
            else if (inputLower == "n")
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }
    }
}
