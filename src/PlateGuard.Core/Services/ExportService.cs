using System.Globalization;
using System.Text;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.Core.Services;

public sealed class ExportService(ISettingsRepository settingsRepository) : IExportService
{
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private readonly ISettingsRepository _settingsRepository = settingsRepository;

    public string GetDefaultExportFolder()
    {
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documentsFolder)
            ? Path.Combine(AppContext.BaseDirectory, "exports")
            : Path.Combine(documentsFolder, "PlateGuard Exports");
    }

    public async Task<ExportPromotionUsageRecordsResult> ExportPromotionUsageRecordsAsync(
        ExportPromotionUsageRecordsRequest request,
        CancellationToken cancellationToken = default)
    {
        request ??= new ExportPromotionUsageRecordsRequest();

        if (request.Records.Count == 0)
        {
            return Failure("No records are available to export.");
        }

        var exportFolder = await ResolveExportFolderAsync(request.ExportFolder, cancellationToken);
        try
        {
            if (File.Exists(exportFolder))
            {
                return Failure("Export folder must point to a folder, not an existing file.");
            }

            Directory.CreateDirectory(exportFolder);

            var prefix = string.IsNullOrWhiteSpace(request.FileNamePrefix)
                ? "plateguard-history"
                : request.FileNamePrefix.Trim();
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var filePath = Path.Combine(exportFolder, $"{prefix}-{timestamp}.csv");

            var csvContent = BuildCsv(request.Records);
            await File.WriteAllTextAsync(filePath, csvContent, Utf8WithBom, cancellationToken);

            return new ExportPromotionUsageRecordsResult
            {
                IsSuccess = true,
                Message = $"Exported {request.Records.Count} record(s) to {filePath}.",
                FilePath = filePath,
                ExportedRecordCount = request.Records.Count
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Failure(BuildExportFailureMessage(exception, exportFolder));
        }
    }

    private async Task<string> ResolveExportFolderAsync(string? preferredExportFolder, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(preferredExportFolder))
        {
            return preferredExportFolder.Trim();
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings?.ExportFolder))
        {
            return settings.ExportFolder.Trim();
        }

        return GetDefaultExportFolder();
    }

    private static string BuildCsv(IReadOnlyList<PromotionUsageRecord> records)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder,
        [
            "Service Date",
            "Promotion Name",
            "Vehicle Number",
            "Owner Name",
            "Phone Number",
            "Brand",
            "Model",
            "Mileage",
            "Normal Price",
            "Discounted Price",
            "Amount Paid",
            "Notes"
        ]);

        foreach (var record in records)
        {
            AppendCsvRow(builder,
            [
                record.ServiceDate == default
                    ? string.Empty
                    : record.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                record.PromotionName,
                string.IsNullOrWhiteSpace(record.VehicleNumberRaw) ? record.VehicleNumberNormalized : record.VehicleNumberRaw,
                record.OwnerName ?? string.Empty,
                record.PhoneNumber,
                record.Brand ?? string.Empty,
                record.Model ?? string.Empty,
                record.Mileage?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatDecimal(record.NormalPrice),
                FormatDecimal(record.DiscountedPrice),
                FormatDecimal(record.AmountPaid),
                record.Notes ?? string.Empty
            ]);
        }

        return builder.ToString();
    }

    private static string FormatDecimal(decimal? value)
    {
        return value?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void AppendCsvRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsvValue)));
    }

    private static string EscapeCsvValue(string? value)
    {
        var sanitized = value ?? string.Empty;
        if (!sanitized.Contains(',') && !sanitized.Contains('"') && !sanitized.Contains('\r') && !sanitized.Contains('\n'))
        {
            return sanitized;
        }

        return $"\"{sanitized.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string BuildExportFailureMessage(Exception exception, string exportFolder)
    {
        if (exception is UnauthorizedAccessException)
        {
            return $"Could not write to the export folder. Check permissions for {exportFolder}.";
        }

        if (exception is PathTooLongException)
        {
            return "Export folder path is too long.";
        }

        if (exception is ArgumentException or NotSupportedException)
        {
            return "Export folder path is invalid.";
        }

        return $"Could not access the export folder. Check that {exportFolder} is available and try again.";
    }

    private static ExportPromotionUsageRecordsResult Failure(string message)
    {
        return new ExportPromotionUsageRecordsResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
