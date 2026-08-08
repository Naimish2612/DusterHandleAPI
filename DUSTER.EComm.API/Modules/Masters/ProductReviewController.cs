using DUSTER.EComm.Services.Modules.Catelog;
using DUSTER.EComm.Services.Modules.Catelog.Models;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/product/review")]
    [ApiController]
    public class ProductReviewController : ControllerBase
    {
        private readonly ICatelogServices _catelogServices;
        public ProductReviewController(ICatelogServices catelogServices)
        {
            _catelogServices = catelogServices;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProductReview([FromBody] ProductReviews model)
        {
            return await _catelogServices.AddProductReview(model);
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditProductReview([FromBody] ProductReviews model)
        {
            return await _catelogServices.EditProductReview(model);
        }

        [HttpGet("get/{reviewId}")]
        public async Task<IActionResult> GetProductReviewById(int reviewId)
        {
            return await _catelogServices.GetProductReviewById(reviewId);
        }

        [HttpGet("get/by/product/{productCode}")]
        public async Task<IActionResult> GetProductReviewsByProductCode(long productCode)
        {
            return await _catelogServices.GetProductReviewsByProductCode(productCode);
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishProductReview([FromBody] ProductReviewsDto dto)
        {
            return await _catelogServices.PublishProductReview(dto.product_review_id, dto.is_publish);
        }

        [HttpPost("delete/{reviewId}")]
        public async Task<IActionResult> DeleteProductReview(int reviewId)
        {
            return await _catelogServices.DeleteProductReview(reviewId);
        }

        [HttpGet("get/for/user")]
        public async Task<IActionResult> GetProductReviewsByUserCode()
        {
            return await _catelogServices.GetProductReviewsByUserCode();
        }


    }
}
