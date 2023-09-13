namespace GDAPMigrationTool.Core.Model
{
    public class DelegatedAdminAccessAssignment : ODataEntity
    {
        /// <summary>
        /// Gets or sets the ID of the access assignment.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the access container.
        /// </summary>
        public DelegatedAdminAccessContainer AccessContainer { get; set; }

        /// <summary>
        /// Gets or sets the access details for this assignment.
        /// </summary>
        public DelegatedAdminAccessDetails AccessDetails { get; set; }

        /// <summary>
        /// Gets or sets the status of the assignment.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this access assignment was created in UTC (ISO 8601 format).
        /// </summary>
        public string CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this access assignment was last modified in UTC (ISO 8601 format).
        /// </summary>
        public string LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the version stamp value used for optimistic concurrency control. This is set by the system and cannot be set by the caller.
        /// </summary>
        public string VersionStamp { get; set; }

        
        public IDictionary<string, object> Properties { get; set; }
    }
}
