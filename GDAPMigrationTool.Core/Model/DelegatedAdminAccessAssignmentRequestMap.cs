using CsvHelper.Configuration;

namespace PartnerLed.Model
{
    public class DelegatedAdminAccessAssignmentRequestMap : ClassMap<DelegatedAdminAccessAssignmentRequest>
    {
        public DelegatedAdminAccessAssignmentRequestMap()
        {
            Map(d => d.GdapRelationshipId).Index(0);
            Map(d => d.AccessAssignmentId).Index(1);

            //customer
            Map(d => d.Customer.DisplayName).Index(2).Name("CustomerName");
            Map(d => d.Customer.TenantId).Index(3).Name("CustomerTenantId");

            Map(d => d.SecurityGroupId).Index(4);
            Map(d => d.SecurityGroupName).Index(5);
            Map(d => d.CommaSeperatedRoles).Index(6);
            Map(d => d.Status).Index(7);
            Map(d => d.Etag).Index(8);
            Map(d => d.CreatedDateTime).Index(9);
            Map(d => d.LastModifiedDateTime).Index(10);
        }
    }
}