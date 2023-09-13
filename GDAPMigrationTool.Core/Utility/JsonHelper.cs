using GDAPMigrationTool.Core.Providers;
using Newtonsoft.Json;

namespace GDAPMigrationTool.Core.Utility
{
    /// <summary>
    /// JSON helper class
    /// </summary>
    internal class JsonHelper : IExportImportProvider
    {

        /// <summary>
        /// Read data from json file async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<List<T?>> ReadAsync<T>(string fileName)
        {
            using (StreamReader r = new StreamReader(fileName))
            {
                string json = r.ReadToEnd();
                return Task.FromResult(JsonConvert.DeserializeObject<List<T>>(json));
            }
        }

        /// <summary>
        /// Write data from json file async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task WriteAsync<T>(IEnumerable<T>? data, string fileName)
        {
            string jsonFile = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(fileName, jsonFile);
        }
    }
}
