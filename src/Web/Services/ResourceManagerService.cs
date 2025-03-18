using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Identity.Web;
using Microsoft.Rest;

namespace TagUpdater.Web.Services;

public class ResourceManagerService
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public ResourceManagerService(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task<bool> UpdateResourceTagsAsync(string resourceId, Dictionary<string, string> tags)
    {
        try
        {
            // Get token for Azure Resource Manager
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                new[] { "https://management.azure.com/user_impersonation" });

            // Parse resource ID to extract subscription, resource group, and resource information
            var parts = resourceId.Split('/');
            if (parts.Length < 9)
            {
                throw new ArgumentException("Invalid Azure resource ID format");
            }

            var subscriptionId = parts[2];
            var resourceGroupName = parts[4];
            var providerNamespace = parts[6];
            var resourceType = parts[7];
            var resourceName = parts[8];

            // Create client with user's access token
            var credentials = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(credentials)
            {
                SubscriptionId = subscriptionId
            };

            // Get the existing resource
            var resource = await resourceClient.Resources.GetAsync(
                resourceGroupName,
                providerNamespace,
                "",
                resourceType,
                resourceName,
                "2021-04-01");

            // Update the tags (merge with existing tags if any)
            if (resource.Tags == null)
            {
                resource.Tags = new Dictionary<string, string>();
            }

            foreach (var tag in tags)
            {
                resource.Tags[tag.Key] = tag.Value;
            }

            // Update the resource
            await resourceClient.Resources.UpdateAsync(
                resourceGroupName,
                providerNamespace,
                "",
                resourceType,
                resourceName,
                "2021-04-01",
                resource);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}