using CsvHelper.Configuration;

namespace GDAPMigrationTool.Core.Model
{

    public class DelegatedAdminRelationshipMap : ClassMap<DelegatedAdminRelationship>
    {
        public DelegatedAdminRelationshipMap()
        {
            Map(d => d.Id).Index(0);
            Map(d => d.CustomerDelegatedAdminRelationshipId).Index(1);
            Map(d => d.DisplayName).Index(2).Name("RelationshipName");
            Map(d => d.Partner.TenantId).Index(3).Name("PartnerTenantId");

            //customer
            Map(d => d.Customer.DisplayName).Index(4).Name("CustomerName");
            Map(d => d.Customer.TenantId).Index(5).Name("CustomerTenantId");

            Map(d => d.Duration).Index(6);
            Map(d => d.Status).Index(7);
            Map(d => d.CreatedDateTime).Index(8);
            Map(d => d.ActivatedDateTime).Index(9);
            Map(d => d.LastModifiedDateTime).Index(10);
            Map(d => d.EndDateTime).Index(11);

        }

    }
}

