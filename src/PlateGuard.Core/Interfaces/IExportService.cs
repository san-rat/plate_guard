using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IExportService
{
    string GetDefaultExportFolder();
    Task<ExportPromotionUsageRecordsResult> ExportPromotionUsageRecordsAsync(
        ExportPromotionUsageRecordsRequest request,
        CancellationToken cancellationToken = default);
}
