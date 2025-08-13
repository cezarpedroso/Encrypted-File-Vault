using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VaultService
{
    private readonly string _vaultPath;
    private readonly string _metaFile;
    private readonly string _hashFile;
    private readonly string _saltFile;

    public VaultService(string vaultPath)
    {
        _vaultPath = vaultPath;
        _metaFile = Path.Combine(vaultPath, "vault.meta");
        _hashFile = Path.Combine(vaultPath, "vault.hash");
        _saltFile = Path.Combine(vaultPath, "vault.salt");
    }

    public void CreateVault(string password)
    {
        Directory.CreateDirectory(_vaultPath);

        var salt = CryptoHelper.GenerateSalt();
        var hash = CryptoHelper.HashPassword(password, salt);

        File.WriteAllBytes(_saltFile, salt);
        File.WriteAllBytes(_hashFile, hash);

        SaveMetadata(new VaultMetadata 
        { 
            Files = new List<FileEntry>(),
            CreatedDate = DateTime.Now,
            LastModified = DateTime.Now
        });
    }

    public bool VerifyPassword(string password)
    {
        if (!File.Exists(_hashFile) || !File.Exists(_saltFile))
            return false;

        var salt = File.ReadAllBytes(_saltFile);
        var storedHash = File.ReadAllBytes(_hashFile);
        var computedHash = CryptoHelper.HashPassword(password, salt);

        return FixedTimeEquals(storedHash, computedHash);
    }

    public void AddFile(string filePath, string password, bool deleteOriginal = false)
    {
        if (!VerifyPassword(password))
            throw new UnauthorizedAccessException("Invalid password");

        var salt = File.ReadAllBytes(_saltFile);
        var key = CryptoHelper.DeriveKey(password, salt);

        var fileBytes = File.ReadAllBytes(filePath);
        var encrypted = CryptoHelper.EncryptBytes(fileBytes, key, out var iv);

        var encryptedFileName = Guid.NewGuid().ToString() + ".enc";
        var encryptedPath = Path.Combine(_vaultPath, encryptedFileName);

        using (var fs = new FileStream(encryptedPath, FileMode.Create))
        {
            fs.Write(iv, 0, iv.Length);
            fs.Write(encrypted, 0, encrypted.Length);
        }

        var meta = LoadMetadata();
        meta.Files.Add(new FileEntry
        {
            OriginalName = Path.GetFileName(filePath),
            EncryptedName = encryptedFileName,
            AddedDate = DateTime.Now,
            FileSize = fileBytes.Length
        });
        meta.LastModified = DateTime.Now;
        SaveMetadata(meta);

        if (deleteOriginal)
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Log this if needed
            }
        }
    }

    public void ExtractFiles(string destination, string password)
    {
        if (!VerifyPassword(password))
            throw new UnauthorizedAccessException("Invalid password");

        var salt = File.ReadAllBytes(_saltFile);
        var key = CryptoHelper.DeriveKey(password, salt);

        var meta = LoadMetadata();

        Directory.CreateDirectory(destination);

        foreach (var file in meta.Files)
        {
            var encryptedPath = Path.Combine(_vaultPath, file.EncryptedName);
            var allBytes = File.ReadAllBytes(encryptedPath);

            var iv = new byte[16];
            Array.Copy(allBytes, 0, iv, 0, iv.Length);

            var cipher = new byte[allBytes.Length - iv.Length];
            Array.Copy(allBytes, iv.Length, cipher, 0, cipher.Length);

            var decrypted = CryptoHelper.DecryptBytes(cipher, key, iv);

            File.WriteAllBytes(Path.Combine(destination, file.OriginalName), decrypted);
        }
    }

    public void RemoveFile(string encryptedFileName, string password)
    {
        if (!VerifyPassword(password))
            throw new UnauthorizedAccessException("Invalid password");

        var meta = LoadMetadata();
        var fileToRemove = meta.Files.FirstOrDefault(f => f.EncryptedName == encryptedFileName);
        
        if (fileToRemove != null)
        {
            var filePath = Path.Combine(_vaultPath, encryptedFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            meta.Files.Remove(fileToRemove);
            meta.LastModified = DateTime.Now;
            SaveMetadata(meta);
        }
    }

    public VaultMetadata GetMetadata(string password)
    {
        if (!VerifyPassword(password))
            throw new UnauthorizedAccessException("Invalid password");

        return LoadMetadata();
    }

    private VaultMetadata LoadMetadata()
    {
        if (!File.Exists(_metaFile))
            return new VaultMetadata { Files = new List<FileEntry>() };

        var json = File.ReadAllText(_metaFile);
        return JsonConvert.DeserializeObject<VaultMetadata>(json) ?? new VaultMetadata { Files = new List<FileEntry>() };
    }

    private void SaveMetadata(VaultMetadata metadata)
    {
        var json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        File.WriteAllText(_metaFile, json);
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;

        int diff = 0;
        for (int i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];
        return diff == 0;
    }
}

public class VaultMetadata
{
    public List<FileEntry> Files { get; set; } = new List<FileEntry>();
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public int FileCount => Files?.Count ?? 0;
    public long TotalSize => Files?.Sum(f => f.FileSize) ?? 0;
}

public class FileEntry
{
    public string OriginalName { get; set; }
    public string EncryptedName { get; set; }
    public DateTime AddedDate { get; set; }
    public long FileSize { get; set; }
}