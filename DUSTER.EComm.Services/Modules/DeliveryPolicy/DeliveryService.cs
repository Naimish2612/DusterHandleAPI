using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.DBConnection;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.DeliveryPolicy.Models;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.DeliveryPolicy
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IEIPLRepository<DeliveryPolicys> _policyRepo;
        private readonly IEIPLRepository<DeliveryPolicySlab> _slabRepo;
        private readonly IEIPLRepository<OrderDeliveryCalculation> _calculationRepo;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ICurrentUserService _currentUserService;
        private readonly string _originState = "Gujarat";

        public DeliveryService(IEIPLRepository<DeliveryPolicys> policyRepo,
            IEIPLRepository<DeliveryPolicySlab> slabRepo,
            IEIPLRepository<OrderDeliveryCalculation> calculationRepo,
            ICurrentUserService currentUserService)
        {
            _policyRepo = policyRepo;
            _slabRepo = slabRepo;
            _calculationRepo = calculationRepo;
            _currentUserService = currentUserService;
        }

        #region Delivery Policies

        public async Task<IActionResult> CreatePolicyAsync(DeliveryPolicys model)
        {
            try
            {
                var validator = await _policyRepo.ModelValidating(new ValidationModel() { ValidateModel = new DeliveryPolicyValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                if (model.is_active)
                {
                    if (await IsTimelineCollapsingAsync(model))
                        return ResponseEntity<object>.Error("The provided timeline is collapsing with an existing delivery policy");
                }

                var response = await _policyRepo.InsertAsync(model);

                if (Convert.ToInt64(response) >= 0)
                    return ResponseEntity<object>.Success(response,"Delivery Policy created successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to create Delivery Policy.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> EditPolicyAsync(DeliveryPolicys model)
        {
            try
            {
                var validator = await _policyRepo.ModelValidating(new ValidationModel() { ValidateModel = new DeliveryPolicyValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                if (model.is_active)
                {
                    if (await IsTimelineCollapsingAsync(model))
                        return ResponseEntity<object>.Error("Timeline is collapsing.");
                }

                var response = await _policyRepo.UpdateAsync(model);

                if (Convert.ToInt32(response) > 0)
                    return ResponseEntity<object>.Success("Delivery Policy updated successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to update Delivery Policy.");

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetAllPolicyAsync()
        {
            try
            {
                var data = await _policyRepo.QueryAsync<DeliveryPolicys>("SELECT * FROM tbl_delivery_policies");

                if (data != null)
                    return ResponseEntity<object>.Success(data, "Active delivery policy retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No active delivery policy found for the current date/time.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> DeliveryPolicyActiveInactive(long delivery_policy_id)
        {
            try
            {
                if (delivery_policy_id <= 0)
                    return ResponseEntity<object>.Error("Invalid policy ID.");

                var existingPolicy = await _policyRepo.GetByIdAsync(delivery_policy_id);

                if (existingPolicy == null)
                    return ResponseEntity<object>.Error("Delivery Policy not found.");

                existingPolicy.is_active = !existingPolicy.is_active;

                if (existingPolicy.is_active)
                {
                    if (await IsTimelineCollapsingAsync(existingPolicy))
                        return ResponseEntity<object>.Error("Timeline is collapsing.");
                }

                var response = await _policyRepo.UpdateAsync(existingPolicy);

                return ResponseEntity<object>.Success(existingPolicy.is_active ? "Delivery Policy activated successfully." : "Delivery Policy deactivated successfully.");

            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Policy Slabs

        public async Task<IActionResult> CreateSlabAsync(List<DeliveryPolicySlab> model)
        {
            try
            {
                if (model == null || model.Count == 0)
                    return ResponseEntity<object>.Error("No slabs provided.");

                foreach (var slab in model)
                {
                    var validator = await _slabRepo.ModelValidating(new ValidationModel() { ValidateModel = new DeliveryPolicySlabValidator(), Model = slab });
                    if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                        return errorResponse;
                }

                var response = await _slabRepo.InsertMultipleAsync(model);

                if (Convert.ToInt64(response) > 0)
                    return ResponseEntity<object>.Success("Policy Slab(s) created successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to create Policy Slab(s).");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> EditSlabAsync(DeliveryPolicySlab model)
        {
            try
            {
                var validator = await _slabRepo.ModelValidating(new ValidationModel() { ValidateModel = new DeliveryPolicySlabValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var response = await _slabRepo.UpdateAsync(model);

                if (Convert.ToInt32(response) <= 0)
                    return ResponseEntity<object>.Error("Failed to update Policy Slab.");
                else
                    return ResponseEntity<object>.Success("Policy Slab updated successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetSlabsByPolicyIdAsync(long policyId)
        {
            try
            {
                var data = await _slabRepo.QueryAsync<DeliveryPolicySlab>("SELECT * FROM tbl_delivery_policy_slabs WHERE delivery_policy_id = @PolicyId", new DynamicParameters(new { PolicyId = policyId }));
                if (data != null)
                    return ResponseEntity<object>.Success(data, "Policy slabs retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No slabs found for the given policy ID.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Order Tracking Calculations

        public async Task<IActionResult> AddOrderCalculationAsync(OrderDeliveryCalculation model)
        {
            try
            {
                var validator = await _calculationRepo.ModelValidating(new ValidationModel() { ValidateModel = new OrderDeliveryCalculationValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                model.calculation_snapshot = JsonSerializer.Serialize(model.calculation_snapshot_obj);

                var response = await _calculationRepo.InsertAsync(model);

                if (Convert.ToInt64(response) < 0)
                    return ResponseEntity<object>.Error("Failed to add delivery txn record.");
                else
                    return ResponseEntity<object>.Success(null);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetOrderCalculationsAsync(long orderId)
        {
            try
            {
                var data = await _calculationRepo.QueryAsync<OrderDeliveryCalculation>("SELECT * FROM tbl_order_delivery_calculations WHERE order_id = @OrderId ORDER BY calculated_on DESC", new DynamicParameters(new { OrderId = orderId }));
                if (data != null)
                    return ResponseEntity<object>.Success(data, "Order delivery calculations retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No delivery calculations found for the given order ID.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Delivery Engine Logic

        public async Task<IActionResult> CalculateDeliveryFeeAsync(DeliveryFeeRequest request)
        {
            try
            {
                var activePolicy = await GetActivePolicyAsync();

                if (activePolicy == null)
                    return ResponseEntity<object>.Error(null, "No active delivery policy found for the current date/time.");

                decimal baseFee = 0;
                object snapshotData = null;

                if (activePolicy.calculation_type == "FlatBracket")
                {
                    var matchingSlab = activePolicy.slabs.FirstOrDefault(s => request.post_discount_cart_total >= s.range_start && request.post_discount_cart_total <= s.range_end);
                    decimal appliedPercentage = matchingSlab?.percentage ?? 0;
                    baseFee = request.post_discount_cart_total * (appliedPercentage / 100m);

                    snapshotData = new { Method = "FlatBracket", CartValueEvaluated = request.post_discount_cart_total, AppliedPercentage = appliedPercentage, CalculatedRawBase = baseFee };
                }
                else if (activePolicy.calculation_type == "ProgressiveSlab")
                {
                    baseFee = CalculateProgressiveFee(request.post_discount_cart_total, activePolicy.slabs, out var details);
                    snapshotData = new { Method = "ProgressiveSlab", SlabsApplied = details, CalculatedRawBase = baseFee };
                }

                decimal feeAfterMin = Math.Max(baseFee, activePolicy.min_charge);
                decimal cappedBaseFee = Math.Min(feeAfterMin, activePolicy.max_charge);

                string taxType = string.Equals(request.destination_state, _originState, StringComparison.OrdinalIgnoreCase) ? "CGST/SGST" : "IGST";
                decimal taxAmount = cappedBaseFee * (activePolicy.tax_percentage / 100m);
                decimal finalTotal = Math.Max(0, (cappedBaseFee + taxAmount) - request.delivery_discount_amount);

                var result = new
                {
                    policy_id_applied = activePolicy.delivery_policy_id,
                    base_fee = Math.Round(cappedBaseFee, 2),
                    tax_amount = Math.Round(taxAmount, 2),
                    tax_type = taxType,
                    discount_amount = request.delivery_discount_amount,
                    final_total = Math.Round(finalTotal, 2),
                    calculation_snapshot = new { EngineDetails = snapshotData, MinMaxApplied = new { Min = activePolicy.min_charge, Max = activePolicy.max_charge } }
                };

                return ResponseEntity<object>.Success(result, "Delivery fee calculated successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> SimulateDeliveryAmountAsync(decimal orderAmount)
        {
            try
            {
                var request = new DeliveryFeeRequest
                {
                    post_discount_cart_total = orderAmount,
                    destination_state = _originState,
                    delivery_discount_amount = 0
                };

                return await CalculateDeliveryFeeAsync(request);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Private Helpers

        private async Task<bool> IsTimelineCollapsingAsync(DeliveryPolicys model)
        {
            var activePolicies = await _policyRepo.QueryAsync<DeliveryPolicys>("SELECT * FROM tbl_delivery_policies WHERE is_active = true");

            foreach (var existing in activePolicies)
            {
                if (existing.delivery_policy_id == model.delivery_policy_id)
                    continue;

                bool startOverlaps = !existing.effective_to.HasValue || model.effective_from <= existing.effective_to.Value;
                bool endOverlaps = !model.effective_to.HasValue || existing.effective_from <= model.effective_to.Value;

                if (startOverlaps && endOverlaps)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<DeliveryPolicys> GetActivePolicyAsync()
        {
            const string sql = @"SELECT * FROM tbl_delivery_policies p
                WHERE p.is_active = true AND p.effective_from <= @Now AND (p.effective_to >= @Now OR p.effective_to IS NULL)";

            var param = new DynamicParameters(new { Now = DateTime.Now });

            var data = await _policyRepo.GetSingleOrDefaultAsync(sql, param);

            if (data != null)
            {
                data.slabs = (await _slabRepo.QueryAsync<DeliveryPolicySlab>("SELECT * FROM tbl_delivery_policy_slabs WHERE delivery_policy_id = @PolicyId",
                new DynamicParameters(new { PolicyId = data.delivery_policy_id }))).ToList();
            }

            return data;
        }

        private decimal CalculateProgressiveFee(decimal cartTotal, List<DeliveryPolicySlab> slabs, out List<object> details)
        {
            decimal totalFee = 0;
            decimal remainingAmount = cartTotal;
            details = new List<object>();

            foreach (var slab in slabs.OrderBy(s => s.range_start))
            {
                if (remainingAmount <= 0) break;
                decimal amountInSlab = Math.Min(remainingAmount, slab.range_end - slab.range_start);
                decimal slabFee = amountInSlab * (slab.percentage / 100m);
                totalFee += slabFee;
                details.Add(new { Range = $"{slab.range_start}-{slab.range_end}", Amount = amountInSlab, Fee = slabFee });
                remainingAmount -= amountInSlab;
            }
            return totalFee;
        }

        #endregion
    }
}
