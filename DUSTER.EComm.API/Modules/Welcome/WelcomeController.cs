using DUSTER.EComm.Services.CommonServices;
using Microsoft.AspNetCore.Authorization;
using System.Net.Sockets;
using System.Reflection;

namespace DUSTER.EComm.API.Modules.Welcome
{
    [Route("api/[controller]")]
    [ApiController]
    public class WelcomeController : ControllerBase
    {
        private readonly IWelcomeService _welcomeService;
        private static readonly DateTime _startTime = DateTime.Now;
        public WelcomeController(IWelcomeService welcomeService)
        {
            _welcomeService = welcomeService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
             
            var response = new
            {
                application = "Duster E-Commerce",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                environment = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Production" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                server_UTC_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                server_LOCAL_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                machine_name = Environment.MachineName,
                ip_address = GetLocalIPAddress(),
                uptime = (DateTime.Now - _startTime).ToString(@"dd\.hh\:mm\:ss")
            };

            return ResponseEntity<object>.Success(response);
        }

        [HttpGet("msg")]
        [AllowAnonymous]
        public async Task<IActionResult> Welcome()
        {
            return await _welcomeService.Welcome();
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "N/A";
        }
    }
}
