//-----------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains serialization operations.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// This class contains serialization operations.
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Serializes an object. From <see href="http://stackoverflow.com/questions/6115721/how-to-save-restore-serializable-object-to-from-file"/>
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="serializableObject">The instance of the object.</param>
        /// <param name="fileName">The file name to write the serialized object.</param>
        public static void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null)
            {
                return;
            }

            string serialized = JsonConvert.SerializeObject(serializableObject);
            File.WriteAllText(fileName, serialized);
        }

        /// <summary>
        /// Deserializes a file into an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The object.</returns>
        public static T DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return default(T);
            }

            string serialized = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        /// Copies a object using serialization.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="objectToCopy">The object to copy.</param>
        /// <returns>A new object without references.</returns>
        public static T CopyObject<T>(T objectToCopy)
        {
            if (objectToCopy == null)
            {
                return default(T);
            }

            string serialized = JsonConvert.SerializeObject(objectToCopy);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
