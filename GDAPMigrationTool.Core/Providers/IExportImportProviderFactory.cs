using PartnerLed.Model;

namespace PartnerLed.Providers
{
    public interface IExportImportProviderFactory
    {
        IExportImportProvider Create(ExportImport type);
    }
}