using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine.Models;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.RightsMasters.Models;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace DUSTER.EComm.Services.Modules.RightsMasters
{
    public class RightsMasterServices : IRightsMasterServices
    {

        public readonly ICurrentUserService _currentUserService;
        IEIPLRepository<RoleModel> _roleRepo;
        IEIPLRepository<PermissionModel> _permissionRepo;
        IEIPLRepository<ActionModel> _actionRepo;
        IEIPLRepository<PermissionActionMapping> _permissionActionMappingRepo;
        IEIPLRepository<RolePermissionMapping> _rolePermissionMappingRepo;
        IEIPLRepository<UserRoleMapping> _userRoleMappingRepo;
        public RightsMasterServices(IEIPLRepository<PermissionModel> permissionRepo, IEIPLRepository<ActionModel> actionRepo, IEIPLRepository<PermissionActionMapping> permissionActionMappingRepo,
            IEIPLRepository<RolePermissionMapping> rolePermissionMappingRepo, IEIPLRepository<RoleModel> roleRepo, IEIPLRepository<UserRoleMapping> userRoleMappiCngRepo,
            ICurrentUserService currentUserService, IEIPLRepository<UserRoleMapping> userRoleMappingRepo)
        {
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
            _actionRepo = actionRepo;
            _permissionActionMappingRepo = permissionActionMappingRepo;
            _rolePermissionMappingRepo = rolePermissionMappingRepo;
            _currentUserService = currentUserService;
            _userRoleMappingRepo = userRoleMappingRepo;
        }

        #region Role

        public async Task<IActionResult> GetRolesAsync()
        {
            var response = await _roleRepo.QueryAsync<RoleModel>("SELECT * FROM tbl_role");

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "Role Not Available.");
            else
                return ResponseEntity<object>.Success(response, "Roles retrieved successfully.");
        }

        public async Task<IActionResult> GetRoleByIdAsync(int roleCode)
        {
            var response = await _roleRepo.GetByIdAsync(roleCode);

            if (response == null)
                return ResponseEntity<object>.Error(null, "Role not found.");
            else
                return ResponseEntity<object>.Success(response, "Role retrieved successfully.");
        }

        public async Task<ResponseEntity<object>> CreateRoleAsync(RoleModel role)
        {
            var validator = await _roleRepo.ModelValidating(new ValidationModel() { ValidateModel = new RoleModelValidator(), Model = role });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            int response = Convert.ToInt32(await _roleRepo.InsertAsync(role));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Role created successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the role.");
        }

        public async Task<ResponseEntity<object>> UpdateRoleAsync(RoleModel role)
        {
            var validator = await _roleRepo.ModelValidating(new ValidationModel() { ValidateModel = new RoleModelValidator(), Model = role });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            int response = Convert.ToInt32(await _roleRepo.UpdateAsync(role));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Role updated successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while updating the role.");
        }

        public async Task<ResponseEntity<object>> DeleteRoleAsync(int roleCode)
        {
            int deleteResponse = Convert.ToInt32(await _roleRepo.DeleteAsync(roleCode));

            if (deleteResponse > 0)
                return ResponseEntity<object>.Success(null, "Role deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the role.");
        }

        #endregion

        #region Permission

        public async Task<IActionResult> GetPermissionsAsync()
        {
            var response = await _permissionRepo.QueryAsync<PermissionModel>("SELECT * FROM tbl_permission");

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "No permissions found.");
            else
                return ResponseEntity<object>.Success(response, "Permissions retrieved successfully.");
        }

        public async Task<IActionResult> GetPermissionByIdAsync(long permissionId)
        {
            var response = await _permissionRepo.GetByIdAsync(permissionId);

            if (response == null)
                return ResponseEntity<object>.Error(null, "Permission not found.");
            else
                return ResponseEntity<object>.Success(response, "Permission retrieved successfully.");
        }

        public async Task<ResponseEntity<object>> CreatePermissionAsync(PermissionModel permission)
        {
            var validator = await _permissionRepo.ModelValidating(new ValidationModel() { ValidateModel = new PermissionModelValidator(), Model = permission });
            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;
            Int64 response = Convert.ToInt64(await _permissionRepo.InsertAsync(permission));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Permission created successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the permission.");
        }

        public async Task<ResponseEntity<object>> UpdatePermissionAsync(PermissionModel permission)
        {
            var validator = await _permissionRepo.ModelValidating(new ValidationModel() { ValidateModel = new PermissionModelValidator(), Model = permission });
            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;
            int response = await _permissionRepo.UpdateAsync(permission);

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Permission updated successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while updating the permission.");
        }

        public async Task<ResponseEntity<object>> DeletePermissionAsync(long permissionId)
        {
            int response = await _permissionRepo.DeleteAsync(permissionId);

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Permission deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the permission.");
        }

        public async Task<ResponseEntity<object>> PermissionDropdown()
        {
            try
            {
                IEnumerable<DropdownItem> data = await _permissionRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_permission", table_columns = "permission_code as id,permission_name as value" });

                if (data.Any())
                    return ResponseEntity<object>.Success(data.ToList(), "Success");
                else
                    return ResponseEntity<object>.Success(null, "Permission not available.");
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<ResponseEntity<object>> GetUserPermission()
        {
            try
            {
                CurrentUser currentUser = _currentUserService.User;

                var userRoleParam = new DynamicParameters();
                userRoleParam.Add("user_code", currentUser.user_code);

                List<UserRoleMapping> roles = (await _userRoleMappingRepo.QueryAsync<UserRoleMapping>(@"select * from tbl_user_role_mapping where user_code=@user_code", userRoleParam)).ToList();

                if (!roles.Any())
                    return ResponseEntity<object>.Error(null, "Role is not mapped.", HttpStatusCode.InternalServerError);

                var actionParam = new DynamicParameters();
                actionParam.Add("role_code", roles.Select(x => x.role_code).ToArray());

                List<ActionModel> actionModels = (await _actionRepo.QueryAsync<ActionModel>(@"
                            select distinct a.* 
                            from tbl_actions as a
                            inner join tbl_permission_action_mapping as b on b.action_code = a.action_code
                            inner join tbl_role_permission_mapping as c on b.permission_code = c.permission_code 
    
                            inner join tbl_role as r on r.role_code = c.role_code
    
                            inner join tbl_permission as p on p.permission_code = b.permission_code
    
                            where c.role_code = ANY(@role_code)
                              and r.is_active = true 
                              and r.is_block = false
      
                              and p.is_active = true 
                              and p.is_block = false", actionParam)).ToList();


                UserPermissionDTO userPermissionDTO = new UserPermissionDTO();

                if (actionModels.Count > 0)
                {
                    // 1. Filter usable actions
                    var validActions = actionModels
                        .Where(x => x.is_active && x.action_special_name == "#" && !x.is_block)
                        .ToList();

                    // 2. Group by parent_code for fast lookup
                    var lookup = validActions
                        .GroupBy(x => x.parent_code)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // 3. Recursive tree builder
                    List<SidebarMenuDto> BuildTree(long parentCode)
                    {
                        if (!lookup.ContainsKey(parentCode))
                            return new List<SidebarMenuDto>();

                        return lookup[parentCode].Select(action => new SidebarMenuDto
                        {
                            ActionCode = action.action_code,
                            Title = action.action_name ?? string.Empty,
                            Icon = action.icon,
                            Route = action.action_path,
                            Children = BuildTree(action.action_code),
                        }).ToList();
                    }

                    // 4. Root nodes (parent_code = 0)
                    //return ResponseEntity<object>.Success(BuildTree(0), "Success", HttpStatusCode.OK);

                    userPermissionDTO.sideBar = BuildTree(0);

                    // 1. Filter usable button actions
                    var button_actions = actionModels
                        .Where(x => x.is_active && !x.is_navigable && !x.is_block)
                        .ToList();

                    userPermissionDTO.button_actions = button_actions.Select(x => x.key_name).ToList();

                    return ResponseEntity<object>.Success(userPermissionDTO, "Success");
                }
                return ResponseEntity<object>.Error(null, "No active actions are mapped.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region Action

        public async Task<IActionResult> GetActionsAsync()
        {
            List<ActionModel> response = (await _actionRepo.QueryAsync<ActionModel>("SELECT * FROM tbl_action WHERE is_active = true")).ToList();

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "No actions found.");
            else
                return ResponseEntity<object>.Success(response, "Actions retrieved successfully.");
        }
        public async Task<IActionResult> GetActionByIdAsync(int actionId)
        {
            var response = await _actionRepo.GetByIdAsync(actionId);

            if (response == null)
                return ResponseEntity<object>.Error(null, "Action not found.");
            else
                return ResponseEntity<object>.Success(response, "Action retrieved successfully.");
        }
        public async Task<ResponseEntity<object>> CreateActionAsync(ActionModel action)
        {
            Int64 response = Convert.ToInt64(await _actionRepo.InsertAsync(action));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Action created successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the action.");
        }
        public async Task<ResponseEntity<object>> UpdateActionAsync(ActionModel action)
        {
            int response = await _actionRepo.UpdateAsync(action);

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Action updated successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while updating the action.");
        }
        public async Task<ResponseEntity<object>> DeleteActionAsync(int actionId)
        {
            int response = await _actionRepo.DeleteAsync(actionId);

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Action deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the action.");
        }
        public async Task<ResponseEntity<object>> GetActionForTree(long permission_code)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("permissionCode", permission_code);

                string checkPermissionQuery = "SELECT COUNT(1) FROM tbl_permission_action_mapping WHERE permission_code = @permissionCode";

                var permissionCountResult = await _actionRepo.QueryAsync<int>(checkPermissionQuery, param);

                string allActionsQuery = "SELECT * FROM tbl_actions WHERE is_active = true";
                List<ActionModel> allActions = (await _actionRepo.QueryAsync<ActionModel>(allActionsQuery)).ToList();

                if (allActions.Count == 0)
                {
                    return ResponseEntity<object>.Error(null, "No master actions found to build the tree.");
                }

                string mappedActionsQuery = @"
            SELECT DISTINCT action_code 
            FROM tbl_permission_action_mapping 
            WHERE permission_code = @permissionCode";

                IEnumerable<long> mappedActionCodes = await _actionRepo.QueryAsync<long>(mappedActionsQuery, param);

                HashSet<long> checkedActionCodes = [.. mappedActionCodes];

                var lookup = allActions
                    .GroupBy(x => x.parent_code)
                    .ToDictionary(g => g.Key, g => g.ToList());

                List<ActionTreeNodeDto> BuildTree(long parentCode)
                {
                    if (!lookup.ContainsKey(parentCode))
                        return new List<ActionTreeNodeDto>();

                    return lookup[parentCode].Select(action =>
                    {
                        bool hasChildren = lookup.ContainsKey(action.action_code);

                        return new ActionTreeNodeDto
                        {
                            Key = action.action_code.ToString(),
                            Title = action.action_name ?? string.Empty,
                            Icon = action.icon,
                            Disabled = action.is_block,
                            IsLeaf = !hasChildren,
                            IsChecked = checkedActionCodes.Contains(action.action_code),
                            Children = BuildTree(action.action_code)
                        };
                    }).ToList();
                }

                List<ActionTreeNodeDto> finalTreeStructure = BuildTree(0);

                if (finalTreeStructure.Any())
                    return ResponseEntity<object>.Success(finalTreeStructure, "Action tree with mapping states generated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No root actions available.");
            }
            catch (Exception)
            {
                throw;
            }
        }


        #endregion

        #region Role Permission Mapping
        public async Task<IActionResult> GetRolePermissionsAsync(int role_code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("role_code", role_code);
            var response = await _rolePermissionMappingRepo.QueryAsync<RolePermissionMapping>("SELECT * FROM tbl_role_permission_mapping WHERE role_code = @role_code", parameters);

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "Role permissions not found.");
            else
                return ResponseEntity<object>.Success(response, "Role permissions retrieved successfully.");
        }
        public async Task<ResponseEntity<object>> CreateRolePermissionMappingAsync(RolePermissionMapping rolePermissionMapping)
        {
            if (rolePermissionMapping.role_code <= 0)
                return ResponseEntity<object>.Error(null, "Invalid role code provided.");

            if (rolePermissionMapping.permission_codes == null)
                return ResponseEntity<object>.Error(null, "Permission codes payload cannot be null.");

            try
            {
                var param = new DynamicParameters();
                param.Add("roleCode", rolePermissionMapping.role_code);

                string existingQuery = "SELECT permission_code FROM tbl_role_permission_mapping WHERE role_code = @roleCode";
                IEnumerable<long> existingPermissionCodes = await _rolePermissionMappingRepo.QueryAsync<long>(existingQuery, param);

                HashSet<long> databaseSet = [.. existingPermissionCodes];
                HashSet<long> payloadSet = [.. rolePermissionMapping.permission_codes.Select(x => (long)x)];

                List<long> codesToDelete = databaseSet.Where(code => !payloadSet.Contains(code)).ToList();

                List<long> codesToInsert = payloadSet.Where(code => !databaseSet.Contains(code)).ToList();
                if (codesToDelete.Count > 0)
                {
                    var deleteParam = new DynamicParameters();
                    deleteParam.Add("roleCode", rolePermissionMapping.role_code);
                    deleteParam.Add("codesToDelete", codesToDelete);

                    string deleteQuery = "DELETE FROM tbl_role_permission_mapping WHERE role_code = @roleCode AND permission_code = ANY(@codesToDelete)";
                    await _rolePermissionMappingRepo.QueryAsync<int>(deleteQuery, deleteParam);
                }

                long insertResponse = 0;
                if (codesToInsert.Count > 0)
                {
                    List<RolePermissionMapping> rmList = new List<RolePermissionMapping>();
                    foreach (var permissionCode in codesToInsert)
                    {
                        RolePermissionMapping newMapping = new RolePermissionMapping
                        {
                            role_code = rolePermissionMapping.role_code,
                            permission_code = permissionCode
                        };
                        rmList.Add(newMapping);
                    }

                    insertResponse = Convert.ToInt64(await _rolePermissionMappingRepo.InsertMultipleAsync(rmList));
                }

                if (codesToDelete.Count == 0 && codesToInsert.Count == 0)
                {
                    return ResponseEntity<object>.Success(null, "No mapping changes detected. Database is already up to date.");
                }

                if (codesToInsert.Count > 0 && insertResponse <= 0)
                {
                    return ResponseEntity<object>.Error(null, "An error occurred while inserting new role permission mapping assignments.");
                }

                return ResponseEntity<object>.Success(null, $"Role permission mapping updated successfully. (Inserted: {codesToInsert.Count}, Deleted: {codesToDelete.Count})");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ResponseEntity<object>> DeleteRolePermissionMappingAsync(int role_code, long permission_code)
        {
            int response = await _rolePermissionMappingRepo.DeleteAsync(role_code, new Dictionary<string, object>
            {
                { "permission_code", permission_code }
            });

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Role Permission Mapping deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the role permission mapping.");
        }

        public async Task<ResponseEntity<object>> GetPermissionByRoleCode(long role_code)
        {
            try
            {
                if (role_code <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid role code provided.");

                string allPermissionsQuery = "SELECT * FROM tbl_permission WHERE is_active = true AND is_block = false";
                List<PermissionModel> allPermissions = (await _actionRepo.QueryAsync<PermissionModel>(allPermissionsQuery)).ToList();

                if (allPermissions.Count == 0)
                {
                    return ResponseEntity<object>.Error(null, "No master permissions found.");
                }

                var actionParam = new DynamicParameters();
                actionParam.Add("role_code", role_code);

                string mappedPermissionsQuery = @"
            SELECT DISTINCT permission_code 
            FROM tbl_role_permission_mapping 
            WHERE role_code = @role_code";

                IEnumerable<long> mappedPermissionCodes = await _actionRepo.QueryAsync<long>(mappedPermissionsQuery, actionParam);

                HashSet<long> checkedPermissionCodes = [.. mappedPermissionCodes];

                foreach (var permission in allPermissions)
                {
                    permission.is_checked = checkedPermissionCodes.Contains(permission.permission_code);
                }

                return ResponseEntity<object>.Success(allPermissions, "Permissions with mapping states retrieved successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Permission Action Mapping

        public async Task<IActionResult> GetPermissionActionsAsync(long permission_code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("permission_code", permission_code);
            var response = await _permissionActionMappingRepo.QueryAsync<PermissionActionMapping>("SELECT * FROM tbl_permission_action_mapping WHERE permission_code = @permission_code", parameters);

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "Permission actions not found.");
            else
                return ResponseEntity<object>.Success(response, "Permission actions retrieved successfully.");
        }

        public async Task<ResponseEntity<object>> CreatePermissionActionMappingAsync(PermissionActionMapping permissionActionMapping)
        {
            if (permissionActionMapping.permission_code <= 0)
                return ResponseEntity<object>.Error(null, "Invalid permission code provided.");

            if (permissionActionMapping.action_codes == null)
                return ResponseEntity<object>.Error(null, "Action codes payload cannot be null.");

            try
            {
                var param = new DynamicParameters();
                param.Add("permissionCode", permissionActionMapping.permission_code);

                string existingQuery = "SELECT action_code FROM tbl_permission_action_mapping WHERE permission_code = @permissionCode";
                IEnumerable<long> existingActionCodes = await _permissionActionMappingRepo.QueryAsync<long>(existingQuery, param);

                HashSet<long> databaseSet = [.. existingActionCodes];
                HashSet<long> payloadSet = [.. permissionActionMapping.action_codes];

                List<long> codesToDelete = databaseSet.Where(code => !payloadSet.Contains(code)).ToList();

                List<long> codesToInsert = payloadSet.Where(code => !databaseSet.Contains(code)).ToList();

                if (codesToDelete.Count > 0)
                {
                    var deleteParam = new DynamicParameters();
                    deleteParam.Add("permissionCode", permissionActionMapping.permission_code);
                    deleteParam.Add("codesToDelete", codesToDelete);

                    string deleteQuery = "DELETE FROM tbl_permission_action_mapping WHERE permission_code = @permissionCode AND action_code = ANY(@codesToDelete)";
                    await _permissionActionMappingRepo.QueryAsync<int>(deleteQuery, deleteParam);
                }

                long insertResponse = 0;
                if (codesToInsert.Count > 0)
                {
                    List<PermissionActionMapping> pamList = new List<PermissionActionMapping>();
                    foreach (var actionCode in codesToInsert)
                    {
                        PermissionActionMapping newMapping = new PermissionActionMapping
                        {
                            permission_code = permissionActionMapping.permission_code,
                            action_code = actionCode
                        };
                        pamList.Add(newMapping);
                    }

                    insertResponse = Convert.ToInt64(await _permissionActionMappingRepo.InsertMultipleAsync(pamList));
                }

                if (codesToDelete.Count == 0 && codesToInsert.Count == 0)
                {
                    return ResponseEntity<object>.Success(null, "No mapping changes detected. Database is already up to date.");
                }

                if (codesToInsert.Count > 0 && insertResponse <= 0)
                {
                    return ResponseEntity<object>.Error(null, "An error occurred while inserting new permission action mapping assignments.");
                }

                return ResponseEntity<object>.Success(null, $"Permission action mapping updated successfully. (Inserted: {codesToInsert.Count}, Deleted: {codesToDelete.Count})");
            }
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<ResponseEntity<object>> DeletePermissionActionMappingAsync(long permission_action_code, long permission_code)
        {
            int response = await _permissionActionMappingRepo.DeleteAsync(permission_action_code, new Dictionary<string, object>
            {
                { "permission_code", permission_code }
            });

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Permission Action Mapping deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the permission action mapping.");
        }

        public async Task<IActionResult> GetPermissionActionByRoleCode(long permission_code)
        {
            try
            {
                if (permission_code <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid role code provided.");

                var actionParam = new DynamicParameters();
                actionParam.Add("permission_code", permission_code);

                List<ActionModel> actionModels = (await _actionRepo.QueryAsync<ActionModel>(
                    @"select distinct a.* from tbl_actions as a
              inner join tbl_permission_action_mapping as b on b.action_code=a.action_code
              where b.permission_code = @permission_code", actionParam)).ToList();

                if (actionModels.Count > 0)
                {
                    var validActions = actionModels
                        .Where(x => x.is_active && !x.is_block)
                        .ToList();

                    var lookup = validActions
                        .GroupBy(x => x.parent_code)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    List<ActionTreeNodeDto> BuildTree(long parentCode)
                    {
                        if (!lookup.ContainsKey(parentCode))
                            return new List<ActionTreeNodeDto>();

                        return lookup[parentCode].Select(action =>
                        {
                            bool hasChildren = lookup.ContainsKey(action.action_code);

                            return new ActionTreeNodeDto
                            {
                                Key = action.action_code.ToString(),
                                Title = action.action_name ?? string.Empty,
                                Icon = action.icon,
                                Disabled = false,
                                IsLeaf = !hasChildren,
                                Children = BuildTree(action.action_code)
                            };
                        }).ToList();
                    }

                    List<ActionTreeNodeDto> finalTreeStructure = BuildTree(0);

                    if (finalTreeStructure.Any())
                        return ResponseEntity<object>.Success(finalTreeStructure, "Action tree generated successfully.");
                    else
                        return ResponseEntity<object>.Error(null, "No root actions available.");
                }

                return ResponseEntity<object>.Error(null, "No mapped actions found for this role.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
