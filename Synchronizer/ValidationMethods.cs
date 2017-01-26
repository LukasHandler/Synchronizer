//-----------------------------------------------------------------------
// <copyright file="ValidationMethods.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains validation methods.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.PresentationLogic
{
    using System.IO;
    using ApplicationLogic;

    /// <summary>
    /// This class contains validation methods to validate correct input.
    /// </summary>
    public static class ValidationMethods
    {
        /// <summary>
        /// Determines whether the value is a valid file size.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if it is valid.</returns>
        public static bool IsValidFileSize(long value)
        {
            return value >= 0;
        }

        /// <summary>
        /// Determines whether the value is a valid block size.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if it is valid.</returns>
        public static bool IsValidBlockSize(int value)
        {
            return value > 0 && value != 0;
        }

        /// <summary>
        /// Determines whether the value is a valid logging file size.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if it is valid.</returns>
        public static bool IsValidLoggingFileSize(long value)
        {
            return value > 0;
        }

        /// <summary>
        /// The try parse function for a file info.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if it can parse.</returns>
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

        /// <summary>
        /// The try parse function for a directory info.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if it can parse.</returns>
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

        /// <summary>
        /// The try parse function for a yes - no input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="value">True if yes, false if no.</param>
        /// <returns>True if it can parse.</returns>
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
