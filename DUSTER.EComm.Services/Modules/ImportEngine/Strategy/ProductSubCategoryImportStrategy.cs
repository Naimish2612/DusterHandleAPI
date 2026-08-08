using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DUSTER.EComm.Services.Modules.ImportEngine.Strategy
{
    public class ProductSubCategoryImportStrategy : IImportStrategy<ProductSubCategory>
    {
        private readonly Dictionary<string, int> _categoryLookup;

        public ProductSubCategoryImportStrategy(Dictionary<string, int> categoryLookup)
        {
            _categoryLookup = categoryLookup;
        }

        public ProductSubCategory Map(Dictionary<string, string> row)
        {
            string categoryName = row.GetValueOrDefault("Category Name", "").Trim();

            //Foreign Key Resolution
            if (!_categoryLookup.TryGetValue(categoryName.ToLower(), out int categoryId))
            {
                // If it fails, throw error. The Generic Engine will catch this and put it in the Error CSV!
                throw new Exception($"Category Name '{categoryName}' does not exist in the system.");
            }

            //Normal Mapping
            return new ProductSubCategory
            {
                category_id = categoryId, // Mapped from the DB lookup!
                name = row.GetValueOrDefault("Sub Category Name", "").Trim(),
                slug = row.GetValueOrDefault("Slug", "").Trim(),
            };
        }
    }

    public class ProductSubCategoryImportHandler : BaseImportHandler<ProductSubCategory>
    {
        public override string EntityType => EnumImportProcess.PRODUCT_SUB_CATEGORY.ToString();

        protected override async Task<IImportStrategy<ProductSubCategory>> GetStrategyAsync(IServiceScope scope)
        {
            // Get the repository for the PARENT table (ProductCategory)
            var categoryRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<ProductCategory>>();

            //Fetch all active categories from the database
            var categories = await categoryRepo.QueryAsync<ProductCategory>("SELECT category_id, name FROM tbl_product_category WHERE is_active = true");

            //Build the Dictionary (Key = Name in lowercase to avoid case-sensitivity issues, Value = ID)
            var lookupMap = new Dictionary<string, int>();
            foreach (var cat in categories)
            {
                if (!string.IsNullOrWhiteSpace(cat.name))
                {
                    lookupMap[cat.name.ToLower()] = cat.category_id;
                }
            }

            //Pass the Dictionary into the Strategy
            return new ProductSubCategoryImportStrategy(lookupMap);
        }

        protected override AbstractValidator<ProductSubCategory> GetValidator()
            => new ProductSubCategoryValidator();
    }
}
