namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// This represents the partner's view of a Delegated Admin relationship between a partner and a customer that is errored.
    /// </summary>
    public class DelegatedAdminRelationshipErrored : DelegatedAdminRelationship
    {
        /// <summary>
        /// Gets or sets the Error details during creation.
        /// </summary>
        public string ErrorDetail { get; set; }

       
    }
}

