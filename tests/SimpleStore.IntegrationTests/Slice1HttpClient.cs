using System.Net.Http.Json;
using System.Text.Json;
using SimpleStore.Application.Products;
using SimpleStore.Application.Stores;

namespace SimpleStore.IntegrationTests;

internal static class Slice1HttpClient
{
    public static async Task LoginAsync(
        this HttpClient client,
        string email,
        string password)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/auth/login",
            JsonContent.Create(new { email, password }));
        await EnsureSuccessAsync(response);
    }

    public static async Task<StoreResult> InitializeStoreAsync(
        this HttpClient client,
        string name = "Cửa hàng kiểm thử")
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/store/initialize",
            JsonContent.Create(new { name }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StoreResult>())!;
    }

    public static async Task<ProductResult> CreateProductAsync(
        this HttpClient client,
        string? sku = null,
        string? barcode = null,
        string name = "Sản phẩm kiểm thử",
        decimal openingQuantity = 0,
        decimal? openingCost = null)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/products",
            JsonContent.Create(new
            {
                sku,
                barcode,
                name,
                unit = "cái",
                salePrice = 12_000,
                referencePurchaseCost = openingCost,
                openingQuantity,
                openingCost
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResult>())!;
    }

    public static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        this HttpClient client,
        string path,
        HttpContent content)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PutWithAntiforgeryAsync(
        this HttpClient client,
        string path,
        HttpContent content)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = content
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/security/antiforgery");
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Request failed with {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }
    }
}
