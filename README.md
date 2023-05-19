# Get Microsoft GDAP under control with Cloud Factory
Latest version supports Indirect Resellers, Direct Partner and Indirect Provider. See build-in roles below.

# Microsoft Partner Center Announcements 2023 May no. 11
New timeline!
https://learn.microsoft.com/en-us/partner-center/announcements/2023-may#new-timelines-important-actions-to-secure-the-partner-ecosystem

# Microsoft Partner Center Announcements 2022 November no. 24 
https://learn.microsoft.com/en-us/partner-center/announcements/2022-november#24

## Overview
Granular Delegated Admin Permissions (GDAP) allows partners to access customer workloads in a more granular and time-bound manner, addressing customer security concerns and also helps customers who have regulatory requirements for least-privileged access to partners. Partners with Admin agent role can request GDAP relationship, have a maximum duration of two years and cannot be made permanent for security reasons.

In the current business landscape, many Managed Service Providers (MSPs) often retain administrative access to their customers' Global Admin (GA) accounts. It's paramount to adapt to evolving industry standards and showcase the exceptional value of your services as a CSP Partner. Security is key in gaining customer trust and confidence.

## How to use
1. [Download](https://github.com/cloudfactorydk/GDAPMigrationTool/releases) the GDAP Migration tool and run it.
2. Sign in with your Microsoft 365 GA Partner Account when prompted (Browser opens).
3. Please don't close the browser and don't close the tool.

## Hypotheses
1. The implementation of granular delegated admin permissions (GDAP) in partner-customer relationships will result in improved customer satisfaction, increased security, and enhanced partner performance.
2. The next generation of MSPs, who prioritize security-first approaches and possess cloud-native expertise, will gain a competitive edge over traditional Microsoft partners who rely on shared Global Admin (GA) access, leading to greater customer acquisition and retention.
3. By offering valuable consulting and services focused on security, MSPs can effectively demonstrate their expertise and differentiate themselves in the market, leading to increased customer engagement and higher revenue generation.

## Contributing
Cloud Factory welcomes contributions and suggestions to enhance its platform. Please ensure you agree to the Contributor License Agreement (CLA), which grants us the necessary rights to use your contributions. Detailed information about the CLA is available [here](https://cla.opensource.microsoft.com).

When submitting a pull request, a CLA bot will automatically assess if a CLA is required and will provide appropriate status checks and comments. Please follow the instructions provided by the bot. Note that you only need to complete this process once across all repositories that utilize our CLA.

Cloud Factory operates under the Microsoft Open Source Code of Conduct, ensuring a respectful and inclusive environment for all contributors. If you have any questions or comments regarding the Code of Conduct, please reach out to opencode@microsoft.com.

## What You Get
1. Create an admin relationship between all customers and you as a Microsoft Partner to start the delegated access.
2. On the new relationship, we assign the roles listed below and set the duration to 730 days.

## Roles Indirect Resellers, Direct Partner, Indirect Provider

The list of roles is based on Microsoft Indirect Reseller GDAP recommendations. The Cloud Factory team added Directory Writers and Global Reader for maximum benefit.

* Authentication Administrator
* Desktop Analytics Administrator
* Directory Readers
* Directory Writers
* Exchange Administrator
* Global Reader
* Groups Administrator
* Guest Inviter
* Helpdesk Administrator
* Intune Administrator
* License Administrator
* Message Center Reader
* Printer Administrator
* Security Reader
* Service Support Administrator
* SharePoint Administrator
* Teams Communications Support Specialist
* User Administrator

The list of roles is based on Microsoft Direct Partner GDAP recommendations. 

* Cloud Application Administrator
* Directory Readers
* Global Reader
* Helpdesk Administrator
* License Administrator
* Privileged Authentication Administrator
* Service Support Administrator
* User Administrator

The list of roles is based on Microsoft Indirect Provider GDAP recommendations. The Cloud Factory team added Cloud Application Administrator and Privileged Authentication Administrator for maximum benefit. 

* Cloud Application Administrator
* Directory Readers
* Directory Writers
* Global Reader
* Helpdesk Administrator
* License Administrator
* Privileged Authentication Administrator
* Service Support Administrator
* User Administrator


##  Security Center on the Partner Center

In the Security Center on the Partner Center, you can monitor cross-tenant sign-in activities for administrative privileges, and remove DAP if inactive. The Security Center also provides other functions, which are primary to ensuring a more secure partner-customer ecosystem, including:
https://partner.microsoft.com/en-us/dashboard/commerce2/securitycenter/administrativeRelationships

* Partner Admin agents can access Security Center for administrative relationships.
* Partners can sign in to the Security Center for administrative relationships to view sign-in activities.
* Partners can see sign-in activities across all their customers.
