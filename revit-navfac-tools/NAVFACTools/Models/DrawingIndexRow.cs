namespace NAVFACTools.Models;

public sealed record DrawingIndexRow(
    int SourceLine,
    string SheetNumber,
    string NavfacDrawingNumber,
    string? SheetTitle
);
