using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Services.Modules.Customers.Models;
using DUSTER.EComm.Services.Modules.Tax.Models;
using System.Data;
using System.Data.Common;
using System.Formats.Asn1;
using System.Reflection;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Tax
{
    public class TaxServices : ITaxServices
    {
        private readonly IEIPLRepository<TaxComponent> _taxCompRepo;
        private readonly IEIPLRepository<TaxClass> _taxClassRepo;
        private readonly IEIPLRepository<TaxRule> _taxRuleRepo;

        public TaxServices(IEIPLRepository<TaxComponent> taxCompRepo, IEIPLRepository<TaxClass> taxClassRepo, IEIPLRepository<TaxRule> taxRuleRepo)
        {
            _taxCompRepo = taxCompRepo;
            _taxClassRepo = taxClassRepo;
            _taxRuleRepo = taxRuleRepo;
        }

        public async Task<IActionResult> ManageTaxComponent(TaxComponent component)
        {
            var validator = await _taxCompRepo.ModelValidating(new ValidationModel() { ValidateModel = new TaxComponentValidator(), Model = component });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            if (component.component_id > 0)
            {
                var updateResult = await _taxCompRepo.UpdateAsync(component);
                if (Convert.ToInt32(updateResult) > 0)
                    return ResponseEntity<object>.Success("Tax component updated successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to update tax component.");
            }
            else
            {
                var result = await _taxCompRepo.InsertAsync(component);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success("Tax component created successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to create tax component.");
            }

        }

        public async Task<IActionResult> TaxComponentList()
        {
            var response = await _taxCompRepo.QueryAsync<TaxComponent>("select * from tbl_tax_components");

            if (response.Any())
                return ResponseEntity<object>.Success(response);
            else
                return ResponseEntity<object>.Error("Tax Component not available");
        }

        public async Task<IActionResult> TaxComponentDropdown()
        {
            var filters = new Dictionary<string, object>
                {
                    { "is_active", true },
                };

            var response = await _taxCompRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_tax_components", table_columns = "component_id as id, component_name as value", StaticFilters = filters });
            if (response.Any())
                return ResponseEntity<object>.Success(response);
            else
                return ResponseEntity<object>.Error("Tax Component not available");
        }

        public async Task<IActionResult> ManageClassComponent(TaxClass tClass)
        {
            var validator = await _taxClassRepo.ModelValidating(new ValidationModel() { ValidateModel = new TaxClassValidator(), Model = tClass });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            if (tClass.class_id > 0)
            {
                var updateResult = await _taxClassRepo.UpdateAsync(tClass);
                if (Convert.ToInt32(updateResult) > 0)
                    return ResponseEntity<object>.Success(updateResult, "Tax Class updated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update tax class.");
            }
            else
            {
                var result = await _taxClassRepo.InsertAsync(tClass);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success("Tax class created successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to create tax class.");
            }

        }

        public async Task<IActionResult> TaxClassList()
        {
            var response = await _taxClassRepo.QueryAsync<TaxClass>("select * from tbl_tax_classes");

            if (response.Any())
                return ResponseEntity<object>.Success(response);
            else
                return ResponseEntity<object>.Error("Tax Classes not available");
        }

        public async Task<IActionResult> TaxClassDropdown()
        {
            var filters = new Dictionary<string, object>
                {
                    { "is_active", true },
                };

            var response = await _taxClassRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_tax_classes", table_columns = "class_id as id, class_name as value", StaticFilters = filters });
            if (response.Any())
                return ResponseEntity<object>.Success(response);
            else
                return ResponseEntity<object>.Error("Tax Component not available");
        }

        public async Task<IActionResult> ManageTaxRule(TaxRule rule)
        {
            var validator = await _taxRuleRepo.ModelValidating(new ValidationModel() { ValidateModel = new TaxRuleValidator(), Model = rule });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            if (rule.rule_id > 0)
            {
                var updateTaxRule = await _taxRuleRepo.UpdateAsync(rule);

                if (Convert.ToInt32(updateTaxRule) > 0)
                    return ResponseEntity<object>.Success("Tax Rule updated successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to update tax rule.");
            }
            else
            {
                var result = await _taxRuleRepo.InsertAsync(rule);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success("Tax Rule created successfully.");
                else
                    return ResponseEntity<object>.Error("Failed to create tax rule.");
            }
        }

        public async Task<IActionResult> TaxRulesList(TaxRuleFilterDto dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "Data is missing or invalid.");

                var sql = new StringBuilder(@"
                    SELECT 
                        tr.rule_id, tr.class_id, tc.class_name, 
                        tr.component_id, comp.component_name, 
                        tr.transaction_type, tr.rate, tr.calculation_type, 
                        tr.valid_from, tr.valid_to, tr.is_active,
                        tr.created_at, tr.updated_at
                    FROM tbl_tax_rules tr
                    INNER JOIN tbl_tax_classes tc ON tc.class_id = tr.class_id
                    INNER JOIN tbl_tax_components comp ON comp.component_id = tr.component_id
                    WHERE 1=1");

                var parameters = new DynamicParameters();

                if (dto.class_id.HasValue && dto.class_id > 0)
                {
                    sql.Append(" AND tr.class_id = @class_id");
                    parameters.Add("class_id", dto.class_id.Value);
                }

                if (dto.component_id.HasValue && dto.component_id > 0)
                {
                    sql.Append(" AND tr.component_id = @component_id");
                    parameters.Add("component_id", dto.component_id.Value);
                }

                if (!string.IsNullOrWhiteSpace(dto.transaction_type))
                {
                    sql.Append(" AND tr.transaction_type = @transaction_type");
                    parameters.Add("transaction_type", dto.transaction_type.Trim());
                }

                if (dto.is_active.HasValue)
                {
                    sql.Append(" AND tr.is_active = @is_active");
                    parameters.Add("is_active", dto.is_active.Value);
                }

                sql.Append(" ORDER BY tc.class_name ASC, tr.transaction_type ASC, comp.component_name ASC");

                var rulesList = (await _taxRuleRepo.QueryAsync<dynamic>(sql.ToString(), parameters)).ToList();

                if (rulesList.Any())
                    return ResponseEntity<object>.Success(rulesList, "Tax rules retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No tax rules found matching the criteria.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> PreviewProductTax(TaxPreviewRequestDto request)
        {
            // 1. Fetch detailed active rules for this Class & Transaction Type
            string sql = @"SELECT tr.rate,tr.calculation_type,tc.component_name 
                FROM tbl_tax_rules tr
                JOIN tbl_tax_components tc ON tr.component_id = tc.component_id
                WHERE tr.class_id = @ClassId 
                  AND tr.transaction_type = @TransactionType
                  AND tr.is_active = true 
                  AND CURRENT_TIMESTAMP BETWEEN tr.valid_from AND COALESCE(tr.valid_to, '2099-12-31')";

            var param = new DynamicParameters();
            param.Add("ClassId", request.tax_class_id);
            param.Add("TransactionType", request.transaction_type);

            var rules = (await _taxRuleRepo.QueryAsync<TaxRule>(sql, param)).ToList();

            if (!rules.Any())
                return ResponseEntity<object>.Error(null, $"No active {request.transaction_type} tax rules found for this class.");

            //Separate Percentages and Flat Amounts
            decimal totalPercentage = rules.Where(r => r.calculation_type == "Percentage").Sum(r => (decimal)r.rate);
            decimal totalFlat = rules.Where(r => r.calculation_type == "FlatAmount").Sum(r => (decimal)r.rate);

            //Initialize the Response
            var response = new TaxPreviewResponseDto { entered_price = request.entered_price };
            decimal basePriceNoTax = 0;

            //Determine Base Price based on Inclusive/Exclusive flag
            if (request.is_inclusive)
            {
                // Reverse Math: Price entered already includes tax
                basePriceNoTax = Math.Round(((request.entered_price - totalFlat) * 100) / (100 + totalPercentage), 2);
                response.final_customer_price = request.entered_price; // Customer pays what was entered
            }
            else
            {
                // Forward Math: Price entered is the base price without tax
                basePriceNoTax = request.entered_price;
                response.final_customer_price = Math.Round(basePriceNoTax + (basePriceNoTax * totalPercentage / 100) + totalFlat, 2);
            }

            response.net_base_price = basePriceNoTax;

            //Calculate Line-by-Line Breakdown (CGST, SGST, IGST, Cess, etc.)
            decimal accumulatedTax = 0;

            foreach (var rule in rules)
            {
                decimal taxAmount = 0;

                if (rule.calculation_type == "Percentage")
                    taxAmount = Math.Round((basePriceNoTax * (decimal)rule.rate) / 100, 2);
                else if (rule.calculation_type == "FlatAmount")
                    taxAmount = (decimal)rule.rate;

                accumulatedTax += taxAmount;

                response.tax_breakdown.Add(new TaxComponentBreakdown
                {
                    component_name = rule.component_name,
                    rate = (decimal)rule.rate,
                    calculation_type = rule.calculation_type,
                    calculated_amount = taxAmount
                });
            }

            response.total_tax_amount = accumulatedTax;

            return ResponseEntity<object>.Success(response, "Tax preview calculated successfully.");
        }
    }
}
