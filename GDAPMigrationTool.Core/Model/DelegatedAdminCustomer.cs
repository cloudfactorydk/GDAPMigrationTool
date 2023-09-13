// <copyright file="DelegatedAdminCustomer.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// Represents a delegated admin customer of a partner, and details about the partner's access to the customer's tenant.
    /// </summary>
    public class DelegatedAdminCustomer
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the customer tenant ID.
        /// </summary>
        public string CustomerTenantId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the customer's organization.
        /// </summary>
        public string OrganizationDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the number of partner agents with access to the customer.
        /// </summary>
        public int PartnerAgentCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times the partner has signed into the customer tenant.
        /// </summary>
        public int PartnerSignInCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times a partner global admin has signed into the customer tenant.
        /// </summary>
        public int GlobalAdminSignInCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the partner has DAP enabled for the customer.
        /// </summary>
        public bool DapEnabled { get; set; }

        /// <summary>
        /// Gets or sets the start date time of the DAP access.
        /// </summary>
        public string StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date time of the DAP access, if applicable.
        /// </summary>
        public string EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the last date time a partner agent signed in into the customer tenant.
        /// </summary>
        public string LastSignInDateTime { get; set; }

    }
}