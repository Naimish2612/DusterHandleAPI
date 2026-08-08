using DUSTER.EComm.Services.Modules.DeliveryPolicy;
using DUSTER.EComm.Services.Modules.DeliveryPolicy.Models;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryPolicyController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryPolicyController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpPost("add/new/policy")]
        public async Task<IActionResult> CreatePolicy([FromBody] DeliveryPolicys model)
        {
            return await _deliveryService.CreatePolicyAsync(model);
        }

        [HttpPost("edit/policy")]
        public async Task<IActionResult> EditPolicy([FromBody] DeliveryPolicys model)
        {
            return await _deliveryService.EditPolicyAsync(model);
        }

        [HttpGet("get/all/policy")]
        public async Task<IActionResult> GetAllPolicies()
        {
            return await _deliveryService.GetAllPolicyAsync();
        }

        [HttpPost("mange/active/inactive/{policyId}")]
        public async Task<IActionResult> ManagePolicyStatus(long policyId)
        {
            return await _deliveryService.DeliveryPolicyActiveInactive(policyId);
        }

        [HttpPost("add/policy/slab")]
        public async Task<IActionResult> CreateSlab([FromBody] List<DeliveryPolicySlab> model)
        {
            return await _deliveryService.CreateSlabAsync(model);
        }

        [HttpPost("edit/policy/slab")]
        public async Task<IActionResult> EditSlab([FromBody] DeliveryPolicySlab model)
        {
            return await _deliveryService.EditSlabAsync(model);
        }

        [HttpGet("get/all/slab/{policyId}")]
        public async Task<IActionResult> GetAllSlabs(long policyId)
        {
            return await _deliveryService.GetSlabsByPolicyIdAsync(policyId);
        }

        [HttpPost("calculate/delivery/cost")]
        public async Task<IActionResult> CalculateDeliveryFee([FromBody] DeliveryFeeRequest request)
        {
            return await _deliveryService.CalculateDeliveryFeeAsync(request);
        }

        [HttpPost("simulator/delivery/cost/calculation/{orderAmount}")]
        public async Task<IActionResult> SimulateDeliveryFee([FromQuery] decimal orderAmount)
        {
            return await _deliveryService.SimulateDeliveryAmountAsync(orderAmount);
        }

        [HttpPost("add/order/delivery/calculation")]
        public async Task<IActionResult> CreateCalculationTracking([FromBody] OrderDeliveryCalculation model)
        {
            return await _deliveryService.AddOrderCalculationAsync(model);
        }
    }
}
