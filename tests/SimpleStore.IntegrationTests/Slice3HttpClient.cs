using System.Net.Http.Json;
using SimpleStore.Application.Customers;
using SimpleStore.Application.Sales;
using SimpleStore.Application.Stores;

namespace SimpleStore.IntegrationTests;

internal static class Slice3HttpClient
{
    public static async Task<CustomerResult> CreateCustomerAsync(
        this HttpClient client,
        string name = "Test customer",
        string? phone = "0909000000")
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/customers",
            JsonContent.Create(new { name, phone }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CustomerResult>())!;
    }

    public static async Task<SaleResult> CompleteSaleAsync(
        this HttpClient client,
        Guid operationId,
        Guid? customerId,
        IReadOnlyCollection<(Guid ProductId, decimal Quantity)> lines,
        params (decimal Amount, string Method)[] payments)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            SaleContent(operationId, customerId, lines, payments));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<SaleResult>())!;
    }

    public static JsonContent SaleContent(
        Guid operationId,
        Guid? customerId,
        IReadOnlyCollection<(Guid ProductId, decimal Quantity)> lines,
        params (decimal Amount, string Method)[] payments) =>
        JsonContent.Create(new
        {
            operationId,
            customerId,
            lines = lines.Select(line => new { productId = line.ProductId, quantity = line.Quantity }),
            payments = payments.Select(payment => new { amount = payment.Amount, method = payment.Method })
        });

    public static async Task<StoreOperationalSettingsResult> SetNegativeStockAsync(
        this HttpClient client,
        bool allowNegativeStock)
    {
        using var response = await client.PutWithAntiforgeryAsync(
            "/api/store/operational-settings/negative-stock",
            JsonContent.Create(new { allowNegativeStock }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<StoreOperationalSettingsResult>())!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Request failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
