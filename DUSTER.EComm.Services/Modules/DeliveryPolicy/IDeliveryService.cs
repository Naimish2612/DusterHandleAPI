using DUSTER.EComm.Services.Modules.DeliveryPolicy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.DeliveryPolicy
{
    public interface IDeliveryService
    {
        #region Delivery Policy

        Task<IActionResult> CreatePolicyAsync(DeliveryPolicys model);
        Task<IActionResult> EditPolicyAsync(DeliveryPolicys model);
        Task<IActionResult> GetAllPolicyAsync();
        Task<IActionResult> DeliveryPolicyActiveInactive(long delivery_policy_id);

        #endregion

        #region Policy Slab

        Task<IActionResult> CreateSlabAsync(List<DeliveryPolicySlab> model);
        Task<IActionResult> EditSlabAsync(DeliveryPolicySlab model);
        Task<IActionResult> GetSlabsByPolicyIdAsync(long policyId);

        #endregion

        Task<IActionResult> AddOrderCalculationAsync(OrderDeliveryCalculation model);
        Task<IActionResult> GetOrderCalculationsAsync(long orderId);
        Task<IActionResult> CalculateDeliveryFeeAsync(DeliveryFeeRequest request);
        Task<IActionResult> SimulateDeliveryAmountAsync(decimal orderAmount);
    }
}
