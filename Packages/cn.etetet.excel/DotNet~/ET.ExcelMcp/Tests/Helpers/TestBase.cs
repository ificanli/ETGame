using System.Text.Json.Nodes;

namespace ET.Test;

/// <summary>
///     Base class for all tests providing common functionality
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly string TestDir;
    protected readonly List<string> TestFiles = new();

    protected TestBase()
    {
        TestDir = Path.Combine(Path.GetTempPath(), "EpplusMcpServerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDir);
    }

    public virtual void Dispose()
    {
        // Clean up test files with retry mechanism
        foreach (var file in TestFiles) DeleteFileWithRetry(file);

        // Delete directory with retry mechanism
        DeleteDirectoryWithRetry(TestDir);
    }

    /// <summary>
    ///     Deletes a file with retry mechanism to handle locked files
    /// </summary>
    private static void DeleteFileWithRetry(string filePath, int maxRetries = 3, int delayMs = 100)
    {
        if (!File.Exists(filePath))
            return;

        for (var attempt = 0; attempt < maxRetries; attempt++)
            try
            {
                // Force garbage collection to release file handles
                if (attempt > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(delayMs * (attempt + 1));
                }

                File.Delete(filePath);
                return; // Success
            }
            catch (IOException)
            {
                // File is locked, retry
                if (attempt == maxRetries - 1)
                    // Last attempt failed, try to remove read-only attribute and retry
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            fileInfo.IsReadOnly = false;
                            File.Delete(filePath);
                            return;
                        }
                    }
                    catch
                    {
                        // Ignore final cleanup errors
                    }
            }
            catch (UnauthorizedAccessException)
            {
                // Permission denied, try to remove read-only attribute
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        fileInfo.IsReadOnly = false;
                        File.Delete(filePath);
                        return;
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch
            {
                // Other errors, ignore after retries
                if (attempt == maxRetries - 1)
                    return;
            }
    }

    /// <summary>
    ///     Deletes a directory with retry mechanism to handle locked files
    /// </summary>
    private static void DeleteDirectoryWithRetry(string directoryPath, int maxRetries = 5, int delayMs = 200)
    {
        if (!Directory.Exists(directoryPath))
            return;

        for (var attempt = 0; attempt < maxRetries; attempt++)
            try
            {
                // Force garbage collection to release file handles
                if (attempt > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect(); // Second collection to ensure finalizers completed
                    Thread.Sleep(delayMs * (attempt + 1));
                }

                // Try to delete all files first, then directory
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files) DeleteFileWithRetry(file, 2, 50);

                // Try to delete all subdirectories first
                var subDirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var subDir in subDirs.Reverse()) // Delete from deepest first
                    try
                    {
                        Directory.Delete(subDir, false);
                    }
                    catch
                    {
                        // Ignore individual subdirectory errors
                    }

                // Finally delete the main directory
                Directory.Delete(directoryPath, false);
                return; // Success
            }
            catch (IOException)
            {
                // Directory or files are locked, retry
                if (attempt == maxRetries - 1)
                    // Last attempt: try to remove read-only attributes
                    try
                    {
                        var dirInfo = new DirectoryInfo(directoryPath);
                        if (dirInfo.Exists)
                        {
                            RemoveReadOnlyAttributes(dirInfo);
                            Directory.Delete(directoryPath, true);
                            return;
                        }
                    }
                    catch
                    {
                        // Ignore final cleanup errors - directory will be cleaned up later
                    }
            }
            catch (UnauthorizedAccessException)
            {
                // Permission denied, try to remove read-only attributes
                try
                {
                    var dirInfo = new DirectoryInfo(directoryPath);
                    if (dirInfo.Exists)
                    {
                        RemoveReadOnlyAttributes(dirInfo);
                        Directory.Delete(directoryPath, true);
                        return;
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch
            {
                // Other errors, ignore after retries
                if (attempt == maxRetries - 1)
                    return;
            }
    }

    /// <summary>
    ///     Removes read-only attributes from directory and files recursively
    /// </summary>
    private static void RemoveReadOnlyAttributes(DirectoryInfo dirInfo)
    {
        try
        {
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;

            foreach (var file in dirInfo.GetFiles())
                try
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }
                catch
                {
                    // Ignore individual file errors
                }

            foreach (var subDir in dirInfo.GetDirectories()) RemoveReadOnlyAttributes(subDir);
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    ///     Creates a test file path
    /// </summary>
    protected string CreateTestFilePath(string fileName)
    {
        var filePath = Path.Combine(TestDir, fileName);
        TestFiles.Add(filePath);
        return filePath;
    }

    /// <summary>
    ///     Creates a JsonObject with common parameters
    /// </summary>
    protected JsonObject CreateArguments(string operation, string path, string? outputPath = null)
    {
        var args = new JsonObject
        {
            ["operation"] = operation,
            ["path"] = path
        };

        if (!string.IsNullOrEmpty(outputPath)) args["outputPath"] = outputPath;

        return args;
    }
}
