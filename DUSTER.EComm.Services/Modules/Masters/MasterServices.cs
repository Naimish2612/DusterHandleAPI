using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Pagination;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.Masters
{
    public class MasterServices : IMasterServices
    {
        public readonly ICurrentUserService _currentUserService;
        public readonly IEIPLRepository<ProductCategory> _productCategoryRepo;
        public readonly IEIPLRepository<ProductSubCategory> _productSubCategoryRepo;
        public readonly IEIPLRepository<Manufacturer> _manufacturerRepo;
        public readonly IEIPLRepository<BannerMaster> _bannerMasterRepo;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        public readonly IEIPLRepository<ProductMaster> _productRepo;

        public MasterServices(ICurrentUserService currentUserService, IEIPLRepository<ProductCategory> productCategoryRepo, IEIPLRepository<ProductSubCategory> productSubCategoryRepo, IEIPLRepository<Manufacturer> manufacturerRepo,
            IEIPLRepository<BannerMaster> bannerMasterRepo, IOptions<CloudinarySettings> cloudinarySettings, IEIPLRepository<ProductMaster> productRepo)
        {
            _currentUserService = currentUserService;
            _productCategoryRepo = productCategoryRepo;
            _productSubCategoryRepo = productSubCategoryRepo;
            _manufacturerRepo = manufacturerRepo;
            _bannerMasterRepo = bannerMasterRepo;
            Account account = new Account(cloudinarySettings.Value.CloudName, cloudinarySettings.Value.ApiKey, cloudinarySettings.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _productRepo = productRepo;
        }

        #region Product Category

        public async Task<IActionResult> AddProductCategory(ProductCategory model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid product category data.");

                var validator = await _productCategoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductCategoryValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _productCategoryRepo.InsertAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Product Category Added Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while adding the product category.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditProductCategory(ProductCategory model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid product category data.");

                var validator = await _productCategoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductCategoryValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _productCategoryRepo.UpdateAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Product Category Update Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while update the product category.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllProductCategories()
        {
            try
            {
                var response = await _productCategoryRepo.QueryAsync<ProductCategory>("select * from tbl_product_category");

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(null, "Product Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Categories retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductCategoryById(int id)
        {
            try
            {
                if (id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid product category id.");

                var response = await _productCategoryRepo.GetByIdAsync(id);

                if (response == null)
                    return ResponseEntity<object>.Error(null, "Product Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Category retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductCategoriesDropdown()
        {
            try
            {
                var staticFilters = new Dictionary<string, object>();
                staticFilters.Add("is_active", true);

                var response = await _productCategoryRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_product_category", table_columns = "category_id as id, name as value", StaticFilters = staticFilters });

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(response, "Product Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Categories retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Product SubCategory

        public async Task<IActionResult> AddProductSubCategory(ProductSubCategory model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid product sub category data.");

                var validator = await _productSubCategoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductSubCategoryValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _productSubCategoryRepo.InsertAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Product Sub Category Added Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while adding the product sub category.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditProductSubCategory(ProductSubCategory model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid product sub category data.");

                var validator = await _productSubCategoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductSubCategoryValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _productSubCategoryRepo.UpdateAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Product Sub Category Update Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while update the product sub category.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllProductSubCategories(int categoryId)
        {
            try
            {
                if (categoryId == 0)
                    return ResponseEntity<object>.Error(null, "Invalid product category id.");

                var parameters = new DynamicParameters();
                parameters.Add("categoryId", categoryId);

                var response = await _productSubCategoryRepo.QueryAsync<ProductSubCategory>("select a.*,b.name as category_name from tbl_product_sub_category as a " +
                    "inner join tbl_product_category as b on a.category_id=b.category_id " +
                    "where a.category_id=@categoryId", parameters);

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(null, "Product Sub Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Sub Categories retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductSubCategoryById(int id)
        {
            try
            {
                if (id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid product sub category id.");
                var response = await _productSubCategoryRepo.GetByIdAsync(id);
                if (response == null)
                    return ResponseEntity<object>.Error(null, "Product Sub Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Sub Category retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetProductSubCategoriesDropdown(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid product category id.");

                var staticFilters = new Dictionary<string, object>();
                staticFilters.Add("category_id", categoryId);

                var response = await _productSubCategoryRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_product_sub_category", table_columns = "sub_category_id as id, name as value", StaticFilters = staticFilters });

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(response, "Product Sub Category Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product Sub Categories retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IActionResult> GetProductbySubCategoryId(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid product category id.");

                var staticFilters = new Dictionary<string, object>();
                staticFilters.Add("sub_category_id", categoryId);

                var response = await _productRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_products", table_columns = "product_code as id, name as value", StaticFilters = staticFilters });

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(response, "Product Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Product retrieved successfully.");

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Manufacturer

        public async Task<IActionResult> AddManufacturer(Manufacturer model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid manufacturer data.");

                var validator = await _manufacturerRepo.ModelValidating(new ValidationModel() { ValidateModel = new ManufacturerValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _manufacturerRepo.InsertAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Manufacturer Added Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while adding the manufacturer.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditManufacturer(Manufacturer model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid manufacturer data.");
                var validator = await _manufacturerRepo.ModelValidating(new ValidationModel() { ValidateModel = new ManufacturerValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;
                int response = Convert.ToInt32(await _manufacturerRepo.UpdateAsync(model));
                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Manufacturer Update Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while update the manufacturer.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllManufacturers()
        {
            try
            {
                var response = await _manufacturerRepo.QueryAsync<Manufacturer>("select * from tbl_manufacturer");
                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(null, "Manufacturer Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Manufacturers retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetManufacturerById(int id)
        {
            try
            {
                if (id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid manufacturer id.");
                var response = await _manufacturerRepo.GetByIdAsync(id);
                if (response == null)
                    return ResponseEntity<object>.Error(null, "Manufacturer Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Manufacturer retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetManufacturersDropdown()
        {
            try
            {
                var response = await _manufacturerRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_manufacturer", table_columns = "manufacturer_id as id, name as value" });

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(response, "Manufacturer Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Manufacturers retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetManufacturersWithFilter(string? search)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("Search", search);

                var response = await _manufacturerRepo.QueryAsync<DropdownItem>("select manufacturer_id as id, name as value from tbl_manufacturer where name like CONCAT('%',@Search,'%')", parameters);

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(null, "Manufacturer Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Manufacturers retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Banner Master

        public async Task<IActionResult> AddBanner(IFormCollection model)
        {
            try
            {
                if (model == null || !model.ContainsKey("data"))
                    return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

                if (model.Files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "At least 1 images are required.");

                var data = model["data"].ToString();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new SpaceDateTimeConverter());
                var bm = System.Text.Json.JsonSerializer.Deserialize<BannerMaster>(data, options);

                if (bm == null)
                    return ResponseEntity<object>.Error(null, "Failed to parse product data.");


                var validator = await _bannerMasterRepo.ModelValidating(new ValidationModel() { ValidateModel = new BannerMasterValidator(), Model = bm });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var imageData = await AddBannerImage(model.Files);

                if (imageData.image_public_id == null && imageData.image_url == null)
                    return ResponseEntity<object>.Error(null, "Banner Image Upload Error, Please Contact your Administrator.");

                bm.image_url = imageData.image_url;
                bm.image_public_id = imageData.image_public_id;

                long response = Convert.ToInt64(await _bannerMasterRepo.InsertAsync(bm));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Banner Added Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while adding the banner");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<(string? image_url, string? image_public_id)> AddBannerImage(IFormFileCollection files)
        {
            try
            {

                string[] array = { "jpg", "jpeg", "png" };
                IFormFile _file;
                _file = files[0];

                using var stream = _file.OpenReadStream();

                string fileExtension = Path.GetExtension(_file.FileName);

                string file_name = $"{StringHelper.GetUniqueString(10)}{Path.GetExtension(files[0].FileName)}";
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file_name, stream),
                    Folder = "EComm/banners",// the specific folder you want in Cloudinary
                    UseFilename = true,                   // use original filename
                    UniqueFilename = true,                // let Cloudinary add uniqueness
                    Overwrite = false,                    // do not overwrite existing
                                                          //ResourceType = ResourceType.Image
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK && uploadResult.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return (null, null);
                }

                // public url (https)
                var publicUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Uri?.ToString();

                return (publicUrl, uploadResult.PublicId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> EditBanner(IFormCollection model)
        {
            try
            {
                if (model == null || !model.ContainsKey("data"))
                    return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

                var data = model["data"].ToString();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new SpaceDateTimeConverter());
                var bm = System.Text.Json.JsonSerializer.Deserialize<BannerMaster>(data, options);

                if (bm == null)
                    return ResponseEntity<object>.Error(null, "Failed to parse product data.");

                var validator = await _bannerMasterRepo.ModelValidating(new ValidationModel() { ValidateModel = new BannerMasterValidator(), Model = bm });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                if (model.Files.Count > 0)
                {
                    var imageData = await AddBannerImage(model.Files);

                    if (string.IsNullOrEmpty(imageData.image_public_id) && string.IsNullOrEmpty(imageData.image_url))
                        return ResponseEntity<object>.Error(null, "Banner Image Upload Error, Please Contact your Administrator.");

                    bm.image_url = imageData.image_url;
                    bm.image_public_id = imageData.image_public_id;
                }

                BannerMaster bannerMaster = await _bannerMasterRepo.GetByIdAsync(bm.banner_id);

                if (bannerMaster == null)
                    return ResponseEntity<object>.Error(null, "Banner not found for update");

                bannerMaster.banner_title = bm.banner_title;
                bannerMaster.description = bm.description;
                bannerMaster.start_date = bm.start_date;
                bannerMaster.redirect_url = bm.redirect_url;
                bannerMaster.end_date = bm.end_date;
                bannerMaster.is_active = bm.is_active;
                bannerMaster.is_default = bm.is_default;
                bannerMaster.platform = bm.platform;
                bannerMaster.state_id = bm.state_id;
                bannerMaster.display_order = bm.display_order;
                if (model.Files.Count > 0)
                {
                    string[] oldPublicId = { bannerMaster.image_public_id };
                    var deleteParams = new DelResParams()
                    {
                        PublicIds = new List<string> { string.Join(",", oldPublicId) },
                        Type = "upload",
                        ResourceType = ResourceType.Image
                    };

                    var result = _cloudinary.DeleteResources(deleteParams);

                    bannerMaster.image_public_id = bm.image_public_id;
                    bannerMaster.image_url = bm.image_url;

                }

                long response = Convert.ToInt64(await _bannerMasterRepo.UpdateAsync(bannerMaster));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Banner Edit Successfully.");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while update the banner");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> DeleteBanner(long bannerId)
        {
            try
            {
                if (bannerId <= 0)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.");

                BannerMaster bannerMaster = await _bannerMasterRepo.GetByIdAsync(bannerId);

                if (bannerMaster == null)
                    return ResponseEntity<object>.Error(null, "Banner Not found for remove.");

                string[] imagePublicId = { bannerMaster.image_public_id };

                var deleteParams = new DelResParams()
                {
                    PublicIds = new List<string> { string.Join(",", imagePublicId) },
                    Type = "upload",
                    ResourceType = ResourceType.Image
                };

                var result = _cloudinary.DeleteResources(deleteParams);

                var deleteBanner = await _bannerMasterRepo.DeleteAsync(bannerMaster.banner_id);

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    return ResponseEntity<object>.Error(null, "Failed to delete images from Server.");

                return ResponseEntity<object>.Success(null, "Banner Remove Successfully.");

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<IActionResult> GetActiveBanners(int? stateId, string? platform = "Both")
        {
            try
            {
                // SQL Logic:
                // 1. Filter by Activity and Date Range
                // 2. Filter by Platform (Specific or 'Both')
                // 3. Filter by State (User's state OR Pan India state_id=0)
                // 4. Fallback: If no results, get is_default = true

                string sql = @"
                    SELECT * FROM tbl_banners 
                    WHERE is_active = true 
                    AND CURRENT_TIMESTAMP BETWEEN start_date AND end_date
                    AND (platform = @Platform OR platform = 'Both')
                    AND (state_id = @StateId OR state_id = 0)
                    ORDER BY display_order ASC";

                var parameters = new DynamicParameters();
                parameters.Add("StateId", stateId.Value);
                parameters.Add("Platform", platform);

                var banners = await _bannerMasterRepo.QueryAsync<BannerMaster>(sql, parameters);

                // Default Fallback Logic
                if (banners == null || !banners.Any())
                {
                    string defaultSql = @"
                        SELECT * FROM tbl_banners 
                        WHERE is_active = true 
                        AND is_default = true
                        AND (platform = @Platform OR platform = 'Both')
                        ORDER BY display_order ASC";

                    banners = await _bannerMasterRepo.QueryAsync<BannerMaster>(defaultSql, parameters);

                    if (banners == null || !banners.Any() || !banners.Any())
                    {
                        return ResponseEntity<object>.Error(null, "Banner not available");
                    }
                }

                return ResponseEntity<object>.Success(banners.Select(x => new
                {
                    x.banner_title,
                    x.display_order,
                    x.description,
                    x.image_url
                }));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllBanners(BannerFilters filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                string sql = "SELECT * FROM tbl_banners WHERE 1=1";

                if (filter.start_date.HasValue && filter.end_date.HasValue)
                {
                    sql += " AND (start_date <= @EndDateFilter AND end_date >= @StartDateFilter)";
                    parameters.Add("StartDateFilter", filter.start_date.Value);
                    parameters.Add("EndDateFilter", filter.end_date.Value);
                }
                else if (filter.start_date.HasValue)
                {
                    sql += " AND end_date >= @StartDateFilter";
                    parameters.Add("StartDateFilter", filter.start_date.Value);
                }
                else if (filter.end_date.HasValue)
                {
                    sql += " AND start_date <= @EndDateFilter";
                    parameters.Add("EndDateFilter", filter.end_date.Value);
                }

                if (filter.is_active.HasValue)
                {
                    sql += " AND is_active = @IsActive";
                    parameters.Add("IsActive", filter.is_active.Value);
                }

                if (filter.state_id.HasValue)
                {
                    sql += " AND state_id = @StateId";
                    parameters.Add("StateId", filter.state_id.Value);
                }

                if (!string.IsNullOrEmpty(filter.platform))
                {
                    sql += " AND platform = @platform";
                    parameters.Add("platform", filter.platform);

                }
                sql += " ORDER BY created_at DESC";

                var banners = await _bannerMasterRepo.QueryAsync<BannerMaster>(sql, parameters);
                //var banners = await _bannerMasterRepo.QueryPagedAsync<BannerMaster>(sql, filter, parameters);

                //if (banners.Metadata.TotalCount > 0)
                if (banners.Count() > 0)
                    return ResponseEntity<object>.Success(banners);
                else
                    return ResponseEntity<object>.Error(null, "Banner not available.");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #region pagination static data
        //public async Task<IActionResult> GetAllBanners(BannerFilters filter)
        //{
        //    try
        //    {
        //        var mockBanners = Enumerable.Range(1, 100).Select(i => new BannerMaster
        //        {
        //            banner_id = i,
        //            banner_title = $"Test Banner {i}",
        //            platform = i % 2 == 0 ? "Web" : "Mobile",
        //            is_active = i % 3 != 0, // Mix of active and inactive
        //            state_id = (i % 5) + 1,   // States from 1 to 5
        //            created_at = DateTime.Now.AddDays(-i),
        //            start_date = DateTime.Now.AddDays(-10),
        //            end_date = DateTime.Now.AddDays(10)
        //        }).AsQueryable();

        //        if (filter.start_date.HasValue && filter.end_date.HasValue)
        //        {
        //            mockBanners = mockBanners.Where(b => b.start_date <= filter.end_date.Value && b.end_date >= filter.start_date.Value);
        //        }
        //        if (filter.is_active.HasValue)
        //        {
        //            mockBanners = mockBanners.Where(b => b.is_active == filter.is_active.Value);
        //        }
        //        if (filter.state_id.HasValue)
        //        {
        //            mockBanners = mockBanners.Where(b => b.state_id == filter.state_id.Value);
        //        }
        //        if (!string.IsNullOrEmpty(filter.platform))
        //        {
        //            mockBanners = mockBanners.Where(b => b.platform.Equals(filter.platform, StringComparison.OrdinalIgnoreCase));
        //        }

        //        var filteredList = mockBanners.OrderByDescending(b => b.created_at).ToList();

        //        int totalCount = filteredList.Count;
        //        int offset = (filter.PageNumber - 1) * filter.PageSize;

        //        var pagedData = filteredList
        //            .Skip(offset)
        //            .Take(filter.PageSize)
        //            .ToList();

        //        var pagedResult = new PaginatedResult<BannerMaster>
        //        {
        //            Data = pagedData,
        //            Metadata = new PaginationMetadata
        //            {
        //                TotalCount = totalCount,
        //                PageSize = filter.PageSize,
        //                CurrentPage = filter.PageNumber,
        //                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        //            }
        //        };

        //        if (pagedResult.Metadata.TotalCount > 0)
        //            return ResponseEntity<object>.Success(pagedResult, "Banners retrieved successfully.");
        //        else
        //            return ResponseEntity<object>.Error(null, "Banner not available.");
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}
        #endregion

        public async Task<IActionResult> GetBannerByBannerId(long banner_id)
        {
            try
            {
                if (banner_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid banner id.");
                var response = await _bannerMasterRepo.GetByIdAsync(banner_id);
                if (response == null)
                    return ResponseEntity<object>.Error(null, "Banner Not Available.");
                else
                    return ResponseEntity<object>.Success(response, "Banner retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }   
        }

        public async Task<IActionResult> ToggleActication(long banner_id)
        {
            try
            {
                if (banner_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid banner id.");
                var response = await _bannerMasterRepo.GetByIdAsync(banner_id);
                if (response == null)
                    return ResponseEntity<object>.Error(null, "Banner Not Available.");

                response.is_active = !response.is_active;
                await _bannerMasterRepo.UpdateAsync(response);
                string message = response.is_active ? "active" : "in-active";
                return ResponseEntity<object>.Success("",$"Banner:{response.banner_title} is now {message}");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        private class SpaceDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
        {
            private const string Format = "yyyy-MM-dd HH:mm:ss";
            public override DateTime Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) =>
                DateTime.ParseExact(r.GetString()!, Format, System.Globalization.CultureInfo.InvariantCulture);

            public override void Write(Utf8JsonWriter w, DateTime v, JsonSerializerOptions o) =>
                w.WriteStringValue(v.ToString(Format, System.Globalization.CultureInfo.InvariantCulture));
        }

    }
}
