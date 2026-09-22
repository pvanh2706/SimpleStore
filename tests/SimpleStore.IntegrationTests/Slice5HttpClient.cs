using System.Net.Http.Json;
using SimpleStore.Application.Debts;

namespace SimpleStore.IntegrationTests;

internal static class Slice5HttpClient
{
    public static async Task<DebtBalanceResult> GetCustomerDebtAsync(
        this HttpClient client,
        Guid customerId) =>
        (await client.GetFromJsonAsync<DebtBalanceResult>($"/api/customers/{customerId}/debt"))!;

    public static async Task<DebtBalanceResult> GetSupplierDebtAsync(
        this HttpClient client,
        Guid supplierId) =>
        (await client.GetFromJsonAsync<DebtBalanceResult>($"/api/suppliers/{supplierId}/debt"))!;

    public static JsonContent DebtPaymentContent(
        Guid operationId,
        decimal amount,
        decimal expectedOutstandingAmount,
        string method = "Cash",
        string? note = null) =>
        JsonContent.Create(new { operationId, amount, method, expectedOutstandingAmount, note });

    public static async Task<DebtPaymentResult> RecordCustomerDebtPaymentAsync(
        this HttpClient client,
        Guid customerId,
        Guid operationId,
        decimal amount,
        decimal expectedOutstandingAmount,
        string method = "Cash",
        string? note = null)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/customers/{customerId}/debt-payments",
            DebtPaymentContent(operationId, amount, expectedOutstandingAmount, method, note));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<DebtPaymentResult>())!;
    }

    public static async Task<DebtPaymentResult> RecordSupplierDebtPaymentAsync(
        this HttpClient client,
        Guid supplierId,
        Guid operationId,
        decimal amount,
        decimal expectedOutstandingAmount,
        string method = "Cash",
        string? note = null)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/suppliers/{supplierId}/debt-payments",
            DebtPaymentContent(operationId, amount, expectedOutstandingAmount, method, note));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<DebtPaymentResult>())!;
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
