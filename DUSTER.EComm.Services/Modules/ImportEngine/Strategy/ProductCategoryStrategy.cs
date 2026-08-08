using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DUSTER.EComm.Services.Modules.ImportEngine.Strategy
{
    public class ProductCategoryImportStrategy : IImportStrategy<ProductCategory>
    {
        public ProductCategory Map(Dictionary<string, string> row)
        {
            return new ProductCategory
            {
                name = row.GetValueOrDefault("Category Name", "").Trim(),
                slug = row.GetValueOrDefault("Slug", "").Trim(),
                description = row.GetValueOrDefault("Description", "").Trim(),
                is_active = ParseBoolean(row.GetValueOrDefault("Is Active", "true"))
            };
        }

        private bool ParseBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            value = value.Trim().ToLower();
            return value == "true" || value == "1" || value == "yes";
        }
    }

    //The Handler (Binds Strategy, Validator, and Engine together)
    public class ProductCategoryImportHandler : BaseImportHandler<ProductCategory>
    {
        // This key matches the frontend dropdown selection
        public override string EntityType => EnumImportProcess.PRODUCT_CATEGORY.ToString();

        //protected override IImportStrategy<ProductCategory> GetStrategy()
        //    => new ProductCategoryImportStrategy();

        protected override Task<IImportStrategy<ProductCategory>> GetStrategyAsync(IServiceScope scope)
        {
            // Simple return. No database pre-fetching needed!
            return Task.FromResult<IImportStrategy<ProductCategory>>(new ProductCategoryImportStrategy());
        }

        protected override AbstractValidator<ProductCategory> GetValidator()
            => new ProductCategoryValidator();
    }
}
