using Dapper;
using DUSTER.EComm.Data.CommonClass;
using DUSTER.EComm.Data.Helpers.Pagination;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.Data
{
    public partial interface IEIPLRepository<T> where T : BaseEntity
    {
        Task<object> InsertAsync(T entity);
        Task<object> InsertMultipleAsync(IEnumerable<T> entities);
        Task<int> UpdateAsync(T entity);
        /// <summary>
        /// Delete with only primary key; Calling => await repo.DeleteAsync(101);
        /// </summary>
        /// <param name="id">entity PrimaryKey</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(object id);

        /// <summary>
        /// Delete Multiple by Primary Key Only
        /// await repo.DeleteAsync(101);
        /// Delete Multiple by Key + Filters
        /// await repo.DeleteAsync(101,new Dictionary<string, object>
        ///                              {
        ///                                 { "company_id", 1 },
        ///                                 { "is_active", false }
        ///                              });
        /// </summary>
        /// <param name="keyValue">entity primary key</param>
        /// <param name="additionalConditions">additional keys</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(object keyValue, Dictionary<string, object> additionalConditions = null);

        /// <summary>
        /// Delete Multiple by Primary Key Only
        /// await repo.DeleteAsync(new object[] { 101, 102, 103 });
        /// Delete Multiple by Key + Filters
        /// await repo.DeleteAsync(
        ///    new object[] { 201, 202 },new Dictionary<string, object>
        ///                              {
        ///                                 { "company_id", 1 },
        ///                                 { "is_active", false }
        ///                              });
        /// </summary>
        /// <param name="keyValue">entity primary key</param>
        /// <param name="additionalConditions">additional keys</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(IEnumerable<object> keyValues, Dictionary<string, object> additionalConditions = null);
        Task<T> GetByIdAsync(object id);
        Task<IEnumerable<dynamic>> QueryDynamicAsync(string sql, DynamicParameters? param = null);
        Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, DynamicParameters? param = null);
        Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string procName, object param);
        Task<IEnumerable<DropdownItem>> GetDropdownAsync(DropdownRequestModel request);
        Task<IActionResult> ModelValidating(ValidationModel validationModel);

        Task<T> GetSingleOrDefaultAsync(string sql, DynamicParameters? param = null);
        //pagination
        Task<PaginatedResult<TResult>> QueryPagedAsync<TResult>(string baseSql,PaginationParams pageParams,DynamicParameters? param = null);
    }
}
