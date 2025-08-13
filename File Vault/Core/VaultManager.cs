using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace File_Vault.Core
{
    public class VaultManager
    {
        private readonly string _databasePath;
        private VaultDatabase _database;

        public VaultManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "FileVault");

            _databasePath = Path.Combine(appFolder, "vaults.db");
            Directory.CreateDirectory(appFolder);
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            if (File.Exists(_databasePath))
            {
                try
                {
                    string json = File.ReadAllText(_databasePath);
                    _database = JsonConvert.DeserializeObject<VaultDatabase>(json) ?? new VaultDatabase();
                }
                catch (Exception ex)
                {
                    // Create new database if corrupted
                    _database = new VaultDatabase();
                    System.Diagnostics.Debug.WriteLine($"Failed to load vault database: {ex.Message}");
                }
            }
            else
            {
                _database = new VaultDatabase();
            }
        }

        private void SaveDatabase()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                File.WriteAllText(_databasePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save vault database: {ex.Message}");
                throw;
            }
        }

        public void CreateVault(string vaultName, string password)
        {
            if (string.IsNullOrWhiteSpace(vaultName))
                throw new ArgumentException("Vault name cannot be empty");

            if (_database.Vaults.Any(v => v.Name.Equals(vaultName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A vault with this name already exists");

            string vaultsRoot = Path.Combine(Path.GetDirectoryName(_databasePath), "Vaults");
            string vaultFolder = Path.Combine(vaultsRoot, Guid.NewGuid().ToString());
            Directory.CreateDirectory(vaultFolder);

            var vaultService = new VaultService(vaultFolder);
            vaultService.CreateVault(password);

            _database.Vaults.Add(new VaultInfo
            {
                Name = vaultName,
                Path = vaultFolder,
                CreatedDate = DateTime.Now,
                LastAccessed = DateTime.Now
            });

            SaveDatabase();
        }

        public List<VaultInfo> GetVaults() => _database.Vaults.OrderBy(v => v.Name).ToList();

        public bool DeleteVault(string vaultName)
        {
            var vault = _database.Vaults.FirstOrDefault(v => v.Name == vaultName);
            if (vault == null) return false;

            try
            {
                if (Directory.Exists(vault.Path))
                {
                    Directory.Delete(vault.Path, true);
                }

                _database.Vaults.Remove(vault);
                SaveDatabase();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete vault: {ex.Message}");
                return false;
            }
        }

        public VaultService GetVaultService(string vaultName)
        {
            var vault = _database.Vaults.FirstOrDefault(v => v.Name == vaultName);
            if (vault == null)
                throw new FileNotFoundException("Vault not found");

            vault.LastAccessed = DateTime.Now;
            SaveDatabase();

            return new VaultService(vault.Path);
        }

        public bool VaultExists(string vaultName)
        {
            return _database.Vaults.Any(v => v.Name.Equals(vaultName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class VaultDatabase
    {
        public List<VaultInfo> Vaults { get; set; } = new List<VaultInfo>();
    }

    public class VaultInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastAccessed { get; set; }

        public string DisplayInfo => $"{Name} (Created: {CreatedDate:yyyy-MM-dd}, Last Accessed: {LastAccessed:yyyy-MM-dd})";
    }
}