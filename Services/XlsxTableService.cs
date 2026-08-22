using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using GloryLikeBackend.Services.Interfaces;

namespace GloryLikeBackend.Services;

public sealed class XlsxTableService : IXlsxTableService
{
    private const long MaxXmlEntryBytes = 20 * 1024 * 1024;
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace DocumentRelationshipsNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipsNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    public IReadOnlyList<XlsxTableRow> ReadSheet(
        Stream input,
        string sheetName,
        int maxRows = 5001,
        int maxColumns = 20)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(sheetName))
            throw new InvalidDataException("Excel sheet name is required.");
        if (maxRows < 1 || maxColumns < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRows));

        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        var workbook = LoadXml(archive, "xl/workbook.xml");
        var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");

        var sheet = workbook
            .Descendants(SpreadsheetNamespace + "sheet")
            .FirstOrDefault(item => string.Equals(
                (string?)item.Attribute("name"),
                sheetName,
                StringComparison.OrdinalIgnoreCase));

        if (sheet is null)
            throw new InvalidDataException($"Excel sheet '{sheetName}' was not found.");

        var relationshipId = (string?)sheet.Attribute(
            DocumentRelationshipsNamespace + "id");
        var target = relationships
            .Descendants(PackageRelationshipsNamespace + "Relationship")
            .FirstOrDefault(item => string.Equals(
                (string?)item.Attribute("Id"),
                relationshipId,
                StringComparison.Ordinal))
            ?.Attribute("Target")
            ?.Value;

        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidDataException($"Excel sheet '{sheetName}' is invalid.");

        var sheetPath = NormalizeWorkbookTarget(target);
        var worksheet = LoadXml(archive, sheetPath);
        var sharedStrings = ReadSharedStrings(archive);
        var result = new List<XlsxTableRow>();

        foreach (var row in worksheet
                     .Descendants(SpreadsheetNamespace + "row")
                     .Take(maxRows + 1))
        {
            if (result.Count >= maxRows)
                throw new InvalidDataException($"Excel can contain at most {maxRows} rows.");

            var rowNumber = ParsePositiveInt((string?)row.Attribute("r"))
                ?? result.Count + 1;
            var cells = Enumerable.Repeat(string.Empty, maxColumns).ToArray();

            foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
            {
                var columnIndex = GetColumnIndex((string?)cell.Attribute("r"));
                if (columnIndex < 0 || columnIndex >= maxColumns)
                    continue;

                cells[columnIndex] = ReadCellValue(cell, sharedStrings).Trim();
            }

            result.Add(new XlsxTableRow(rowNumber, cells));
        }

        return result;
    }

    public byte[] CreateWorkbook(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            throw new ArgumentException("Sheet name is required.", nameof(sheetName));
        if (headers.Count == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTextEntry(archive, "[Content_Types].xml", ContentTypesXml());
            WriteTextEntry(archive, "_rels/.rels", RootRelationshipsXml());
            WriteTextEntry(archive, "xl/workbook.xml", WorkbookXml(sheetName));
            WriteTextEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                WorkbookRelationshipsXml());
            WriteTextEntry(archive, "xl/styles.xml", StylesXml());
            WriteTextEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                WorksheetXml(headers, rows));
        }

        return output.ToArray();
    }

    private static XDocument LoadXml(ZipArchive archive, string entryPath)
    {
        var entry = archive.GetEntry(entryPath)
            ?? throw new InvalidDataException($"Excel entry '{entryPath}' was not found.");

        if (entry.Length > MaxXmlEntryBytes)
            throw new InvalidDataException("Excel contains an unexpectedly large XML entry.");

        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxXmlEntryBytes
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        if (archive.GetEntry("xl/sharedStrings.xml") is null)
            return Array.Empty<string>();

        var document = LoadXml(archive, "xl/sharedStrings.xml");
        return document
            .Descendants(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(
                item.Descendants(SpreadsheetNamespace + "t")
                    .Select(text => text.Value)))
            .ToList();
    }

    private static string ReadCellValue(
        XElement cell,
        IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");

        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
        {
            return string.Concat(cell
                .Descendants(SpreadsheetNamespace + "t")
                .Select(item => item.Value));
        }

        var rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.Ordinal)
            && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        if (string.Equals(type, "b", StringComparison.Ordinal))
            return rawValue == "1" ? "TRUE" : "FALSE";

        return rawValue;
    }

    private static string NormalizeWorkbookTarget(string target)
    {
        var normalized = target.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"xl/{normalized}";
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
            return -1;

        var index = 0;
        var letters = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
                break;

            index = checked(index * 26 + (char.ToUpperInvariant(character) - 'A' + 1));
            letters++;
        }

        return letters == 0 ? -1 : index - 1;
    }

    private static int? ParsePositiveInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0
                ? parsed
                : null;
    }

    private static void WriteTextEntry(
        ZipArchive archive,
        string path,
        string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(
            entry.Open(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string ContentTypesXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private static string RootRelationshipsXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string WorkbookXml(string sheetName)
    {
        using var output = new Utf8StringWriter();
        using (var writer = XmlWriter.Create(output, CompactXmlSettings()))
        {
            writer.WriteStartDocument(true);
            writer.WriteStartElement("workbook", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString(
                "xmlns",
                "r",
                null,
                DocumentRelationshipsNamespace.NamespaceName);
            writer.WriteStartElement("sheets", SpreadsheetNamespace.NamespaceName);
            writer.WriteStartElement("sheet", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString("name", sheetName);
            writer.WriteAttributeString("sheetId", "1");
            writer.WriteAttributeString(
                "r",
                "id",
                DocumentRelationshipsNamespace.NamespaceName,
                "rId1");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return output.ToString();
    }

    private static string WorkbookRelationshipsXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private static string StylesXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="2">
            <font><sz val="11"/><name val="Aptos"/><family val="2"/></font>
            <font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Aptos"/><family val="2"/></font>
          </fonts>
          <fills count="3">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF086A52"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="2">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left style="thin"><color rgb="FFD6DCE7"/></left><right style="thin"><color rgb="FFD6DCE7"/></right><top style="thin"><color rgb="FFD6DCE7"/></top><bottom style="thin"><color rgb="FFD6DCE7"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="3">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private static string WorksheetXml(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var output = new Utf8StringWriter();
        using (var writer = XmlWriter.Create(output, CompactXmlSettings()))
        {
            writer.WriteStartDocument(true);
            writer.WriteStartElement("worksheet", SpreadsheetNamespace.NamespaceName);
            writer.WriteStartElement("dimension", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString(
                "ref",
                $"A1:{ColumnName(headers.Count - 1)}{Math.Max(1, rows.Count + 1)}");
            writer.WriteEndElement();

            writer.WriteStartElement("sheetViews", SpreadsheetNamespace.NamespaceName);
            writer.WriteStartElement("sheetView", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString("showGridLines", "0");
            writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString("ySplit", "1");
            writer.WriteAttributeString("topLeftCell", "A2");
            writer.WriteAttributeString("activePane", "bottomLeft");
            writer.WriteAttributeString("state", "frozen");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("cols", SpreadsheetNamespace.NamespaceName);
            for (var index = 0; index < headers.Count; index++)
            {
                writer.WriteStartElement("col", SpreadsheetNamespace.NamespaceName);
                writer.WriteAttributeString("min", (index + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("max", (index + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("width", index == headers.Count - 1 ? "30" : "24");
                writer.WriteAttributeString("customWidth", "1");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData", SpreadsheetNamespace.NamespaceName);
            WriteRow(writer, 1, headers, 1);
            for (var index = 0; index < rows.Count; index++)
                WriteRow(writer, index + 2, rows[index], 2);
            writer.WriteEndElement();

            writer.WriteStartElement("autoFilter", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString(
                "ref",
                $"A1:{ColumnName(headers.Count - 1)}{Math.Max(1, rows.Count + 1)}");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return output.ToString();
    }

    private static void WriteRow(
        XmlWriter writer,
        int rowNumber,
        IReadOnlyList<string> cells,
        int styleIndex)
    {
        writer.WriteStartElement("row", SpreadsheetNamespace.NamespaceName);
        writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
        if (rowNumber == 1)
        {
            writer.WriteAttributeString("ht", "28");
            writer.WriteAttributeString("customHeight", "1");
        }

        for (var index = 0; index < cells.Count; index++)
        {
            writer.WriteStartElement("c", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString("r", $"{ColumnName(index)}{rowNumber}");
            writer.WriteAttributeString("s", styleIndex.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("t", "inlineStr");
            writer.WriteStartElement("is", SpreadsheetNamespace.NamespaceName);
            writer.WriteStartElement("t", SpreadsheetNamespace.NamespaceName);
            writer.WriteAttributeString(
                "xml",
                "space",
                "http://www.w3.org/XML/1998/namespace",
                "preserve");
            writer.WriteString(index < cells.Count ? cells[index] ?? string.Empty : string.Empty);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static string ColumnName(int zeroBasedIndex)
    {
        var number = zeroBasedIndex + 1;
        var result = new StringBuilder();
        while (number > 0)
        {
            number--;
            result.Insert(0, (char)('A' + number % 26));
            number /= 26;
        }

        return result.ToString();
    }

    private static XmlWriterSettings CompactXmlSettings() => new()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = false,
        OmitXmlDeclaration = false
    };

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
