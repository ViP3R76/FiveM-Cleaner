namespace FiveMCleaner.Models;

public sealed record CleanupResult(bool Success, long BytesFreed, long FilesProcessed, int TargetsProcessed, string Message);
