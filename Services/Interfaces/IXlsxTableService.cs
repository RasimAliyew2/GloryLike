namespace GloryLikeBackend.Services.Interfaces;

public interface IXlsxTableService
{
    IReadOnlyList<XlsxTableRow> ReadSheet(
        Stream input,
        string sheetName,
        int maxRows = 5001,
        int maxColumns = 20);

    byte[] CreateWorkbook(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows);
}

public sealed record XlsxTableRow(
    int RowNumber,
    IReadOnlyList<string> Cells);
