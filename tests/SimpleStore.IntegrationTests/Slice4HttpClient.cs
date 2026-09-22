using System.Net.Http.Json;
using SimpleStore.Application.Corrections;
using SimpleStore.Application.Returns;

namespace SimpleStore.IntegrationTests;

internal static class Slice4HttpClient
{
    public static JsonContent ReturnContent(
        Guid operationId,
        Guid saleId,
        IReadOnlyCollection<(Guid SaleLineId, decimal Quantity, bool Restock)> lines,
        string? refundMethod = null,
        decimal? expectedAggregateCustomerDebt = null,
        decimal? expectedRequiredActualRefund = null) =>
        JsonContent.Create(new
        {
            operationId,
            originalSaleId = saleId,
            lines = lines.Select(item => new
            {
                originalSaleLineId = item.SaleLineId,
                quantity = item.Quantity,
                restock = item.Restock
            }),
            refundMethod,
            expectedAggregateCustomerDebt,
            expectedRequiredActualRefund
        });

    public static async Task<ReturnResult> CreateReturnAsync(
        this HttpClient client,
        Guid operationId,
        Guid saleId,
        IReadOnlyCollection<(Guid SaleLineId, decimal Quantity, bool Restock)> lines,
        string? refundMethod = null,
        decimal? expectedAggregateCustomerDebt = null,
        decimal? expectedRequiredActualRefund = null)
    {
        if (!expectedAggregateCustomerDebt.HasValue && !expectedRequiredActualRefund.HasValue)
        {
            using var previewResponse = await client.PostWithAntiforgeryAsync(
                "/api/returns/preview",
                JsonContent.Create(new
                {
                    originalSaleId = saleId,
                    lines = lines.Select(item => new
                    {
                        originalSaleLineId = item.SaleLineId,
                        quantity = item.Quantity,
                        restock = item.Restock
                    })
                }));
            await EnsureSuccessAsync(previewResponse);
            var preview = (await previewResponse.Content.ReadFromJsonAsync<ReturnPreviewResult>())!;
            expectedAggregateCustomerDebt = preview.CurrentAggregateCustomerDebt;
            expectedRequiredActualRefund = preview.RequiredActualRefund;
        }

        using var response = await client.PostWithAntiforgeryAsync(
            "/api/returns",
            ReturnContent(
                operationId,
                saleId,
                lines,
                refundMethod,
                expectedAggregateCustomerDebt,
                expectedRequiredActualRefund));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ReturnResult>())!;
    }

    public static async Task<SaleVoidResult> VoidSaleAsync(
        this HttpClient client,
        Guid saleId,
        Guid operationId,
        string reason = "Incorrect transaction")
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/sales/{saleId}/void",
            JsonContent.Create(new { operationId, reason }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<SaleVoidResult>())!;
    }

    public static async Task<PurchaseVoidResult> VoidPurchaseAsync(
        this HttpClient client,
        Guid purchaseId,
        Guid operationId,
        string reason = "Incorrect receipt")
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchaseId}/void",
            JsonContent.Create(new { operationId, reason }));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PurchaseVoidResult>())!;
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
