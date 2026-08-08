using CsvHelper;
using CsvHelper.Configuration;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.FileHelper;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using ExcelDataReader;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Globalization;

namespace DUSTER.EComm.Services.Modules.ImportEngine
{
    /// <summary>
    /// The Generic Base Engine. Handles ALL file reading, validation, DB insertion, and error file generation.
    /// You never need to rewrite this logic for new processes.
    /// </summary>
    public abstract class BaseImportHandler<T> : IImportHandler where T : BaseEntity
    {
        public abstract string EntityType { get; }

        //protected abstract IImportStrategy<T> GetStrategy();
        protected abstract Task<IImportStrategy<T>> GetStrategyAsync(IServiceScope scope);
        protected abstract AbstractValidator<T> GetValidator();

        public async Task ProcessAsync(ImportJob job, IServiceScope scope)
        {
            var entityRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<T>>();
            var jobRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<ImportJob>>();

            var strategy = await GetStrategyAsync(scope);
            var validator = GetValidator();

            var validRecords = new List<T>();
            var errorRecords = new List<ImportRowError>();
            string[] headers = null;

            string fileExtension = Path.GetExtension(job.file_path).ToLower();

            try
            {
                if (fileExtension == ".csv")
                {
                    ParseCsv(job.file_path, strategy, validRecords, errorRecords, out headers);
                }
                else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    ParseExcel(job.file_path, strategy, validRecords, errorRecords, out headers);
                }
                else
                {
                    throw new Exception("Unsupported file format. Please upload .csv, .xls, or .xlsx");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read file: {ex.Message}");
            }

            var recordsToInsert = new List<T>();

            foreach (var model in validRecords)
            {
                var validationResult = await entityRepo.ModelValidating(new ValidationModel() { ValidateModel = validator, Model = model });

                if (validationResult is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                {
                    errorRecords.Add(new ImportRowError
                    {
                        OriginalRow = new Dictionary<string, string> { { "Data", "Validation Failed" } },
                        ErrorMessage = errorResponse.Message
                    });
                }
                else
                {
                    recordsToInsert.Add(model);
                }
            }

            if (recordsToInsert.Any())
            {
                await entityRepo.InsertMultipleAsync(recordsToInsert);
            }

            // 5. Generate Error CSV if necessary
            string errorFilePath = null;
            if (errorRecords.Any())
            {
                string errorsDir = FileHelper.ImportErrorFilePath();
                FileHelper.CreateDirectory(errorsDir);

                string errorFileName = $"Errors_Job_{job.job_id}_{Guid.NewGuid()}.csv";
                string physicalErrorPath = Path.Combine(errorsDir, errorFileName);

                GenerateErrorCsvFile(errorRecords, headers, physicalErrorPath);

                // Set relative URL for frontend download
                errorFilePath = Path.Combine(errorsDir, errorFileName);
            }

            // 6. Update Job Status
            job.total_rows = validRecords.Count + errorRecords.Count;
            job.processed_rows = job.total_rows;
            job.success_count = validRecords.Count;
            job.failed_count = errorRecords.Count;
            job.error_file_path = errorFilePath;
            job.status = errorRecords.Any() ? "CompletedWithErrors" : "Completed";

            await jobRepo.UpdateAsync(job);
        }

        private void GenerateErrorCsvFile(List<ImportRowError> errorRecords, string[] originalHeaders, string outputPath)
        {
            using var writer = new StreamWriter(outputPath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Write Headers
            foreach (var header in originalHeaders) csv.WriteField(header);
            csv.WriteField("Import_Error_Reason");
            csv.NextRecord();

            // Write Failed Data
            foreach (var record in errorRecords)
            {
                foreach (var header in originalHeaders)
                {
                    csv.WriteField(record.OriginalRow.GetValueOrDefault(header, ""));
                }
                csv.WriteField(record.ErrorMessage);
                csv.NextRecord();
            }
        }

        //CSV PARSING LOGIC
        private void ParseCsv(string filePath, IImportStrategy<T> strategy, List<T> validRecords, List<ImportRowError> errorRecords, out string[] headers)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.Read();
            csv.ReadHeader();
            headers = csv.HeaderRecord;

            while (csv.Read())
            {
                var rawRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in headers) rawRow[header] = csv.GetField(header) ?? string.Empty;

                try
                {
                    validRecords.Add(strategy.Map(rawRow));
                }
                catch (Exception ex)
                {
                    errorRecords.Add(new ImportRowError { OriginalRow = rawRow, ErrorMessage = "Mapping Error: " + ex.Message });
                }
            }
        }

        //EXCEL PARSING LOGIC
        private void ParseExcel(string filePath, IImportStrategy<T> strategy, List<T> validRecords, List<ImportRowError> errorRecords, out string[] headers)
        {
            // Required for ExcelDataReader in .NET Core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Read Excel into a fast DataTable, treating the first row as headers
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
            });

            var dataTable = result.Tables[0];

            // Extract headers
            var headerList = new List<string>();
            foreach (DataColumn column in dataTable.Columns) headerList.Add(column.ColumnName);
            headers = headerList.ToArray();

            // Loop through rows
            foreach (DataRow row in dataTable.Rows)
            {
                var rawRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in headers)
                {
                    rawRow[header] = row[header]?.ToString()?.Trim() ?? string.Empty;
                }

                try
                {
                    validRecords.Add(strategy.Map(rawRow));
                }
                catch (Exception ex)
                {
                    errorRecords.Add(new ImportRowError { OriginalRow = rawRow, ErrorMessage = "Mapping Error: " + ex.Message });
                }
            }
        }
    }
}
