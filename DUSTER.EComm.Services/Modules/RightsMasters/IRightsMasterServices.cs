using DUSTER.EComm.Services.Modules.RightsMasters.Models;

namespace DUSTER.EComm.Services.Modules.RightsMasters
{
    public interface IRightsMasterServices
    {
        #region Role

        Task<IActionResult> GetRolesAsync();
        Task<IActionResult> GetRoleByIdAsync(int roleCode);
        Task<ResponseEntity<object>> CreateRoleAsync(RoleModel role);
        Task<ResponseEntity<object>> UpdateRoleAsync(RoleModel role);
        Task<ResponseEntity<object>> DeleteRoleAsync(int roleCode);

        #endregion

        #region Permission
        Task<IActionResult> GetPermissionsAsync();
        Task<IActionResult> GetPermissionByIdAsync(long permissionId);
        Task<ResponseEntity<object>> CreatePermissionAsync(PermissionModel permission);
        Task<ResponseEntity<object>> UpdatePermissionAsync(PermissionModel permission);
        Task<ResponseEntity<object>> DeletePermissionAsync(long permissionId);
        Task<ResponseEntity<object>> PermissionDropdown();
        Task<ResponseEntity<object>> GetUserPermission();
        #endregion

        #region Action
        Task<IActionResult> GetActionsAsync();
        Task<IActionResult> GetActionByIdAsync(int actionId);
        Task<ResponseEntity<object>> CreateActionAsync(ActionModel action);
        Task<ResponseEntity<object>> UpdateActionAsync(ActionModel action);
        Task<ResponseEntity<object>> DeleteActionAsync(int actionId);
        Task<ResponseEntity<object>> GetActionForTree(long permission_code);
        #endregion

        #region Role Permission Mapping
        Task<IActionResult> GetRolePermissionsAsync(int role_code);
        Task<ResponseEntity<object>> CreateRolePermissionMappingAsync(RolePermissionMapping rolePermissionMapping);
        Task<ResponseEntity<object>> DeleteRolePermissionMappingAsync(int role_code, long permission_code);
        Task<ResponseEntity<object>> GetPermissionByRoleCode(long role_code);

        #endregion

        #region Permission Action Mapping
        Task<IActionResult> GetPermissionActionsAsync(long permission_code);
        Task<ResponseEntity<object>> CreatePermissionActionMappingAsync(PermissionActionMapping permissionActionMapping);
        Task<ResponseEntity<object>> DeletePermissionActionMappingAsync(long permission_action_code, long permission_code);
        Task<IActionResult> GetPermissionActionByRoleCode(long permission_code);
        #endregion
    }
}
