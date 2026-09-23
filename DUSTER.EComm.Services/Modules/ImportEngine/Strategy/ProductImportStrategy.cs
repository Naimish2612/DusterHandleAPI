using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using DUSTER.EComm.Services.Modules.Masters.Models;
using DUSTER.EComm.Services.Modules.Tax.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DUSTER.EComm.Services.Modules.ImportEngine.Strategy
{
    public class ProductImportStrategy : IImportStrategy<ProductMaster>
    {
        private readonly Dictionary<string, int> _categoryLookup;
        private readonly Dictionary<string, int> _subCategoryLookup;
        private readonly Dictionary<string, int> _manufacturerLookup;
        private readonly Dictionary<string, int> _taxClassLookup;

        public ProductImportStrategy(
            Dictionary<string, int> categoryLookup,
            Dictionary<string, int> subCategoryLookup,
            Dictionary<string, int> manufacturerLookup,
            Dictionary<string, int> taxClassLookup)
        {
            _categoryLookup = categoryLookup;
            _subCategoryLookup = subCategoryLookup;
            _manufacturerLookup = manufacturerLookup;
            _taxClassLookup = taxClassLookup;
        }

        public ProductMaster Map(Dictionary<string, string> row)
        {
            string categoryName = GetValue(row, "Category Name", "Category");
            string subCategoryName = GetValue(row, "Sub Category Name", "SubCategory", "Sub Category");
            string manufacturerName = GetValue(row, "Manufacturer Name", "Manufacturer");
            string taxClassName = GetValue(row, "Tax Class Name", "Tax Class", "TaxClass");

            // Foreign Key Resolution: Category
            if (string.IsNullOrWhiteSpace(categoryName) || !_categoryLookup.TryGetValue(categoryName.Trim().ToLower(), out int categoryId))
            {
                throw new Exception($"Category Name '{categoryName}' does not exist in the system.");
            }

            // Foreign Key Resolution: Sub Category (Checks composite key categoryId:subCategoryName first, then fallback to subCategoryName)
            int subCategoryId = 0;
            string subCategoryKey = $"{categoryId}:{subCategoryName.Trim().ToLower()}";
            if (string.IsNullOrWhiteSpace(subCategoryName) ||
                (!_subCategoryLookup.TryGetValue(subCategoryKey, out subCategoryId) &&
                 !_subCategoryLookup.TryGetValue(subCategoryName.Trim().ToLower(), out subCategoryId)))
            {
                throw new Exception($"Sub Category Name '{subCategoryName}' does not exist in the system (or under Category '{categoryName}').");
            }

            // Foreign Key Resolution: Manufacturer
            if (string.IsNullOrWhiteSpace(manufacturerName) || !_manufacturerLookup.TryGetValue(manufacturerName.Trim().ToLower(), out int manufacturerId))
            {
                throw new Exception($"Manufacturer Name '{manufacturerName}' does not exist in the system.");
            }

            // Foreign Key Resolution: Tax Class
            if (string.IsNullOrWhiteSpace(taxClassName) || !_taxClassLookup.TryGetValue(taxClassName.Trim().ToLower(), out int taxClassId))
            {
                throw new Exception($"Tax Class Name '{taxClassName}' does not exist in the system.");
            }

            decimal basePrice = ParseDecimal(GetValue(row, "Base Price", "BasePrice", "Price"), 0);
            decimal actualPrice = ParseDecimal(GetValue(row, "Actual Price", "ActualPrice", "MRP"), basePrice);
            if (actualPrice < basePrice)
            {
                actualPrice = basePrice;
            }

            int stockQuantity = ParseInt(GetValue(row, "Stock Quantity", "StockQuantity", "Quantity"), 0);
            bool inStock = ParseBoolean(GetValue(row, "In Stock", "InStock"), defaultValue: stockQuantity > 0);
            bool isActive = ParseBoolean(GetValue(row, "Is Active", "IsActive"), defaultValue: true);
            bool isTopSelling = ParseBoolean(GetValue(row, "Is Top Selling", "IsTopSelling"), defaultValue: false);
            bool isNewArrival = ParseBoolean(GetValue(row, "Is New Arrival", "IsNewArrival"), defaultValue: false);
            int estimatedDeliveryDays = ParseInt(GetValue(row, "Estimated Delivery Days", "EstimatedDeliveryDays", "Delivery Days"), 0);

            string attributes = GetValue(row, "Attributes", "Attribute");
            if (string.IsNullOrWhiteSpace(attributes))
            {
                attributes = "{}";
            }

            string productName = GetValue(row, "Product Name", "Name", "Title");
            string sku = GetValue(row, "SKU", "Sku", "Product SKU");
            if (string.IsNullOrWhiteSpace(sku))
            {
                sku = GenerateSku(productName);
            }

            string slug = GetValue(row, "Slug", "URL Slug");
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = GenerateSlug(productName);
            }

            return new ProductMaster
            {
                name = productName,
                sku = sku,
                slug = slug,
                description = GetValue(row, "Description", "Desc"),
                base_price = basePrice,
                actual_price = actualPrice,
                category_id = categoryId,
                sub_category_id = subCategoryId,
                manufacturer_id = manufacturerId,
                tax_class_id = taxClassId,
                stock_quantity = stockQuantity,
                in_stock = inStock,
                is_active = isActive,
                is_top_selling = isTopSelling,
                is_new_arrival = isNewArrival,
                estimated_delivery_days = estimatedDeliveryDays,
                sap_sku_code = GetValue(row, "SAP SKU Code", "SAP SKU", "SapSkuCode"),
                attributes = attributes
            };
        }

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string slug = name.ToLowerInvariant().Trim();
            slug = Regex.Replace(slug, @"[^\w\s-]", "");
            slug = Regex.Replace(slug, @"[\s_-]+", "-");
            slug = Regex.Replace(slug, @"^-+|-+$", "");
            return slug;
        }

        private string GenerateSku(string name)
        {
            string prefix = "";
            if (!string.IsNullOrWhiteSpace(name))
            {
                string upper = name.ToUpperInvariant();
                string cleaned = Regex.Replace(upper, @"[AEIOU\s]", "");
                prefix = cleaned.Length > 6 ? cleaned.Substring(0, 6) : cleaned;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "PRD";
            }

            int randomNum = Random.Shared.Next(1000, 10000);
            return $"{prefix}-{randomNum}";
        }

        private string GetValue(Dictionary<string, string> row, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val.Trim();
                }
            }
            return string.Empty;
        }

        private decimal ParseDecimal(string value, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            if (decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        private int ParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            if (int.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        private bool ParseBoolean(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            value = value.Trim().ToLower();
            return value == "true" || value == "1" || value == "yes";
        }
    }

    public class ProductImportHandler : BaseImportHandler<ProductMaster>
    {
        public override string EntityType => EnumImportProcess.PRODUCT.ToString();

        protected override async Task<IImportStrategy<ProductMaster>> GetStrategyAsync(IServiceScope scope)
        {
            var categoryRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<ProductCategory>>();
            var subCategoryRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<ProductSubCategory>>();
            var manufacturerRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<Manufacturer>>();
            var taxClassRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<TaxClass>>();

            // Fetch lookup entities from DB
            var categories = await categoryRepo.QueryAsync<ProductCategory>("SELECT category_id, name FROM tbl_product_category WHERE is_active = true");
            var subCategories = await subCategoryRepo.QueryAsync<ProductSubCategory>("SELECT sub_category_id, category_id, name FROM tbl_product_sub_category");
            var manufacturers = await manufacturerRepo.QueryAsync<Manufacturer>("SELECT manufacturer_id, name FROM tbl_manufacturer");
            var taxClasses = await taxClassRepo.QueryAsync<TaxClass>("SELECT class_id, class_name FROM tbl_tax_classes WHERE is_active = true");

            // Category Lookup Map
            var categoryLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categories)
            {
                if (!string.IsNullOrWhiteSpace(cat.name))
                {
                    categoryLookup[cat.name.Trim().ToLower()] = cat.category_id;
                }
            }

            // SubCategory Lookup Map (composite key & direct name)
            var subCategoryLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var sub in subCategories)
            {
                if (!string.IsNullOrWhiteSpace(sub.name))
                {
                    string subNameLower = sub.name.Trim().ToLower();
                    subCategoryLookup[$"{sub.category_id}:{subNameLower}"] = sub.sub_category_id;
                    subCategoryLookup[subNameLower] = sub.sub_category_id;
                }
            }

            // Manufacturer Lookup Map
            var manufacturerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var mfg in manufacturers)
            {
                if (!string.IsNullOrWhiteSpace(mfg.name))
                {
                    manufacturerLookup[mfg.name.Trim().ToLower()] = mfg.manufacturer_id;
                }
            }

            // Tax Class Lookup Map
            var taxClassLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var tc in taxClasses)
            {
                if (!string.IsNullOrWhiteSpace(tc.class_name))
                {
                    taxClassLookup[tc.class_name.Trim().ToLower()] = tc.class_id;
                }
            }

            return new ProductImportStrategy(categoryLookup, subCategoryLookup, manufacturerLookup, taxClassLookup);
        }

        protected override AbstractValidator<ProductMaster> GetValidator()
            => new ProductMasterValidator();
    }
}
