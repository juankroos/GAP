using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceModel;
using Npgsql;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GAP
{
    public sealed partial class MainPage1 : Page
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=0000;Database=Agenda";

        public MainPage1()
        {
            InitializeComponent();
            LoadClients();
        }

        // Clear all input fields
        private void ClearFields()
        {
            clientName.Text = string.Empty;
            telephoneClient.Text = string.Empty;
            emailClient.Text = string.Empty;
            passwordClient.Password = string.Empty;
            codeCNI.Text = string.Empty;
        }

        public class Client
        {
            public int Id { get; set; }
            public string Nom { get; set; }
            public string Telephone { get; set; }
            public string Email { get; set; }
        }

        // Load clients from the database
        private async void LoadClients()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT id, nom, telephone, email FROM utilisateurs";
                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var clients = new ObservableCollection<Client>();

                        while (await reader.ReadAsync())
                        {
                            clients.Add(new Client
                            {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Telephone = reader.GetString(2),
                                Email = reader.GetString(3)
                            });
                        }

                        clientListView.ItemsSource = clients;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"Erreur : {ex.Message}");
            }
        }

        // Save new client to the database
        private async void SaveClient_Click(object sender, RoutedEventArgs e)
        {
            string nom = clientName.Text;
            string telephone = telephoneClient.Text;
            string email = emailClient.Text;
            string motDePasse = passwordClient.Password;
            string cni = codeCNI.Text;

            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(telephone) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse) ||
                string.IsNullOrWhiteSpace(cni))
            {
                await ShowMessage("Veuillez remplir tous les champs.");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        INSERT INTO utilisateurs (nom, telephone, email, mot_de_passe, cni)
                        VALUES (@nom, @telephone, @email, @motDePasse, @cni)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("nom", nom);
                        command.Parameters.AddWithValue("telephone", telephone);
                        command.Parameters.AddWithValue("email", email);
                        command.Parameters.AddWithValue("motDePasse", motDePasse);
                        command.Parameters.AddWithValue("cni", cni);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            await ShowMessage("Compte créé avec succès.");
                            ClearFields();
                            LoadClients();
                        }
                        else
                        {
                            await ShowMessage("Échec de la création du compte.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"Erreur : {ex.Message}");
            }
        }

        // Delete a client from the database
        private async void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Client client)
            {
                var confirmation = new ContentDialog
                {
                    Title = "Confirmation",
                    Content = $"Voulez-vous vraiment supprimer {client.Nom} ?",
                    PrimaryButtonText = "Oui",
                    CloseButtonText = "Non"
                };

                if (await confirmation.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var connection = new NpgsqlConnection(ConnectionString))
                        {
                            await connection.OpenAsync();

                            string query = "DELETE FROM utilisateurs WHERE id = @id";
                            using (var command = new NpgsqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("id", client.Id);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await ShowMessage($"{client.Nom} a été supprimé avec succès.");
                        LoadClients();  // Re-load the client list
                    }
                    catch (Exception ex)
                    {
                        await ShowMessage($"Erreur : {ex.Message}");
                    }
                }
            }
        }

        // Update client information (modification of the name)
        private async void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Client client)
            {
                // Créer un StackPanel pour contenir les champs
                var stackPanel = new StackPanel();

                // Champ pour le nom
                var nameTextBox = new TextBox { Text = client.Nom, Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 10) };
                var telephoneTextBox = new TextBox { Text = client.Telephone, Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 10) };
                var emailTextBox = new TextBox { Text = client.Email, Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 10) };

                // Ajouter les champs au StackPanel
                stackPanel.Children.Add(new TextBlock { Text = "Nom", Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(nameTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Téléphone", Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(telephoneTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Email", Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(emailTextBox);

                // Créer le ContentDialog avec les champs
                var inputDialog = new ContentDialog
                {
                    Title = "Modifier le client",
                    Content = stackPanel,
                    PrimaryButtonText = "Enregistrer",
                    CloseButtonText = "Annuler"
                };

                var result = await inputDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var newName = nameTextBox.Text;
                    var newTelephone = telephoneTextBox.Text;
                    var newEmail = emailTextBox.Text;

                    // Vérifiez si les champs sont valides avant de sauvegarder
                    if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newTelephone) && !string.IsNullOrEmpty(newEmail))
                    {
                        try
                        {
                            using (var connection = new NpgsqlConnection(ConnectionString))
                            {
                                await connection.OpenAsync();

                                string query = "UPDATE utilisateurs SET nom = @nom, telephone = @telephone, email = @email WHERE id = @id";
                                using (var command = new NpgsqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("nom", newName);
                                    command.Parameters.AddWithValue("telephone", newTelephone);
                                    command.Parameters.AddWithValue("email", newEmail);
                                    command.Parameters.AddWithValue("id", client.Id);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            await ShowMessage($"Client {client.Nom} modifié avec succès.");
                            LoadClients();  // Re-charge la liste des clients
                        }
                        catch (Exception ex)
                        {
                            await ShowMessage($"Erreur : {ex.Message}");
                        }
                    }
                    else
                    {
                        await ShowMessage("Veuillez remplir tous les champs.");
                    }
                }
            }
        }


        // Navigate back to the main page
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        // Utility method for displaying messages
        private static async System.Threading.Tasks.Task ShowMessage(string content)
        {
            var dialog = new MessageDialog(content);
            await dialog.ShowAsync();
        }
    }
}
