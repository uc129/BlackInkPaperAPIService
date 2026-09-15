namespace Infrastructure.Configuration;

/// <summary>
/// Where the customer-facing storefront lives. The API needs this only to build links it emails
/// out — it has no other reason to know the site's address, and cannot derive one: the email is
/// sent from a request to the API host, which is a different origin.
/// </summary>
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Origin of the storefront, with or without a trailing slash.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
