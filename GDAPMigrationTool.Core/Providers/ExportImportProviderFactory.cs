using PartnerLed.Model;
using PartnerLed.Utility;

namespace PartnerLed.Providers
{
    public class ExportImportProviderFactory : IExportImportProviderFactory
    {
        public IExportImportProvider Create(ExportImport type)
        {
            switch (type)
            {
                case ExportImport.Csv:
                    return new CSVhelper();
                case ExportImport.Json:
                    return new JsonHelper();
                default:
                    throw new InvalidOperationException($"Unknown ExportImport  type: {type}");

            }
        }
    }
}
