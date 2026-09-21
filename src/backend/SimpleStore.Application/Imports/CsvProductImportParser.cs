using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using SimpleStore.Application.Errors;
using SimpleStore.Domain;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;

namespace SimpleStore.Application.ProductImports;

internal static class CsvProductImportParser
{
    private static readonly string[] RequiredHeaders =
    [
        "SKU",
        "Barcode",
        "Name",
        "Unit",
        "SalePrice",
        "OpeningCost",
        "OpeningQuantity"
    ];

    public static ParsedImport Parse(byte[] content)
    {
        if (content.Length == 0)
        {
            return new ParsedImport([], [Error(null, "file", "file-empty", "CSV file is empty.")]);
        }

        try
        {
            using var reader = new StreamReader(
                new MemoryStream(content),
                new UTF8Encoding(false, true),
                true);
            using var parser = new TextFieldParser(reader)
            {
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true,
                TextFieldType = FieldType.Delimited
            };
            parser.SetDelimiters(",");

            if (parser.EndOfData)
            {
                return new ParsedImport([], [Error(null, "file", "file-empty", "CSV file is empty.")]);
            }

            var headers = parser.ReadFields() ?? [];
            var headerErrors = ValidateHeaders(headers);
            if (headerErrors.Count > 0)
            {
                return new ParsedImport([], headerErrors);
            }

            var rows = new List<ParsedImportRow>();
            var identifiers = new List<ImportIdentifier>();
            var errors = new List<ValidationError>();
            var rowNumber = 1;

            while (!parser.EndOfData)
            {
                rowNumber++;
                if (rowNumber > 10_001)
                {
                    errors.Add(Error(
                        rowNumber,
                        "file",
                        "too-many-rows",
                        "CSV file cannot contain more than 10,000 product rows."));
                    break;
                }

                string[] fields;
                try
                {
                    fields = parser.ReadFields() ?? [];
                }
                catch (MalformedLineException)
                {
                    errors.Add(Error(
                        rowNumber,
                        "row",
                        "malformed-csv-row",
                        "CSV row is malformed."));
                    continue;
                }

                if (fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (fields.Length != RequiredHeaders.Length)
                {
                    errors.Add(Error(
                        rowNumber,
                        "row",
                        "invalid-column-count",
                        $"Expected {RequiredHeaders.Length} columns but found {fields.Length}."));
                    continue;
                }

                identifiers.Add(new ImportIdentifier(
                    rowNumber,
                    string.IsNullOrWhiteSpace(fields[0]) ? null : fields[0].Trim(),
                    string.IsNullOrWhiteSpace(fields[1]) ? null : fields[1].Trim()));

                var parsedRow = ParseRow(fields, rowNumber, errors);
                if (parsedRow is not null)
                {
                    rows.Add(parsedRow);
                }
            }

            if (rows.Count == 0 && errors.Count == 0)
            {
                errors.Add(Error(null, "file", "import-empty", "CSV file contains no product rows."));
            }

            AddFileDuplicateErrors(identifiers, errors);
            return new ParsedImport(rows, errors);
        }
        catch (DecoderFallbackException)
        {
            return new ParsedImport(
                [],
                [Error(null, "file", "invalid-encoding", "CSV file must use UTF-8 encoding.")]);
        }
    }

    private static List<ValidationError> ValidateHeaders(string[] headers)
    {
        var errors = new List<ValidationError>();
        if (headers.Length != RequiredHeaders.Length)
        {
            errors.Add(Error(
                1,
                "header",
                "invalid-template",
                $"Template must contain: {string.Join(",", RequiredHeaders)}."));
            return errors;
        }

        for (var index = 0; index < RequiredHeaders.Length; index++)
        {
            if (!headers[index].Trim().Equals(RequiredHeaders[index], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error(
                    1,
                    "header",
                    "invalid-template",
                    $"Column {index + 1} must be '{RequiredHeaders[index]}'."));
            }
        }

        return errors;
    }

    private static ParsedImportRow? ParseRow(
        string[] fields,
        int rowNumber,
        List<ValidationError> errors)
    {
        var sku = string.IsNullOrWhiteSpace(fields[0])
            ? $"SP-{Guid.NewGuid():N}"[..11].ToUpperInvariant()
            : fields[0].Trim();
        var barcode = string.IsNullOrWhiteSpace(fields[1]) ? null : fields[1].Trim();
        var name = fields[2].Trim();
        var unit = fields[3].Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Error(rowNumber, "Name", "name-required", "Product name is required."));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            errors.Add(Error(rowNumber, "Unit", "unit-required", "Product unit is required."));
        }

        var salePrice = ParseRequiredDecimal(fields[4], rowNumber, "SalePrice", errors);
        var openingCost = ParseOptionalDecimal(fields[5], rowNumber, "OpeningCost", errors);
        var openingQuantity = ParseRequiredDecimal(fields[6], rowNumber, "OpeningQuantity", errors);

        if (salePrice is null || openingQuantity is null)
        {
            return null;
        }

        try
        {
            _ = Product.Create(
                Guid.NewGuid(),
                sku,
                barcode,
                name,
                unit,
                salePrice.Value,
                openingCost,
                DateTimeOffset.UnixEpoch);
            _ = OpeningInventory.Create(openingQuantity.Value, openingCost);
        }
        catch (DomainRuleException exception)
        {
            errors.Add(Error(rowNumber, MapDomainCodeToField(exception.Code), exception.Code, exception.Message));
            return null;
        }

        return new ParsedImportRow(
            rowNumber,
            sku,
            barcode,
            name,
            unit,
            salePrice.Value,
            openingCost,
            openingQuantity.Value);
    }

    private static decimal? ParseRequiredDecimal(
        string value,
        int rowNumber,
        string field,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(Error(rowNumber, field, "field-required", $"{field} is required."));
            return null;
        }

        return ParseDecimal(value, rowNumber, field, errors);
    }

    private static decimal? ParseOptionalDecimal(
        string value,
        int rowNumber,
        string field,
        List<ValidationError> errors) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value, rowNumber, field, errors);

    private static decimal? ParseDecimal(
        string value,
        int rowNumber,
        string field,
        List<ValidationError> errors)
    {
        if (decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedValue))
        {
            return parsedValue;
        }

        errors.Add(Error(
            rowNumber,
            field,
            "invalid-number",
            $"{field} must be a valid number using '.' as the decimal separator."));
        return null;
    }

    private static void AddFileDuplicateErrors(
        IReadOnlyCollection<ImportIdentifier> identifiers,
        List<ValidationError> errors)
    {
        AddDuplicateErrors(
            identifiers.Where(item => item.Sku is not null),
            item => Product.NormalizeIdentifier(item.Sku!),
            "SKU",
            "duplicate-sku-in-file",
            errors);
        AddDuplicateErrors(
            identifiers.Where(item => item.Barcode is not null),
            item => Product.NormalizeIdentifier(item.Barcode!),
            "Barcode",
            "duplicate-barcode-in-file",
            errors);
    }

    private static void AddDuplicateErrors(
        IEnumerable<ImportIdentifier> identifiers,
        Func<ImportIdentifier, string> keySelector,
        string field,
        string code,
        List<ValidationError> errors)
    {
        foreach (var duplicateGroup in identifiers.GroupBy(keySelector).Where(group => group.Count() > 1))
        {
            foreach (var identifier in duplicateGroup)
            {
                errors.Add(Error(
                    identifier.RowNumber,
                    field,
                    code,
                    $"{field} is duplicated in the import file."));
            }
        }
    }

    private static string MapDomainCodeToField(string code) => code switch
    {
        "sku-required" => "SKU",
        "invalid-barcode" => "Barcode",
        "name-required" => "Name",
        "unit-required" => "Unit",
        "invalid-sale-price" => "SalePrice",
        "invalid-opening-cost" or "opening-cost-required" => "OpeningCost",
        "invalid-opening-quantity" => "OpeningQuantity",
        _ => "row"
    };

    private static ValidationError Error(int? row, string field, string code, string message) =>
        new(row, field, code, message);
}

internal sealed record ParsedImport(
    IReadOnlyList<ParsedImportRow> Rows,
    IReadOnlyList<ValidationError> Errors);

internal sealed record ParsedImportRow(
    int RowNumber,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? OpeningCost,
    decimal OpeningQuantity);

internal sealed record ImportIdentifier(int RowNumber, string? Sku, string? Barcode);
