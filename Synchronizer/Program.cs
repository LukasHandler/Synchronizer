//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains the main window.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.PresentationLogic
{
    /// <summary>
    /// This class contains the main window.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Starts the presentation manager.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            PresentationManager presentationManager = new PresentationManager(args);
        }
    }
}
