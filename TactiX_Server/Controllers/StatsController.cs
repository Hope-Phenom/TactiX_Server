using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TactiX_Server.Data;
using TactiX_Server.Models.Req;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Stats;

namespace TactiX_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly StatsDbContext _context;
        private readonly ILogger<StatsController> _logger;

        public StatsController(StatsDbContext context, ILogger<StatsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 对外接口

        [HttpPost("PostExceptionReport")]
        public async Task<ActionResult<ExceptionReportModel>> PostExceptionReportModel([FromBody] ExceptionReportModel report)
        {
            _logger.LogInformation("Received POST request for new ExceptionReport.");

            try
            {
                // 模型验证
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for ExceptionReport.");
                    return BadRequest(ModelState);
                }

                // 添加Report
                _context.ExceptionReports.Add(report);
                await _context.SaveChangesAsync();

                // 记录成功日志
                _logger.LogInformation("ExceptionReport created successfully.");

                // 返回200保存成功
                return StatusCode(200, "Save ExceptionReport successfully.");
            }
            catch (DbUpdateException ex)
            {
                // 记录数据库错误
                _logger.LogError(ex, "Database error while creating ExceptionReport.");
                return StatusCode(500, "An error occurred while saving the ExceptionReport.");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                _logger.LogError(ex, "Unexpected error while creating ExceptionReport.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("PostVersionControl")]
        public async Task<ActionResult<PostVersionControlResp>> PostPostVersionControlResp([FromBody] PostVersionControlReq req)
        {
            _logger.LogInformation("Received POST request for new PostVersionControl.");

            try
            {
                // 模型验证
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for PostVersionControlReq.");
                    return BadRequest(ModelState);
                }

                var lastest = await QueryLastestVersionAsync();
                if (lastest == null) return NotFound("Can't find any lastest version data.");

                var currVer = await QueryVersionByVersionAsync(req.Version);
                if (currVer == null) return NotFound("Can't find version data by version string.");

                var compareResult = Tools.CompareVersion(req.Version, lastest.Version);

                var resp = new PostVersionControlResp() 
                { 
                    LastestVersion = lastest.Version,
                    Banned = currVer.Banned,
                    ForceUpgrade = lastest.ForceUpgrade,
                    ReleaseUrl = lastest.ReleaseUrl
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                // 记录其他异常
                _logger.LogError(ex, "Unexpected error while get PostVersionControl.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        #endregion

        #region 内部处理方法

        /// <summary>
        /// 获取最新的一条版本数据
        /// </summary>
        /// <returns>版本记录条目</returns>
        private async Task<VersionControlModel?> QueryLastestVersionAsync()
        {
            return await _context.VersionControls
                .AsNoTracking()
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// 根据传入的版本号确定其状态
        /// </summary>
        /// <param name="version">传入版本号</param>
        /// <returns>Version数据</returns>
        private async Task<VersionControlModel?> QueryVersionByVersionAsync(string version)
        {
            return await _context.VersionControls
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Version == version);
        }

        #endregion
    }
}
