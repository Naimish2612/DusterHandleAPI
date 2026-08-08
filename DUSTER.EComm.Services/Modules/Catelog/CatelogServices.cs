using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Pagination;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.Catelog
{
    public class CatelogServices : ICatelogServices
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IEIPLRepository<ProductMaster> _productRepository;
        private readonly IEIPLRepository<ProductImages> _productImagesRepo;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private readonly IEIPLRepository<ProductReviews> _productReviewsRepo;
        private readonly IEIPLRepository<ProductWishlist> _productWishlistRepo;

        public CatelogServices(ICurrentUserService currentUserService, IEIPLRepository<ProductMaster> productRepository, IEIPLRepository<ProductImages> productImagesRepo,
             IOptions<CloudinarySettings> cloudinarySettings, IEIPLRepository<ProductReviews> productReviewsRepo, IEIPLRepository<ProductWishlist> productWishlistRepo)
        {
            _currentUserService = currentUserService;
            _productRepository = productRepository;
            _productImagesRepo = productImagesRepo;
            _cloudinarySettings = cloudinarySettings;
            Account account = new Account(cloudinarySettings.Value.CloudName, cloudinarySettings.Value.ApiKey, cloudinarySettings.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _productReviewsRepo = productReviewsRepo;
            _productWishlistRepo = productWishlistRepo;
        }

        #region Product 

        public async Task<IActionResult> AddProduct(IFormCollection model)
        {
            try
            {
                if (model == null || !model.ContainsKey("data"))
                    return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

                if (model.Files.Count < 3)
                    return ResponseEntity<object>.Error(null, "At least 3 images are required.");

                var data = model["data"].ToString();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pm = System.Text.Json.JsonSerializer.Deserialize<ProductMaster>(data, options);

                if (pm == null)
                    return ResponseEntity<object>.Error(null, "Failed to parse product data.");

                #region Check Duplicate Validation

                if (await IsProductNameExistsAsync(pm.name))
                    return ResponseEntity<object>.Error(null, "Product name already exist.");

                if (await IsProductSlugExistsAsync(pm.slug))
                    return ResponseEntity<object>.Error(null, "Product slug should be unique");

                #endregion

                var validator = await _productRepository.ModelValidating(new ValidationModel()
                {
                    ValidateModel = new ProductMasterValidator(),
                    Model = pm
                });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                string attributesJson = pm.attribute == null || pm.attribute.Count == 0 ? "{}" : System.Text.Json.JsonSerializer.Serialize(pm.attribute);

                pm.attributes = attributesJson;

                var result = await _productRepository.InsertAsync(pm);
                long product_code = Convert.ToInt64(result);

                if (product_code > 0)
                {
                    var imageUploadResult = await AddProductImages(model.Files, product_code);

                    return ResponseEntity<object>.Success(result, "Product added successfully.");
                }
                else
                {
                    return ResponseEntity<object>.Error(null, "Failed to add product to the database.");
                }
            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, $"An error occurred while processing your request: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<IActionResult> AddProductImages(IFormFileCollection files, long productCode)
        {
            try
            {
                if (productCode <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.", HttpStatusCode.InternalServerError);

                if (files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "Product image is required.", HttpStatusCode.InternalServerError);

                // ✅ Check ONCE before the loop — does a primary image already exist?
                var PrimaryproductImages = await _productImagesRepo.QueryDynamicAsync("select * from tbl_product_images where product_code = @product_code and is_primary = true",
                    new DynamicParameters(new { product_code = productCode }));
                bool hasPrimary = PrimaryproductImages != null && PrimaryproductImages.Any();
                bool primaryAssignedInThisBatch = false;

                string[] array = { "jpg", "jpeg", "png" };
                for (int i = 0; i < files.Count; i++)
                {
                    IFormFile _file;
                    _file = files[i];

                    using var stream = _file.OpenReadStream();

                    string fileExtension = Path.GetExtension(_file.FileName);

                    if (array.Contains(fileExtension))
                        return ResponseEntity<object>.Error(null, "Only jpg, jpeg and png file are allowed.", HttpStatusCode.InternalServerError);

                    string file_name = $"{productCode}_{StringHelper.GetUniqueString(10)}{Path.GetExtension(files[i].FileName)}";
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file_name, stream),
                        Folder = "EComm/catelog_photo",// the specific folder you want in Cloudinary
                        UseFilename = true,                   // use original filename
                        UniqueFilename = true,                // let Cloudinary add uniqueness
                        Overwrite = false,                    // do not overwrite existing
                                                              //ResourceType = ResourceType.Image
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK && uploadResult.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        continue;
                    }

                    // public url (https)
                    var publicUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Uri?.ToString();

                    // ✅ Decide primary — only if no primary in DB AND none assigned yet in this batch
                    bool shouldBePrimary = !hasPrimary && !primaryAssignedInThisBatch;

                    ///save image path to db
                    ProductImages productImage = new ProductImages()
                    {
                        product_code = productCode,
                        image_url = publicUrl,
                        alt_text = "EIPL EComm",
                        display_order = i + 1,
                        is_primary = shouldBePrimary,
                        image_public_id = uploadResult.PublicId
                    };

                    await _productImagesRepo.InsertAsync(productImage);

                    // ✅ Lock — once assigned, don't assign again in this batch
                    if (shouldBePrimary)
                        primaryAssignedInThisBatch = true;
                }

                return ResponseEntity<object>.Success(null, "Product Images uploaded successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> AddProductImages(IFormCollection model)
        {
            try
            {
                if (model == null || !model.ContainsKey("data"))
                    return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

                if (model.Files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "At least 1 images are required.");

                // 1. Directly extract the JSON string without looping
                var data = model["data"].ToString();

                var pm = System.Text.Json.JsonSerializer.Deserialize<ProductMaster>(data);

                if (pm.product_code <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                var result = await AddProductImages(model.Files, pm.product_code);

                return result;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductsBySlugValue(string? slug)
        {
            try
            {
                if (string.IsNullOrEmpty(slug))
                    return ResponseEntity<object>.Error(null, "passing value are null or empty.");

                var param = new DynamicParameters();
                param.Add("@slug", slug);

                string sql = @"SELECT 
                                product_code, p.name, sku, p.slug, p.description, base_price, p.category_id,pc.name as category_name, 
                                p.sub_category_id,psc.name as sub_category_name, p.manufacturer_id,m.name as manufacturer_name, p.is_active, in_stock, stock_quantity,
                                attributes::text as attributes,is_top_selling,is_new_arrival,tax_class_id,estimated_delivery_days,p.actual_price, p.sap_sku_code
                            FROM tbl_products as p
                            INNER JOIN tbl_product_category as pc on pc.category_id=p.category_id
                            INNER JOIN tbl_product_sub_category as psc on psc.sub_category_id=p.sub_category_id
                            INNER JOIN tbl_manufacturer as m on m.manufacturer_id=p.manufacturer_id
                            WHERE p.slug = @slug";

                var product = (await _productRepository.QueryDynamicAsync(sql, param)).FirstOrDefault();

                if (product == null)
                    return ResponseEntity<object>.Error(null, "Product not found.");

                param = new DynamicParameters();
                param.Add("@product_code", product.product_code);

                var images = await _productImagesRepo.QueryAsync<ProductImages>("SELECT * FROM tbl_product_images WHERE product_code = @product_code", param);

                string reviewSql = @"SELECT u.user_name, rating, review_title, comment, is_verified_purchase, is_publish,pr.created_at as review_datetime
                                FROM tbl_product_reviews as pr
                                INNER JOIN tbl_users as u on pr.user_id=u.user_code
                                WHERE product_code = @product_code AND is_publish = true";

                var productReviews = await _productReviewsRepo.QueryDynamicAsync(reviewSql, param);

                var responseDTO = new ProductMasterDto()
                {
                    product_code = product.product_code,
                    name = product.name,
                    sku = product.sku,
                    slug = product.slug,
                    description = product.description,
                    base_price = product.base_price,
                    actual_price = product.actual_price,
                    category_id = product.category_id,
                    category_name = product.category_name,
                    sub_category_id = product.sub_category_id,
                    sub_category_name = product.sub_category_name,
                    manufacturer_id = product.manufacturer_id,
                    manufacturer_name = product.manufacturer_name,
                    is_active = product.is_active,
                    in_stock = product.in_stock,
                    is_new_arrival = product.is_new_arrival,
                    is_top_selling = product.is_top_selling,
                    stock_quantity = product.stock_quantity,
                    sap_sku_code = product.sap_sku_code,
                    product_reviews = productReviews.Select(r => new
                    {
                        user_name = r.user_name,
                        rating = r.rating,
                        review_title = r.review_title,
                        comment = r.comment,
                        is_verified_purchase = r.is_verified_purchase,
                        is_publish = r.is_publish,
                        review_datetime = r.review_datetime
                    }).ToList(),
                    product_images = images.Select(img => new
                    {
                        image_url = img.image_url,
                        alt_text = img.alt_text,
                        display_order = img.display_order,
                        is_primary = img.is_primary
                    }).ToList(),
                    attribute = string.IsNullOrEmpty(product.attributes) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(product.attributes),
                    tax_class_id = product.tax_class_id,
                    estimated_delivery_days = product.estimated_delivery_days == null ? 0 : product.estimated_delivery_days
                };


                return ResponseEntity<object>.Success(responseDTO, "Product retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditProduct(ProductMasterDto dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "data is missing or invalid.");

                var param = new DynamicParameters();
                param.Add("@product_code", dto.product_code);

                ProductMaster pm = await _productRepository.GetSingleOrDefaultAsync("SELECT * FROM tbl_products WHERE product_code = @product_code", param);

                if (pm == null)
                    return ResponseEntity<object>.Error(null, "Product not found.");

                pm.name = dto.name;
                pm.sku = dto.sku;
                pm.slug = dto.slug;
                pm.description = dto.description;
                pm.base_price = dto.base_price;
                pm.actual_price = dto.actual_price;
                pm.category_id = dto.category_id;
                pm.sub_category_id = dto.sub_category_id;
                pm.manufacturer_id = dto.manufacturer_id;
                pm.is_active = dto.is_active;
                pm.in_stock = dto.in_stock;
                pm.is_top_selling = dto.is_top_selling;
                pm.is_new_arrival = dto.is_new_arrival;
                pm.stock_quantity = dto.stock_quantity;
                pm.tax_class_id = dto.tax_class_id;
                pm.estimated_delivery_days = dto.estimated_delivery_days;
                pm.sap_sku_code = dto.sap_sku_code;
                string attributesJson = dto.attribute == null || dto.attribute.Count == 0 ? "{}" : System.Text.Json.JsonSerializer.Serialize(dto.attribute);

                pm.attributes = attributesJson;

                var validator = await _productRepository.ModelValidating(new ValidationModel() { ValidateModel = new ProductMasterValidator(), Model = pm });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var result = await _productRepository.UpdateAsync(pm);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success(null, "Product updated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update product.");

            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, $"An error occurred while processing your request: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<IActionResult> DeleteImages(long[] imageIds, long productCode)
        {
            try
            {
                if (imageIds == null || imageIds.Length == 0 || productCode <= 0)
                    return ResponseEntity<object>.Error(null, "passing data is null or empty.");

                var param = new DynamicParameters();
                param.Add("product_code", productCode);
                param.Add("product_image_id", imageIds.Select(x => x).ToArray());

                List<ProductImages> productImages = (await _productImagesRepo.QueryAsync<ProductImages>($"SELECT * FROM tbl_product_images " +
                    $"WHERE product_code = @product_code and product_image_id = ANY(@product_image_id)", param)).ToList();

                if (productImages == null || productImages.Count == 0)
                    return ResponseEntity<object>.Error(null, "No images found for the provided data and product.");

                var deleteParams = new DelResParams()
                {
                    PublicIds = new List<string> { string.Join(",", productImages.Select(x => x.image_public_id)) },
                    Type = "upload",
                    ResourceType = ResourceType.Image
                };

                var result = _cloudinary.DeleteResources(deleteParams);

                for (int i = 0; i < productImages.Count; i++)
                {
                    int deleteCount = await _productImagesRepo.DeleteAsync(productImages[i].product_image_id);
                }

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    return ResponseEntity<object>.Error(null, "Failed to delete images from Cloudinary.");


                return ResponseEntity<object>.Success(null, "Selected images deleted successfully.");
            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, $"An error occurred while processing your request: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<IActionResult> ProductList(ProductFilterDto dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "data is missing or invalid.");

                var sql = new StringBuilder(@"SELECT product_code, p.name, p.sku, p.slug, p.description,actual_price, base_price, 
                p.category_id, p.sub_category_id, p.manufacturer_id,p.is_active, in_stock, stock_quantity,
                p.is_top_selling, p.is_new_arrival,pc.name as category_name, psc.name as sub_category_name, m.name as manufacturer_name,
                attributes::text as attributes,tax_class_id,estimated_delivery_days,p.sap_sku_code 
                FROM tbl_products as p
                INNER JOIN tbl_product_category as pc on pc.category_id=p.category_id
                INNER JOIN tbl_product_sub_category as psc on psc.sub_category_id=p.sub_category_id
                INNER JOIN tbl_manufacturer as m on m.manufacturer_id=p.manufacturer_id
                WHERE 1=1 and p.is_active = true");

                var parameters = new DynamicParameters();

                // 2. Dynamically append conditions
                if (!string.IsNullOrWhiteSpace(dto.name))
                {
                    // Use ILIKE in PostgreSQL for case-insensitive partial matching
                    sql.Append(" AND p.name ILIKE @name");
                    parameters.Add("name", $"%{dto.name.Trim()}%");
                }

                if (!string.IsNullOrWhiteSpace(dto.sku))
                {
                    // Exact match for SKU
                    sql.Append(" AND p.sku = @sku");
                    parameters.Add("sku", dto.sku.Trim());
                }

                if (dto.category_id.HasValue && dto.category_id > 0)
                {
                    sql.Append(" AND p.category_id = @category_id");
                    parameters.Add("category_id", dto.category_id.Value);
                }

                if (dto.sub_category_id.HasValue && dto.sub_category_id > 0)
                {
                    sql.Append(" AND p.sub_category_id = @sub_category_id");
                    parameters.Add("sub_category_id", dto.sub_category_id.Value);
                }

                if (dto.manufacturer_id.HasValue && dto.manufacturer_id > 0)
                {
                    sql.Append(" AND p.manufacturer_id = @manufacturer_id");
                    parameters.Add("manufacturer_id", dto.manufacturer_id.Value);
                }

                //if (dto.is_active.HasValue)
                //{
                //    sql.Append(" AND p.is_active = @is_active");
                //    parameters.Add("is_active", dto.is_active.Value);
                //}

                if (dto.in_stock.HasValue)
                {
                    sql.Append(" AND p.in_stock = @in_stock");
                    parameters.Add("in_stock", dto.in_stock.Value);
                }

                // Optional: Add default sorting
                sql.Append(" ORDER BY product_code DESC");


                var productListData = await _productRepository.QueryDynamicAsync(sql.ToString(), parameters);
                //var productListData = await _productRepository.QueryPagedAsync<dynamic>(sql.ToString(), dto, parameters);

                // 4. Map back to ProductMaster list
                var products = new List<ProductMasterDto>();
                foreach (var row in productListData)
                {
                    var param = new DynamicParameters();
                    param.Add("@product_code", row.product_code);

                    var images = await _productImagesRepo.QueryAsync<ProductImages>("SELECT * FROM tbl_product_images WHERE product_code = @product_code", param);

                    string reviewSql = @"SELECT rating FROM tbl_product_reviews as pr WHERE product_code = @product_code AND is_publish = true";

                    var productReviews = await _productReviewsRepo.QueryAsync<ProductReviews>(reviewSql, param);

                    // 1. Ensure the variable receiving the result is a decimal
                    decimal avgRating = 0;

                    if (productReviews != null && productReviews.Any())
                    {
                        // 2. Parse the string property into a decimal inside Average
                        avgRating = Math.Ceiling(productReviews.Average(p => decimal.Parse(p.rating ?? "0")));
                    }

                    products.Add(new ProductMasterDto
                    {
                        product_code = row.product_code,
                        name = row.name,
                        sku = row.sku,
                        slug = row.slug,
                        description = row.description,
                        base_price = row.base_price,
                        actual_price = row.actual_price,
                        category_id = row.category_id,
                        sub_category_id = row.sub_category_id,
                        manufacturer_id = row.manufacturer_id,
                        is_active = row.is_active,
                        in_stock = row.in_stock,
                        stock_quantity = row.stock_quantity,
                        is_top_selling = row.is_top_selling,
                        is_new_arrival = row.is_new_arrival,
                        category_name = row.category_name,
                        sub_category_name = row.sub_category_name,
                        manufacturer_name = row.manufacturer_name,
                        attribute = string.IsNullOrEmpty(row.attributes) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(row.attributes),
                        product_images = images.Select(img => new
                        {
                            image_url = img.image_url,
                            alt_text = img.alt_text,
                            display_order = img.display_order,
                            is_primary = img.is_primary
                        }).ToList(),
                        total_review = productReviews.Count(),
                        rating = avgRating.ToString(),
                        tax_class_id = row.tax_class_id == null ? 0 : row.tax_class_id,
                        estimated_delivery_days = row.estimated_delivery_days == null ? 0 : row.estimated_delivery_days
                    });
                }
                //var finalResult = new PaginatedResult<ProductMasterDto>
                //{
                //    Data = products,
                //    Metadata = productListData.Metadata
                //};

                if (products.Count > 0)
                    return ResponseEntity<object>.Success(products, "Products retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No products found matching the criteria.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<IActionResult> AdminProductList(ProductFilterDto dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "data is missing or invalid.");

                var sql = new StringBuilder(@"SELECT product_code, p.name, p.sku, p.slug, p.description,actual_price,base_price,pc.name as category_name, psc.name as sub_category_name,
                m.name as manufacturer_name,  tc.class_name as tax_class_name ,
                p.category_id, p.sub_category_id, p.manufacturer_id,p.is_active, in_stock, stock_quantity,
                p.is_top_selling, p.is_new_arrival, tax_class_id, estimated_delivery_days
                FROM tbl_products as p
                INNER JOIN tbl_tax_classes as tc on tc.class_id = p.tax_class_id
                INNER JOIN tbl_product_category as pc on pc.category_id=p.category_id
                INNER JOIN tbl_product_sub_category as psc on psc.sub_category_id=p.sub_category_id
                INNER JOIN tbl_manufacturer as m on m.manufacturer_id=p.manufacturer_id
                WHERE 1=1");

                var parameters = new DynamicParameters();

                // 2. Dynamically append conditions
                if (!string.IsNullOrWhiteSpace(dto.name))
                {
                    // Use ILIKE in PostgreSQL for case-insensitive partial matching
                    sql.Append(" AND p.name ILIKE @name");
                    parameters.Add("name", $"%{dto.name.Trim()}%");
                }

                if (!string.IsNullOrWhiteSpace(dto.sku))
                {
                    // Exact match for SKU
                    sql.Append(" AND p.sku = @sku");
                    parameters.Add("sku", dto.sku.Trim());
                }

                if (dto.category_id.HasValue && dto.category_id > 0)
                {
                    sql.Append(" AND p.category_id = @category_id");
                    parameters.Add("category_id", dto.category_id.Value);
                }

                if (dto.sub_category_id.HasValue && dto.sub_category_id > 0)
                {
                    sql.Append(" AND p.sub_category_id = @sub_category_id");
                    parameters.Add("sub_category_id", dto.sub_category_id.Value);
                }

                if (dto.manufacturer_id.HasValue && dto.manufacturer_id > 0)
                {
                    sql.Append(" AND p.manufacturer_id = @manufacturer_id");
                    parameters.Add("manufacturer_id", dto.manufacturer_id.Value);
                }

                if (dto.is_active.HasValue)
                {
                    sql.Append(" AND p.is_active = @is_active");
                    parameters.Add("is_active", dto.is_active.Value);
                }

                if (dto.in_stock.HasValue)
                {
                    sql.Append(" AND p.in_stock = @in_stock");
                    parameters.Add("in_stock", dto.in_stock.Value);
                }

                // Optional: Add default sorting
                sql.Append(" ORDER BY product_code DESC");


                //var productListData = await _productRepository.QueryDynamicAsync(sql.ToString(), parameters);
                var productListData = await _productRepository.QueryPagedAsync<dynamic>(sql.ToString(), dto, parameters);


                if (productListData.Data.Count() > 0)
                    return ResponseEntity<object>.Success(productListData, "Products retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No products found matching the criteria.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IActionResult> TopSellingProduct()
        {
            try
            {
                string sql = @"SELECT product_code, name, sku, sap_sku_code, slug, description,actual_price,base_price, category_id, sub_category_id, manufacturer_id,is_active, in_stock, stock_quantity,
                                is_top_selling, is_new_arrival,tax_class_id FROM tbl_products WHERE is_top_selling = true and is_active = true";

                var topSellingProducts = (await _productRepository.QueryDynamicAsync(sql)).ToList();

                if (topSellingProducts.Count <= 0)
                    return ResponseEntity<object>.Error(null, "No top selling products found.");


                var response = new List<ProductMasterDto>();
                foreach (var x in topSellingProducts)
                {
                    var images = await _productImagesRepo.QueryAsync<ProductImages>("SELECT image_url,alt_text,display_order,is_primary FROM tbl_product_images WHERE product_code = @product_code and display_order = 1", new DynamicParameters(new { product_code = x.product_code }));

                    string reviewSql = $"SELECT rating FROM tbl_product_reviews as pr WHERE product_code = {x.product_code} AND is_publish = true";
                    var productReviews = await _productReviewsRepo.QueryAsync<ProductReviews>(reviewSql);
                    decimal avgRating = 0;

                    if (productReviews != null && productReviews.Any())
                    {
                        // 2. Parse the string property into a decimal inside Average
                        avgRating = Math.Ceiling(productReviews.Average(p => decimal.Parse(p.rating ?? "0")));
                    }

                    response.Add(new ProductMasterDto()
                    {
                        product_code = x.product_code,
                        name = x.name,
                        sku = x.sku,
                        sap_sku_code = x.sap_sku_code,
                        slug = x.slug,
                        description = x.description,
                        base_price = x.base_price,
                        actual_price = x.actual_price,
                        category_id = x.category_id,
                        sub_category_id = x.sub_category_id,
                        manufacturer_id = x.manufacturer_id,
                        is_active = x.is_active,
                        in_stock = x.in_stock,
                        stock_quantity = x.stock_quantity,
                        is_top_selling = x.is_top_selling,
                        is_new_arrival = x.is_new_arrival,
                        tax_class_id = x.tax_class_id,
                        total_review = productReviews.Count(),
                        rating = avgRating.ToString(),
                        product_images = images.Select(img => new
                        {
                            image_url = img.image_url,
                            alt_text = img.alt_text,
                            display_order = img.display_order,
                            is_primary = img.is_primary
                        }).ToList(),
                    });
                }

                if (response != null)
                    return ResponseEntity<object>.Success(response, "Top selling products retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No top selling products found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> NewArrivalProduct()
        {
            try
            {
                string sql = @"SELECT product_code, name, sku, sap_sku_code, slug, description, actual_price, base_price, category_id, sub_category_id, manufacturer_id,is_active, in_stock, stock_quantity,
                                is_top_selling, is_new_arrival,tax_class_id FROM tbl_products WHERE is_new_arrival = true and is_active = true";

                var newArrivalProducts = (await _productRepository.QueryDynamicAsync(sql)).ToList();

                if (newArrivalProducts.Count <= 0)
                    return ResponseEntity<object>.Error(null, "No new arrival products found.");

                var response = new List<ProductMasterDto>();
                foreach (var x in newArrivalProducts)
                {
                    var images = await _productImagesRepo.QueryAsync<ProductImages>("SELECT image_url,alt_text,display_order,is_primary FROM tbl_product_images WHERE product_code = @product_code  and display_order = 1", new DynamicParameters(new { product_code = x.product_code }));

                    string reviewSql = $"SELECT rating FROM tbl_product_reviews as pr WHERE product_code = {x.product_code} AND is_publish = true";
                    var productReviews = await _productReviewsRepo.QueryAsync<ProductReviews>(reviewSql);
                    decimal avgRating = 0;

                    if (productReviews != null && productReviews.Any())
                    {
                        // 2. Parse the string property into a decimal inside Average
                        avgRating = Math.Ceiling(productReviews.Average(p => decimal.Parse(p.rating ?? "0")));
                    }

                    response.Add(new ProductMasterDto()
                    {
                        product_code = x.product_code,
                        name = x.name,
                        sku = x.sku,
                        sap_sku_code = x.sap_sku_code,
                        slug = x.slug,
                        description = x.description,
                        base_price = x.base_price,
                        actual_price = x.actual_price,
                        category_id = x.category_id,
                        sub_category_id = x.sub_category_id,
                        manufacturer_id = x.manufacturer_id,
                        is_active = x.is_active,
                        in_stock = x.in_stock,
                        stock_quantity = x.stock_quantity,
                        is_top_selling = x.is_top_selling,
                        is_new_arrival = x.is_new_arrival,
                        tax_class_id = x.tax_class_id,
                        total_review = productReviews.Count(),
                        rating = avgRating.ToString(),
                        product_images = images.Select(img => new
                        {
                            image_url = img.image_url,
                            alt_text = img.alt_text,
                            display_order = img.display_order,
                            is_primary = img.is_primary
                        }).ToList(),
                    });
                }

                if (response != null)
                    return ResponseEntity<object>.Success(response, "Top selling products retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No top selling products found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetCurrentImagesByProductCode(long productId)
        {
            try
            {
                if (productId <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");

                var product = await _productRepository.GetByIdAsync(productId);

                if (product == null)
                    return ResponseEntity<object>.Error(null, "Product Not Found");

                var productImages = await _productImagesRepo.QueryDynamicAsync("select * from tbl_product_images where product_code = @product_code", new DynamicParameters(new { product_code = product.product_code }));

                if (productImages == null)
                    return ResponseEntity<object>.Error(null, "Image not available");

                return ResponseEntity<object>.Success(productImages.Select(x => new
                {
                    x.product_image_id,
                    x.image_url,
                    x.product_code,
                    x.is_primary,
                    x.alt_text,
                    image_public_id = (x.image_public_id == null ? "No Name" : x.image_public_id)
                }));

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> ProductSearch(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return ResponseEntity<object>.Success(null);

                string sql = @"
                    SELECT product_code, p.name,p.sku,p.description,p.is_active,p.base_price,p.slug,pc.name as category_name,psc.name as sub_category_name,m.name as manufacturer_name,p.sap_sku_code FROM tbl_products as p
                    INNER JOIN tbl_product_category as pc on pc.category_id=p.category_id
                    INNER JOIN tbl_product_sub_category as psc on psc.sub_category_id=p.sub_category_id
                    INNER JOIN tbl_manufacturer as m on m.manufacturer_id=p.manufacturer_id
                    WHERE p.is_active = true 
                    AND (
                        p.name ILIKE @Search 
                        OR p.sku ILIKE @Search 
                        OR p.slug ILIKE @Search 
                        OR p.description ILIKE @Search
                        OR pc.name ILIKE @Search
                        OR psc.name ILIKE @Search
                        OR m.name ILIKE @Search
                        OR attributes::text ILIKE @Search
                    )
                    ORDER BY 
                        CASE 
                            WHEN p.name ILIKE @ExactSearch THEN 1 -- Exact name matches first
                            WHEN p.name ILIKE @StartSearch THEN 2 -- Starts with name second
                            WHEN p.sku ILIKE @ExactSearch THEN 3  -- Exact SKU match third
                            ELSE 4 
                        END, 
                        name ASC";

                var parameters = new DynamicParameters();
                parameters.Add("Search", $"%{searchText}%");
                parameters.Add("ExactSearch", searchText);
                parameters.Add("StartSearch", $"{searchText}%");

                var products = await _productRepository.QueryAsync<ProductMasterDto>(sql, parameters);

                var productDTO = new List<ProductMasterDto>();
                foreach (var product in products)
                {
                    var param = new DynamicParameters();
                    param.Add("@product_code", product.product_code);

                    var images = await _productImagesRepo.QueryAsync<ProductImages>("SELECT * FROM tbl_product_images WHERE product_code = @product_code", param);

                    product.attribute = string.IsNullOrEmpty(product.attributes) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(product.attributes);
                    product.product_images = images.Select(img => new
                    {
                        image_url = img.image_url,
                        alt_text = img.alt_text,
                        display_order = img.display_order,
                        is_primary = img.is_primary
                    }).ToList();
                }

                return ResponseEntity<object>.Success(products.Select(x => new
                {
                    x.name,
                    x.category_name,
                    x.sub_category_name,
                    x.sku,
                    x.sap_sku_code,
                    x.slug,
                    x.base_price,
                    x.is_active,
                    x.is_new_arrival,
                    x.is_top_selling,
                    x.in_stock,
                    x.product_images
                }).ToList());

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Validation Methods

        public async Task<bool> IsProductNameExistsAsync(string productName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("name", productName);

            var product = await _productRepository.GetSingleOrDefaultAsync("SELECT name FROM tbl_products WHERE name = @name", parameters);

            return product != null;
        }

        public async Task<bool> IsProductSlugExistsAsync(string slugName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("slug", slugName);

            var product = await _productRepository.GetSingleOrDefaultAsync("SELECT slug FROM tbl_products WHERE slug = @slug", parameters);

            return product != null;
        }

        #endregion

        #region Product Review

        public async Task<IActionResult> AddProductReview(ProductReviews model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "data is missing or invalid.");

                model.user_id = _currentUserService.User.user_code;
                model.created_at = DateTime.Now;
                model.is_publish = false; // default to false, admin can review and publish it later

                var validator = await _productReviewsRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductReviewsValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var result = await _productReviewsRepo.InsertAsync(model);

                if (Convert.ToInt64(result) > 0)
                    return ResponseEntity<object>.Success(null, "Review added successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add review.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditProductReview(ProductReviews model)
        {
            try
            {
                if (model == null || model.product_review_id <= 0)
                    return ResponseEntity<object>.Error(null, "data is missing or invalid.");

                var param = new DynamicParameters();
                param.Add("@product_review_id", model.product_review_id);

                ProductReviews review = await _productReviewsRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_product_reviews WHERE product_review_id = @product_review_id", param);
                if (review == null)
                    return ResponseEntity<object>.Error(null, "Review not found.");

                if (review.user_id != _currentUserService.User.user_code)
                    return ResponseEntity<object>.Error(null, "You are not authorized to edit this review.");

                review.rating = model.rating;
                review.review_title = model.review_title;
                review.comment = model.comment;
                review.is_verified_purchase = model.is_verified_purchase;

                var validator = await _productReviewsRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductReviewsValidator(), Model = review });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var result = await _productReviewsRepo.UpdateAsync(review);

                if (Convert.ToInt64(result) > 0)
                    return ResponseEntity<object>.Success(null, "Review updated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update review.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductReviewById(long reviewId)
        {
            try
            {
                if (reviewId <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                ProductReviews review = await _productReviewsRepo.GetByIdAsync(reviewId);

                if (review != null)
                    return ResponseEntity<object>.Success(review, "Review retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Review not found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductReviewsByProductCode(long productCode)
        {
            try
            {
                if (productCode <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                var param = new DynamicParameters();
                param.Add("@product_code", productCode);

                string reviewSql = @"SELECT u.user_name,pr.product_code,p.name as product_name, rating, review_title, comment, is_verified_purchase,pr.is_publish,pr.is_delete,pr.product_review_id,pr.created_at as review_datetime
                                FROM tbl_product_reviews as pr
                                INNER JOIN tbl_products as p on p.product_code=pr.product_code
                                INNER JOIN tbl_users as u on pr.user_id=u.user_code
                                WHERE pr.product_code = @product_code";

                var productReviews = await _productReviewsRepo.QueryDynamicAsync(reviewSql, param);

                if (productReviews != null)
                {
                    if (_currentUserService.User.user_type == "CUSTOMER")
                        return ResponseEntity<object>.Success(productReviews.Where(x => x.is_publish == true).Select(x => new
                        {
                            x.user_name,
                            x.product_code,
                            x.product_name,
                            x.rating,
                            x.review_title,
                            x.comment,
                            x.is_verified_purchase,
                            x.review_datetime

                        }).ToList(), "Product reviews retrieved successfully.");
                    else if (_currentUserService.User.user_type == "ADMIN")
                    {
                        return ResponseEntity<object>.Success(productReviews.Select(x => new
                        {
                            x.is_publish,
                            x.is_delete,
                            x.product_review_id,
                            x.user_name,
                            x.product_code,
                            x.product_name,
                            x.rating,
                            x.review_title,
                            x.comment,
                            x.is_verified_purchase,
                            x.review_datetime

                        }).ToList(), "Product reviews retrieved successfully.");
                    }
                    else
                        return ResponseEntity<object>.Success(productReviews, "Product reviews retrieved successfully.");
                }
                else
                    return ResponseEntity<object>.Error(null, "No reviews found for this product.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> PublishProductReview(long reviewId, bool publish)
        {
            try
            {
                if (reviewId <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                var param = new DynamicParameters();
                param.Add("@product_review_id", reviewId);

                ProductReviews review = await _productReviewsRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_product_reviews WHERE product_review_id = @product_review_id", param);

                if (review == null)
                    return ResponseEntity<object>.Error(null, "Review not found.");

                if (review.is_delete)
                    return ResponseEntity<object>.Error(null, "Cannot publish a deleted review.");

                review.is_publish = publish;
                review.is_verified_purchase = publish;

                var result = await _productReviewsRepo.UpdateAsync(review);

                if (Convert.ToInt64(result) > 0)
                    return ResponseEntity<object>.Success(null, $"Review {(publish ? "published" : "unpublished")} successfully.");
                else
                    return ResponseEntity<object>.Error(null, $"Failed to {(publish ? "publish" : "unpublish")} review.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> DeleteProductReview(long reviewId)
        {
            try
            {
                if (reviewId <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                var param = new DynamicParameters();
                param.Add("@product_review_id", reviewId);

                ProductReviews review = await _productReviewsRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_product_reviews WHERE product_review_id = @product_review_id", param);

                if (review == null)
                    return ResponseEntity<object>.Error(null, "Review not found.");

                review.is_delete = true;

                var result = await _productReviewsRepo.UpdateAsync(review);

                if (Convert.ToInt64(result) > 0)
                    return ResponseEntity<object>.Success(null, "Review deleted successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to delete review.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductReviewsByUserCode()
        {
            try
            {
                long userCode = _currentUserService.User.user_code;

                var param = new DynamicParameters();
                param.Add("@user_id", userCode);

                string reviewSql = @"SELECT u.user_name,pr.product_code,p.name as product_name, rating, review_title, comment, is_verified_purchase, pr.is_publish,pr.is_delete,pr.created_at as review_datetime
                                FROM tbl_product_reviews as pr
                                INNER JOIN tbl_products as p on p.product_code=pr.product_code
                                INNER JOIN tbl_users as u on pr.user_id=u.user_code
                                WHERE pr.user_id = @user_id";

                var productReviews = await _productReviewsRepo.QueryDynamicAsync(reviewSql, param);

                if (productReviews != null)
                    return ResponseEntity<object>.Success(productReviews, "Product reviews retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No reviews found for user.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Product Wishlist

        public async Task<IActionResult> AddToWishlist(ProductWishlistDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");

                ProductWishlist model = new ProductWishlist()
                {
                    product_code = dto.product_code,
                    user_id = _currentUserService.User.user_code,
                };

                var validator = await _productWishlistRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductWishlistValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var response = await _productWishlistRepo.InsertAsync(model);

                if (Convert.ToInt64(response) > 0)
                    return ResponseEntity<object>.Success(null, "Wishlist Add Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add in Wishlist.");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> RemoveFromWishlist(ProductWishlistDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");

                var param = new DynamicParameters();
                param.Add("product_code", dto.product_code);
                param.Add("user_id", _currentUserService.User.user_code);

                ProductWishlist pwl = await _productWishlistRepo.GetSingleOrDefaultAsync("select * from tbl_product_wishlist where product_code=@product_code and user_id=@user_id", param);

                var response = await _productWishlistRepo.DeleteAsync(pwl.product_wishlist_id);

                if (Convert.ToInt32(response) > 0)
                    return ResponseEntity<object>.Success(null, "Wishlist Remove Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to remove from Wishlist.");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductWishlist(long user_code)
        {
            try
            {
                if (user_code <= 0)
                    return ResponseEntity<object>.Error(null, "passing data is missing or invalid");

                string query = @$"select pw.user_id,pw.product_wishlist_id,p.* from tbl_product_wishlist as pw
                                  inner join tbl_products as p on p.product_code=pw.product_code
                                  where user_id={user_code}";

                var productList = await _productWishlistRepo.QueryAsync<ProductMasterDto>(query);

                if (productList.Any())
                {
                    foreach (var product in productList)
                    {
                        query = $"select * from tbl_product_images where product_code={product.product_code} and is_primary = true";
                        var productImages = await _productImagesRepo.QueryDynamicAsync(query);

                        string reviewSql = $"SELECT rating FROM tbl_product_reviews as pr WHERE product_code = {product.product_code} AND is_publish = true";

                        var productReviews = await _productReviewsRepo.QueryAsync<ProductReviews>(reviewSql);

                        // 1. Ensure the variable receiving the result is a decimal
                        decimal avgRating = 0;

                        if (productReviews != null && productReviews.Any())
                        {
                            // 2. Parse the string property into a decimal inside Average
                            avgRating = Math.Ceiling(productReviews.Average(p => decimal.Parse(p.rating ?? "0")));
                        }

                        product.product_images = productImages.Select(img => new
                        {
                            image_url = img.image_url,
                            alt_text = img.alt_text,
                            display_order = img.display_order,
                            is_primary = img.is_primary
                        }).ToList();
                        product.total_review = productReviews.Count();
                        product.rating = avgRating.ToString();

                    }

                    return ResponseEntity<object>.Success(productList.Select(x => new
                    {
                        x.product_code,
                        x.name,
                        x.product_wishlist_id,
                        x.sku,
                        x.description,
                        x.base_price,
                        x.actual_price,
                        x.is_active,
                        x.is_new_arrival,
                        x.is_top_selling,
                        x.in_stock,
                        x.stock_quantity,
                        x.slug,
                        x.total_review,
                        x.rating,
                        x.product_images
                    }));

                }

                return ResponseEntity<object>.Success(null, "Wishlist is Empty.");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        #endregion
    }
}
