using System.Net.Http.Json;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Suppliers;

namespace SimpleStore.IntegrationTests;

internal static class Slice2HttpClient
{
    public static async Task<SupplierResult> CreateSupplierAsync(
        this HttpClient client,
        string name = "Nhà cung cấp kiểm thử",
        string? phone = "0909000000")
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/suppliers",
            JsonContent.Create(new { name, phone, note = "Ghi chú" }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<SupplierResult>())!;
    }

    public static async Task<PurchaseResult> CreatePurchaseAsync(
        this HttpClient client,
        Guid supplierId,
        params (Guid ProductId, decimal Quantity, decimal UnitPrice)[] lines)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/purchases",
            JsonContent.Create(new
            {
                supplierId,
                lines = lines.Select(line => new
                {
                    productId = line.ProductId,
                    quantity = line.Quantity,
                    unitPrice = line.UnitPrice
                })
            }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PurchaseResult>())!;
    }

    public static async Task<PurchaseResult> CompletePurchaseAsync(
        this HttpClient client,
        Guid purchaseId,
        Guid operationId,
        params (decimal Amount, string Method)[] payments)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchaseId}/complete",
            JsonContent.Create(new
            {
                operationId,
                payments = payments.Select(payment => new
                {
                    amount = payment.Amount,
                    method = payment.Method
                })
            }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PurchaseResult>())!;
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
