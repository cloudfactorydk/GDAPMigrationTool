using PartnerLed.Model;

namespace PartnerLed.Providers
{
    public interface ICustomerProvider
    {
        Task<bool> DAPTermination(ExportImport type);

        Task<bool> CreateDAPTerminateFile(ExportImport type);
    }
}