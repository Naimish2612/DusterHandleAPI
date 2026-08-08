using Dapper;
using Dapper.Contrib.Extensions;
using DUSTER.EComm.Data.CommonClass;
using DUSTER.EComm.Data.Helpers.Pagination;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Net;
using System.Reflection;
using static Dapper.SqlMapper;
using TableAttribute = Dapper.Contrib.Extensions.TableAttribute;

namespace DUSTER.EComm.Data
{
    public partial class EIPLRepository<T> : IEIPLRepository<T> where T : BaseEntity
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly ICurrentUserService _currentUserService;

        public EIPLRepository(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _connection = unitOfWork.Connection;
            _transaction = unitOfWork.Transaction;
            _currentUserService = currentUserService;
        }

        #region Insert Methods

        /// <summary>
        /// Inserts the entity dynamically based on key type and property attributes
        /// </summary>
        /// <param name="entity">it should be any model with BaseEntity type</param>
        /// <returns>specific model primery key.</returns>
        public async Task<object> InsertAsync(T entity)
        {
            try
            {
                // Get table name and valid properties
                var tableName = GetTableName();
                var type = typeof(T);
                var currentUser = _currentUserService.User;

                // Exclude properties marked with [NotMapped] or [Computed]
                var props = type.GetProperties()
                    .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null && p.GetCustomAttribute<ComputedAttribute>() == null)
                    .ToList();

                // Identify key and its behavior
                var primaryKey = GetPrimaryKeyProperty();

                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] defined on type {type.Name}");

                bool isExplicitKey = primaryKey?.GetCustomAttribute<ExplicitKeyAttribute>() != null; // user-defined key
                bool isAutoKey = primaryKey?.GetCustomAttribute<KeyAttribute>() != null && !isExplicitKey; // DB auto-generated key
                string keyName = primaryKey?.Name ?? "Id";

                // If model inherits from BaseEntity, set audit fields
                if (typeof(BaseEntity).IsAssignableFrom(type))
                    type.GetProperty("created_at")?.SetValue(entity, DateTime.Now);

                entity.created_by = currentUser.user_code.ToString();

                // Exclude primary key from insert if it's auto-generated
                var insertProps = props
                    .Where(p => isAutoKey ? p.Name != keyName : true)
                    .ToList();

                // Prepare SQL insert column names and values
                var columns = string.Join(", ", insertProps.Select(p => p.Name));

                //var parameters = string.Join(", ", insertProps.Select(p => "@" + p.Name));

                // NEW LOGIC: Dynamically append ::jsonb for JSON columns
                var parameters = string.Join(", ", insertProps.Select(p =>
                {
                    var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                    if (columnAttr != null && columnAttr.TypeName?.ToLower() == "jsonb")
                    {
                        return "@" + p.Name + "::jsonb"; // Appends the cast!
                    }
                    return "@" + p.Name; // Standard parameter
                }));

                // Build insert SQL with or without RETURNING based on key behavior
                var sql = isAutoKey
                    ? $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}) RETURNING {keyName};"
                    : $"INSERT INTO {tableName} ({columns}) VALUES ({parameters});";

                // Execute insert and handle RETURNING only if auto-key
                if (isAutoKey)
                {
                    var result = await _connection.ExecuteScalarAsync<object>(sql, entity);

                    //if you want to return the value into entity 
                    //if (result != null)
                    //    primaryKey?.SetValue(entity, Convert.ChangeType(result, primaryKey.PropertyType));

                    var dynamicType = primaryKey?.PropertyType;

                    // Insert audit log for insert
                    await InsertEntityHistoryAsync(tableName, result?.ToString() ?? string.Empty, "INSERT", null, entity);

                    return Convert.ChangeType(result, dynamicType);
                }
                else
                {
                    await _connection.ExecuteAsync(sql, entity);

                    await InsertEntityHistoryAsync(tableName, primaryKey?.GetValue(entity).ToString() ?? string.Empty, "INSERT", null, entity);

                    return primaryKey?.GetValue(entity);
                }
            }
            catch (Exception ex)
            {
                // print exception
                //return null;
                throw ex;
            }
        }

        public async Task<object> InsertMultipleAsync(IEnumerable<T> entities)
        {
            var ids = new List<object>();
            foreach (var entity in entities)
            {
                var id = await InsertAsync(entity);

                if (id != null)
                    ids.Add(id);
                else
                    throw new Exception($"Failed to insert entity of type {typeof(T).Name}");
            }
            return ids.Count;
        }

        #endregion

        #region Update Methods

        public async Task<int> UpdateAsync(T entity)
        {
            try
            {
                var type = typeof(T);
                var tableName = GetTableName(); // uses [Table("table_name")] or type.Name.ToLower()
                var currentUser = _currentUserService.User;

                // Get key info
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] defined on type {type.Name}");

                string keyName = primaryKey.Name;
                object keyValue = primaryKey.GetValue(entity);

                if (keyValue == null)
                    throw new Exception($"Primary key value cannot be null for update on {type.Name}");

                // Set audit field
                if (typeof(BaseEntity).IsAssignableFrom(type))
                    type.GetProperty("updated_at")?.SetValue(entity, DateTime.Now);

                entity.updated_by = currentUser.user_code.ToString();

                // Get properties excluding key, NotMapped, Computed
                var excludedFields = new[] { "created_at", "created_by" };
                var props = type.GetProperties()
                    .Where(p =>
                        p.Name != keyName &&
                        p.GetCustomAttribute<NotMappedAttribute>() == null &&
                        p.GetCustomAttribute<ComputedAttribute>() == null &&
                        !excludedFields.Contains(p.Name.ToLower()))
                    .ToList();

                if (!props.Any())
                    throw new Exception($"No updatable fields found on {type.Name}");

                //Dynamically append ::jsonb for JSON columns in the SET clause
                var setClause = string.Join(", ", props.Select(p =>
                {
                    var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                    if (columnAttr != null && columnAttr.TypeName?.ToLower() == "jsonb")
                    {
                        return $"{p.Name} = @{p.Name}::jsonb"; // Appends the cast!
                    }
                    return $"{p.Name} = @{p.Name}"; // Standard parameter
                }));

                var sql = $"UPDATE {tableName} SET {setClause} WHERE {keyName} = @{keyName};";

                var oldEntity = await _connection.QueryFirstOrDefaultAsync(
                    $"SELECT * FROM {tableName} WHERE {keyName} = @Id",
                    new { Id = keyValue },
                    _transaction);

                // Execute update
                var rowUpdate = await _connection.ExecuteAsync(sql, entity, _transaction);

                if (oldEntity != null)
                {
                    var history = await InsertEntityHistoryAsync(tableName, keyValue.ToString(), "UPDATE", oldEntity, entity);
                }

                return rowUpdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateAsync Error: {ex.Message}");
                throw ex;
            }
        }
        #endregion

        #region Delete Methods

        public async Task<int> DeleteAsync(object id)
        {
            try
            {
                var type = typeof(T);

                // Get table name
                var tableName = GetTableName();

                // Get primary key property
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] defined on type {type.Name}");

                var keyName = primaryKey.Name;

                // Prepare SQL
                var sql = $"DELETE FROM {tableName} WHERE {keyName} = @Id;";

                var oldEntity = await _connection.QueryFirstOrDefaultAsync($"SELECT * FROM {tableName} WHERE {keyName} = @Id", new { Id = id }, _transaction);

                // Execute the delete
                var deletedRow = await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);

                if (oldEntity != null)
                {
                    var history = await InsertEntityHistoryAsync(tableName, id.ToString(), "DELETE", oldEntity, null);
                }

                return deletedRow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteAsync Error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteAsync(object keyValue, Dictionary<string, object> additionalConditions = null)
        {
            try
            {
                var type = typeof(T);
                var tableName = GetTableName();

                // Identify primary key
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] defined for {type.Name}");

                string keyName = primaryKey.Name;

                // Build WHERE clause
                var whereConditions = new List<string> { $"{keyName} = @PrimaryKey" };
                var parameters = new DynamicParameters();
                parameters.Add("PrimaryKey", keyValue);

                // Add additional filters
                if (additionalConditions != null)
                {
                    foreach (var condition in additionalConditions)
                    {
                        whereConditions.Add($"{condition.Key} = @{condition.Key}");
                        parameters.Add(condition.Key, condition.Value);
                    }
                }

                // Compose SQL
                var whereClause = string.Join(" AND ", whereConditions);
                var sql = $"DELETE FROM {tableName} WHERE {whereClause};";

                var oldEntity = await _connection.QueryFirstOrDefaultAsync($"SELECT * FROM {tableName} WHERE {keyName} = @Id", new { Id = keyValue }, _transaction);

                // Execute
                var deletedRow = await _connection.ExecuteAsync(sql, parameters, _transaction);

                if (oldEntity != null)
                {
                    var history = await InsertEntityHistoryAsync(tableName, keyValue.ToString(), "DELETE", oldEntity, null);
                }

                return deletedRow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteAsync (multi-key) Error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteAsync(IEnumerable<object> keyValues, Dictionary<string, object> additionalConditions = null)
        {
            try
            {
                var type = typeof(T);
                var tableName = GetTableName();

                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] defined on type {type.Name}");

                string keyName = primaryKey.Name;



                // Determine array type and cast properly
                Array typedArray;
                var keyType = primaryKey.PropertyType;

                if (keyType == typeof(int))
                    typedArray = keyValues.Cast<int>().ToArray();
                else if (keyType == typeof(Guid))
                    typedArray = keyValues.Cast<Guid>().ToArray();
                else if (keyType == typeof(string))
                    typedArray = keyValues.Cast<string>().ToArray();
                else if (keyType == typeof(long))
                    typedArray = keyValues.Cast<long>().ToArray();
                else
                    throw new NotSupportedException($"Key type '{keyType.Name}' is not supported for IN clause");

                var sql = $"DELETE FROM {tableName} WHERE {keyName} = ANY(@PrimaryKeys)";

                // -- Build WHERE clause --
                var whereParts = new List<string> { $"{keyName} = ANY(@PrimaryKeys)" };
                var parameters = new DynamicParameters();

                // Add key array with correct PostgreSQL array type
                parameters.Add("PrimaryKeys", typedArray, (DbType)GetNpgsqlArrayType(keyType));

                // Add additional filters
                if (additionalConditions != null)
                {
                    foreach (var kvp in additionalConditions)
                    {
                        whereParts.Add($"{kvp.Key} = @{kvp.Key}");
                        parameters.Add(kvp.Key, kvp.Value);
                    }
                }

                sql = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereParts)};";

                // Execute
                return await _connection.ExecuteAsync(sql, parameters, _transaction);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteAsync IN-clause Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Select / Get Methods

        public async Task<T> GetByIdAsync(object id)
        {
            try
            {
                var type = typeof(T);
                var tableName = GetTableName();

                var primaryKey = GetPrimaryKeyProperty();

                if (primaryKey == null)
                    throw new Exception($"No [Key] or [ExplicitKey] found on {type.Name}");

                string keyName = primaryKey.Name;

                var sql = $"SELECT * FROM {tableName} WHERE {keyName} = @Id;";

                return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, _transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetByIdAsync Error: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> QueryDynamicAsync(string sql, DynamicParameters? param = null)
        {
            try
            {
                return await _connection.QueryAsync(sql, param, _transaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, DynamicParameters? param = null)
        {
            try
            {
                /*
                 var ids = new[] { 1, 2, 3 };
                var parameters = new DynamicParameters();
                parameters.Add("name", "Ravi");
                parameters.Add("active", true);
                parameters.Add("ids", ids); // Npgsql uses = ANY(@ids)
                
                string sql = "SELECT * FROM members WHERE id = ANY(@ids)";

                 */

                return await _connection.QueryAsync<TResult>(sql, param, _transaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string procName, object param)
        {
            return await _connection.QueryAsync<T>(procName, param, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<DropdownItem>> GetDropdownAsync(DropdownRequestModel model)
        {
            try
            {
                var type = typeof(T);
                var tableName = GetTableName();

                var sql = $"SELECT {model.table_columns} FROM {tableName} WHERE 1=1";

                var parameters = new DynamicParameters();

                if (model.StaticFilters != null)
                {
                    foreach (var filter in model.StaticFilters)
                    {
                        sql += $" and {filter.Key} = @{filter.Key}";
                        parameters.Add(filter.Key, filter.Value);
                    }
                }

                return await _connection.QueryAsync<DropdownItem>(sql, parameters, _transaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<T> GetSingleOrDefaultAsync(string sql, DynamicParameters? param = null)
        {
            try
            {
                return (await _connection.QueryAsync<T>(sql, param)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region Private Methods

        // Retrieves the table name from the model's [Table] attribute or defaults to lowercase class name
        private string GetTableName()
        {
            var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
            return tableAttr != null ? tableAttr.Name : typeof(T).Name.ToLower();
        }

        // Identifies the primary key property marked by either [ExplicitKey] or [Key]
        private PropertyInfo GetPrimaryKeyProperty()
        {
            var type = typeof(T);

            var explicitKey = type.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ExplicitKeyAttribute>() != null);

            if (explicitKey != null)
                return explicitKey;

            return type.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        }

        // Helper: Resolve Npgsql array type
        private NpgsqlDbType GetNpgsqlArrayType(Type type)
        {
            if (type == typeof(int)) return NpgsqlDbType.Array | NpgsqlDbType.Integer;
            if (type == typeof(Guid)) return NpgsqlDbType.Array | NpgsqlDbType.Uuid;
            if (type == typeof(string)) return NpgsqlDbType.Array | NpgsqlDbType.Text;
            if (type == typeof(long)) return NpgsqlDbType.Array | NpgsqlDbType.Bigint;

            throw new NotSupportedException($"Unsupported key type for NpgsqlDbType array mapping: {type.Name}");
        }

        private async Task<int> InsertEntityHistoryAsync(string tableName, string key, string action, object? beforeData, object? afterData)
        {
            try
            {
                var currentUser = _currentUserService.User;

                var history = new EntityHistory
                {
                    table_name = tableName,
                    primary_key_id = key,
                    change_type = action,
                    changed_by = currentUser.user_code.ToString(), // Replace with actual user context if available
                    changed_at = DateTime.Now,
                    data_before = beforeData != null ? JsonConvert.SerializeObject(beforeData) : string.Empty,
                    data_after = afterData != null ? JsonConvert.SerializeObject(afterData) : string.Empty,
                    created_at = DateTime.Now,
                    created_by = currentUser.user_code.ToString()
                };

                var sql = "INSERT INTO entity_history (table_name, primary_key_id, change_type, changed_by, changed_at, data_before, data_after,created_at,created_by) " +
                          "VALUES (@table_name, @primary_key_id, @change_type, @changed_by, @changed_at, @data_before, @data_after,@created_at,@created_by);";

                return await _connection.ExecuteAsync(sql, history, _transaction);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        #endregion

        #region Model Validation 

        public async Task<IActionResult> ModelValidating(ValidationModel validationModel)
        {
            try
            {
                if (validationModel.ValidateModel == null)
                {
                    return ResponseEntity<object>.Success(null);
                }

                ValidationResult result = IsValid(validationModel.ValidateModel, validationModel.Model);
                if (!result.IsValid)
                {
                    //var errors = result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    var errorMessages = result.Errors
                                        .Select(e => $"{HumanizeField(e.PropertyName)} : {e.ErrorMessage}")
                                        .ToList();
                    return ResponseEntity<object>.Error(errorMessages, "Validation failed", HttpStatusCode.InternalServerError);
                }

                return ResponseEntity<object>.Success(null);
            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, "Validation exception: " + ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private ValidationResult IsValid(IValidator validateModel, dynamic saveModel)
        {
            var context = new ValidationContext<object>(saveModel);
            return validateModel.Validate(context);
        }

        private static string HumanizeField(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            var clean = input.Replace("_", " ");
            var parts = System.Text.RegularExpressions.Regex
                .Replace(clean, "([a-z])([A-Z])", "$1 $2") // camelCase to space
                .Split(' ');

            return string.Join(" ", parts.Select(p => char.ToUpper(p[0]) + p[1..]));
        }

        #endregion

        #region Pagination
        public async Task<PaginatedResult<TResult>> QueryPagedAsync<TResult>(string baseSql,PaginationParams pageParams,DynamicParameters? param = null)
        {
            try
            {
                param ??= new DynamicParameters();
                string countSql = $"SELECT COUNT(1) FROM ({baseSql}) AS total_count_query";
                string paginationSql = $"{baseSql} LIMIT @PageSize OFFSET @Offset";

                int offset = (pageParams.PageNumber - 1) * pageParams.PageSize;
                param.Add("PageSize", pageParams.PageSize);
                param.Add("Offset", offset);
                string finalSql = $"{countSql}; {paginationSql};";
                using var multi = await _connection.QueryMultipleAsync(finalSql, param, _transaction);

                var totalCount = await multi.ReadFirstAsync<int>();
                var items = await multi.ReadAsync<TResult>();
                return new PaginatedResult<TResult>
                {
                    Data = items,
                    Metadata = new PaginationMetadata
                    {
                        TotalCount = totalCount,
                        PageSize = pageParams.PageSize,
                        CurrentPage = pageParams.PageNumber,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageParams.PageSize)
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
