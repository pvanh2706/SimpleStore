using SimpleStore.Application.Errors;
using SimpleStore.Domain.Returns;

namespace SimpleStore.Application.Returns;

internal static class ReturnFinancialHistory
{
    public static decimal GetActualRefundTotal(IEnumerable<CustomerReturn> returns)
    {
        decimal total = 0;
        foreach (var customerReturn in returns)
        {
            var actualRefund = customerReturn.RefundPayments.Sum(payment => payment.Amount);
            if (customerReturn.RefundAmount != actualRefund)
            {
                throw new ApplicationConflictException(
                    "return-financial-state-invalid",
                    "Return refund history is inconsistent with actual refund payments.");
            }

            total += actualRefund;
        }

        return total;
    }
}
