namespace DUSTER.EComm.Services.CommonServices
{
    public class WelcomeService : IWelcomeService
    {
        public WelcomeService()
        {

        }

        public async Task<IActionResult> Welcome()
        {
            try
            {
                return ResponseEntity<object>.Success(new { message = "Welcome to Duster E-Commerce API" });
            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error( new { error = ex.Message },"Failed to get welcome message");
            }
        }
    }
}
