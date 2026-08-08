using DUSTER.EComm.Services.Modules.RightsMasters;
using DUSTER.EComm.Services.Modules.RightsMasters.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.API.Modules.RightsMasters
{
    [Route("api/[controller]")]
    [ApiController]
    public class RightsMasterController : ControllerBase
    {
        private readonly IRightsMasterServices _masterServices;
        public RightsMasterController(IRightsMasterServices masterServices)
        {
            _masterServices = masterServices;
        }

        #region Role Master

        [HttpPost("role/create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleModel roleModel)
        {
            try
            {
                return await _masterServices.CreateRoleAsync(roleModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("role/update")]
        public async Task<IActionResult> UpdateRole([FromBody] RoleModel roleModel)
        {
            try
            {
                return await _masterServices.UpdateRoleAsync(roleModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("role/list")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                return await _masterServices.GetRolesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("role/{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            try
            {
                return await _masterServices.GetRoleByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpDelete("role/delete/{id}")]
        public async Task<IActionResult> RoleDropdown(int id)
        {
            try
            {
                return await _masterServices.DeleteRoleAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region Permission Master

        [HttpPost("permission/create")]
        public async Task<IActionResult> CreatePermission([FromBody] PermissionModel permissionModel)
        {
            try
            {
                return await _masterServices.CreatePermissionAsync(permissionModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("permission/update")]
        public async Task<IActionResult> UpdatePermission([FromBody] PermissionModel permissionModel)
        {
            try
            {
                return await _masterServices.UpdatePermissionAsync(permissionModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("permission/list")]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                return await _masterServices.GetPermissionsAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("permission/{id}")]
        public async Task<IActionResult> GetPermissionById(long id)
        {
            try
            {
                return await _masterServices.GetPermissionByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpDelete("permission/delete/{id}")]
        public async Task<IActionResult> DeletePermission(long id)
        {
            try
            {
                return await _masterServices.DeletePermissionAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("permission/dropdown")]
        public async Task<IActionResult> PermissionDropdown()
        {
            try
            {
                return await _masterServices.PermissionDropdown();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        #endregion

        #region Role Permission Mapping

        [HttpGet("role/permissions/{role_code}")]
        public async Task<IActionResult> GetRolePermissions(int role_code)
        {
            try
            {
                return await _masterServices.GetRolePermissionsAsync(role_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("role/permission/create")]
        public async Task<IActionResult> CreateRolePermissionMapping([FromBody] RolePermissionMapping rolePermissionMapping)
        {
            try
            {
                return await _masterServices.CreateRolePermissionMappingAsync(rolePermissionMapping);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("permission/of/role/code/{role_code}")]
        public async Task<IActionResult> GetPermissionByRoleCode(long role_code)
        {
            try
            {
                return await _masterServices.GetPermissionByRoleCode(role_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        #endregion

        #region Permission Action Mapping

        [HttpGet("permission/actions/{permission_code}")]
        public async Task<IActionResult> GetPermissionActions(long permission_code)
        {
            try
            {
                return await _masterServices.GetPermissionActionsAsync(permission_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("permission/action/create")]
        public async Task<IActionResult> CreatePermissionActionMapping([FromBody] PermissionActionMapping permissionActionMapping)
        {
            try
            {
                return await _masterServices.CreatePermissionActionMappingAsync(permissionActionMapping);
                //return await _masterServices.SyncPermissionActionMappingAsync(permissionActionMapping);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("permission/actions/of/permission/code/{permission_code}")]
        public async Task<IActionResult> GetPermissionActionByRoleCode(long permission_code)
        {
            try
            {
                return await _masterServices.GetPermissionActionByRoleCode(permission_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region Actions

        [HttpGet("get/action/tree/for/{permission_code}")]
        public async Task<IActionResult> GetActionForTree(long permission_code)
        {
            try
            {
                return await _masterServices.GetActionForTree(permission_code);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet("get/user/permissions")]
        public async Task<IActionResult> GetUserPermission()
        {
            try
            {
                return await _masterServices.GetUserPermission();
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        #endregion
    }
}
