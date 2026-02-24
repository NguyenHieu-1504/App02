using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Entities;
using InternalApkDistribution.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternalApkDistribution.Api.Controllers;

[ApiController]
[Route("api/apps")]
public class AppsController : ControllerBase
{
    private readonly IApkReleaseRepository _repository;

    public AppsController(IApkReleaseRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Danh sách ứng dụng (nhóm theo packageName).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var list = await _repository.GetAppSummariesAsync(cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Danh sách version của một ứng dụng. Query: sort=versionCode|uploadedAt, order=asc|desc.
    /// </summary>
    [HttpGet("{packageName}/versions")]
    public async Task<ActionResult<IReadOnlyList<ApkRelease>>> Versions(
        string packageName,
        [FromQuery] string sort = "versionCode",
        [FromQuery] string order = "desc",
        CancellationToken cancellationToken = default)
    {
        var descending = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        var list = await _repository.GetVersionsAsync(packageName, sort, descending, cancellationToken);
        if (list.Count == 0)
            return NotFound(new { error = "Không tìm thấy ứng dụng hoặc chưa có phiên bản nào." });
        return Ok(list);
    }
}
