using File_Vault.Core;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace File_Vault
{
    public partial class MainWindow : Window
    {
        private readonly VaultManager _vaultManager;
        private string _currentVaultName;

        public MainWindow()
        {
            InitializeComponent();
            _vaultManager = new VaultManager();
            RefreshVaultList();
            VaultCreationPanel.Visibility = Visibility.Collapsed;
        }

        private void RefreshVaultList()
        {
            SelectVault.ItemsSource = _vaultManager.GetVaults();
            SelectVault.DisplayMemberPath = "DisplayInfo";
        }

        private void NewVaultButton_Click(object sender, RoutedEventArgs e)
        {
            VaultCreationPanel.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Creating new vault...";
        }

        private void CreateVaultButton_Click(object sender, RoutedEventArgs e)
        {
            string vaultName = VaultNameTextBox.Text;
            string password = VaultPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(vaultName))
            {
                MessageBox.Show("Please enter a vault name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter a password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _vaultManager.CreateVault(vaultName, password);
                RefreshVaultList();
                StatusTextBlock.Text = $"Vault '{vaultName}' created successfully";
                VaultCreationPanel.Visibility = Visibility.Collapsed;
                VaultNameTextBox.Clear();
                VaultPasswordBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create vault: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectVault.SelectedItem == null)
            {
                MessageBox.Show("Please select a vault first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedVault = (VaultInfo)SelectVault.SelectedItem;
            _currentVaultName = selectedVault.Name;

            var passwordDialog = new PasswordDialog("Enter vault password to add files:");
            if (passwordDialog.ShowDialog() == true)
            {
                try
                {
                    var vaultService = _vaultManager.GetVaultService(_currentVaultName);
                    if (!vaultService.VerifyPassword(passwordDialog.Password))
                    {
                        MessageBox.Show("Invalid password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var openFileDialog = new OpenFileDialog { Multiselect = true, Title = "Select files to add to vault" };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        foreach (string filePath in openFileDialog.FileNames)
                        {
                            vaultService.AddFile(filePath, passwordDialog.Password, deleteOriginal: true);
                        }
                        StatusTextBlock.Text = $"Added {openFileDialog.FileNames.Length} file(s) to vault '{_currentVaultName}'";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExtractFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectVault.SelectedItem == null)
            {
                MessageBox.Show("Please select a vault first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedVault = (VaultInfo)SelectVault.SelectedItem;
            _currentVaultName = selectedVault.Name;

            var passwordDialog = new PasswordDialog("Enter vault password to extract files:");
            if (passwordDialog.ShowDialog() == true)
            {
                try
                {
                    var vaultService = _vaultManager.GetVaultService(_currentVaultName);
                    if (!vaultService.VerifyPassword(passwordDialog.Password))
                    {
                        MessageBox.Show("Invalid password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "Select destination folder for extracted files"
                    };
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        vaultService.ExtractFiles(folderDialog.SelectedPath, passwordDialog.Password);
                        StatusTextBlock.Text = $"Files extracted from vault '{_currentVaultName}' to {folderDialog.SelectedPath}";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to extract files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteVaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectVault.SelectedItem == null)
            {
                MessageBox.Show("Please select a vault first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedVault = (VaultInfo)SelectVault.SelectedItem;
            if (MessageBox.Show($"Are you sure you want to delete vault '{selectedVault.Name}'? This action cannot be undone.",
                                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    if (_vaultManager.DeleteVault(selectedVault.Name))
                    {
                        RefreshVaultList();
                        StatusTextBlock.Text = $"Vault '{selectedVault.Name}' deleted";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete vault: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
