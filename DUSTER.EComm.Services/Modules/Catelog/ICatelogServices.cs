using DUSTER.EComm.Services.Modules.Catelog.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Catelog
{
    public interface ICatelogServices
    {
        #region  Product

        Task<IActionResult> AddProduct(IFormCollection model);
        Task<IActionResult> EditProduct(ProductMasterDto dto);
        Task<IActionResult> GetProductsBySlugValue(string? slug);
        Task<IActionResult> AddProductImages(IFormCollection model);
        Task<IActionResult> DeleteImages(long[] imageIds, long productCode);
        Task<IActionResult> ProductList(ProductFilterDto dto);
        Task<IActionResult> AdminProductList(ProductFilterDto dto);
        Task<IActionResult> TopSellingProduct();
        Task<IActionResult> NewArrivalProduct();
        Task<IActionResult> GetCurrentImagesByProductCode(long productId);
        Task<IActionResult> ProductSearch(string searchText);

        #endregion

        #region Product Review

        Task<IActionResult> AddProductReview(ProductReviews model);
        Task<IActionResult> EditProductReview(ProductReviews model);
        Task<IActionResult> GetProductReviewById(long reviewId);
        Task<IActionResult> GetProductReviewsByProductCode(long productCode);
        Task<IActionResult> PublishProductReview(long reviewId, bool publish);
        Task<IActionResult> DeleteProductReview(long reviewId);
        Task<IActionResult> GetProductReviewsByUserCode();

        #endregion

        #region Product Wishlist

        Task<IActionResult> AddToWishlist(ProductWishlistDTO dto);
        Task<IActionResult> RemoveFromWishlist(ProductWishlistDTO dto);
        Task<IActionResult> GetProductWishlist(long user_code);

        #endregion
    }
}
