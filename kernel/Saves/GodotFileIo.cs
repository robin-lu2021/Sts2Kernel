using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaCrit.Sts2.Core.Saves;

public class GodotFileIo : ISaveStore
{
	private static readonly string _userDataRoot = Path.Combine(Environment.CurrentDirectory, ".kernel_user_data");

	public string SaveDir { get; set; }

	public GodotFileIo(string saveDir)
	{
		SaveDir = saveDir;
		CreateDirectory(SaveDir);
	}

	public string GetFullPath(string filename)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			return NormalizePath(SaveDir);
		}
		string normalizedFilename = filename.Replace('\\', '/');
		if (Path.IsPathRooted(normalizedFilename) || normalizedFilename.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return NormalizePath(normalizedFilename);
		}
		string saveDir = SaveDir.Replace('\\', '/');
		string combined = normalizedFilename.StartsWith(saveDir, StringComparison.OrdinalIgnoreCase)
			? normalizedFilename
			: saveDir.TrimEnd('/') + "/" + normalizedFilename;
		return NormalizePath(combined);
	}

	public string? ReadFile(string path)
	{
		path = GetFullPath(path);
		return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
	}

	public async Task<string?> ReadFileAsync(string path)
	{
		path = GetFullPath(path);
		return File.Exists(path) ? await File.ReadAllTextAsync(path, Encoding.UTF8) : null;
	}

	public DateTimeOffset GetLastModifiedTime(string path)
	{
		return File.GetLastWriteTimeUtc(GetFullPath(path));
	}

	public int GetFileSize(string path)
	{
		string fullPath = GetFullPath(path);
		return File.Exists(fullPath) ? (int)new FileInfo(fullPath).Length : 0;
	}

	public void SetLastModifiedTime(string path, DateTimeOffset time)
	{
		File.SetLastWriteTimeUtc(GetFullPath(path), time.UtcDateTime);
	}

	public void WriteFile(string path, string content)
	{
		WriteFile(path, Encoding.UTF8.GetBytes(content ?? string.Empty));
	}

	public void WriteFile(string path, byte[] bytes)
	{
		path = GetFullPath(path);
		EnsureParentDirectory(path);
		CopyBackup(path);
		string tempPath = path + ".tmp";
		File.WriteAllBytes(tempPath, bytes);
		RenameFile(tempPath, path);
	}

	public Task WriteFileAsync(string path, string content)
	{
		return WriteFileAsync(path, Encoding.UTF8.GetBytes(content ?? string.Empty));
	}

	public async Task WriteFileAsync(string path, byte[] bytes)
	{
		path = GetFullPath(path);
		EnsureParentDirectory(path);
		CopyBackup(path);
		string tempPath = path + ".tmp";
		await File.WriteAllBytesAsync(tempPath, bytes);
		RenameFile(tempPath, path);
	}

	public bool FileExists(string path)
	{
		return File.Exists(GetFullPath(path));
	}

	public bool DirectoryExists(string path)
	{
		return Directory.Exists(GetFullPath(path));
	}

	public void DeleteFile(string path)
	{
		path = GetFullPath(path);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public void RenameFile(string sourcePath, string destinationPath)
	{
		sourcePath = GetFullPath(sourcePath);
		destinationPath = GetFullPath(destinationPath);
		EnsureParentDirectory(destinationPath);
		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException("Cannot rename missing file.", sourcePath);
		}
		if (File.Exists(destinationPath))
		{
			File.Delete(destinationPath);
		}
		File.Move(sourcePath, destinationPath);
	}

	public string[] GetFilesInDirectory(string directoryPath)
	{
		directoryPath = GetFullPath(directoryPath);
		return Directory.Exists(directoryPath)
			? Directory.GetFiles(directoryPath).Select(Path.GetFileName).Where(name => name != null).Cast<string>().ToArray()
			: Array.Empty<string>();
	}

	public string[] GetDirectoriesInDirectory(string directoryPath)
	{
		directoryPath = GetFullPath(directoryPath);
		return Directory.Exists(directoryPath)
			? Directory.GetDirectories(directoryPath).Select(Path.GetFileName).Where(name => name != null).Cast<string>().ToArray()
			: Array.Empty<string>();
	}

	public void CreateDirectory(string directoryPath)
	{
		Directory.CreateDirectory(GetFullPath(directoryPath));
	}

	public void DeleteDirectory(string directoryPath)
	{
		directoryPath = GetFullPath(directoryPath);
		if (Directory.Exists(directoryPath))
		{
			Directory.Delete(directoryPath, recursive: true);
		}
	}

	public void DeleteTemporaryFiles(string directoryPath)
	{
		directoryPath = GetFullPath(directoryPath);
		if (!Directory.Exists(directoryPath))
		{
			return;
		}
		foreach (string tempFile in Directory.GetFiles(directoryPath, "*.tmp", SearchOption.TopDirectoryOnly))
		{
			File.Delete(tempFile);
		}
	}

	private static void CopyBackup(string fullPath)
	{
		if (File.Exists(fullPath))
		{
			File.Copy(fullPath, fullPath + ".backup", overwrite: true);
		}
	}

	private static void EnsureParentDirectory(string path)
	{
		string? parent = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(parent))
		{
			Directory.CreateDirectory(parent);
		}
	}

	private static string NormalizePath(string path)
	{
		string normalized = path.Replace('\\', '/');
		if (normalized.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			string relativePath = normalized.Substring("user://".Length).Replace('/', Path.DirectorySeparatorChar);
			return Path.GetFullPath(Path.Combine(_userDataRoot, relativePath));
		}
		return Path.GetFullPath(normalized.Replace('/', Path.DirectorySeparatorChar));
	}
}
