using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static System.Reflection.Metadata.BlobBuilder;

namespace GAP
{
    public sealed partial class MainPage4 : Page
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=0000;Database=Agenda";

        public ObservableCollection<Book> Books { get; set; } = new ObservableCollection<Book>();

        public MainPage4()
        {
            this.InitializeComponent();
            LoadBooks();
        }

        private void LoadBooks()
        {
            Books.Clear();
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand("SELECT * FROM books", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Books.Add(new Book
                        {
                            Id = reader.GetInt32(0),
                            Titre = reader.GetString(1),
                            Auteur = reader.GetString(2),
                            DatePublication = reader.GetDateTime(3),
                            Description = reader.GetString(4),
                            Status = reader.GetString(5)
                        });
                    }
                }
            }
            clientListView.ItemsSource = Books;
        }

        private async void SaveClient_Click(object sender, RoutedEventArgs e)
        {
            var titre = bookName.Text;
            var auteur = autorName.Text;
            var datePublication = DateTime.Parse(dateName.Text);
            var description = descriptionName.Text;
            var status = statuName.Text;

            if (string.IsNullOrEmpty(titre) || string.IsNullOrEmpty(auteur) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(status))
            {
                await ShowMessage("Tous les champs doivent être remplis.");
                return;
            }

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand("INSERT INTO books (titre, auteur, publication, description, status) VALUES (@titre, @auteur, @publication, @description, @status)", connection);
                command.Parameters.AddWithValue("titre", titre);
                command.Parameters.AddWithValue("auteur", auteur);
                command.Parameters.AddWithValue("publication", datePublication);
                command.Parameters.AddWithValue("description", description);
                command.Parameters.AddWithValue("status", status);
                await command.ExecuteNonQueryAsync();
            }
            LoadBooks();
            ClearForm();
        }

        private async void EditBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                // Créer un StackPanel pour contenir les champs
                var stackPanel = new StackPanel();

                // Champs pour chaque propriété du livre
                var titreTextBox = new TextBox { Text = book.Titre, Margin = new Thickness(0, 0, 0, 10) };
                var auteurTextBox = new TextBox { Text = book.Auteur, Margin = new Thickness(0, 0, 0, 10) };
                var publicationTextBox = new TextBox { Text = book.DatePublication.ToString("yyyy-MM-dd"), Margin = new Thickness(0, 0, 0, 10) };
                var descriptionTextBox = new TextBox { Text = book.Description, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
                var statusTextBox = new TextBox { Text = book.Status, Margin = new Thickness(0, 0, 0, 10) };

                // Ajouter les champs au StackPanel
                stackPanel.Children.Add(new TextBlock { Text = "Titre", Margin = new Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(titreTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Auteur", Margin = new Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(auteurTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Date de publication (AAAA-MM-JJ)", Margin = new Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(publicationTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Description", Margin = new Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(descriptionTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Statut", Margin = new Thickness(0, 0, 0, 5) });
                stackPanel.Children.Add(statusTextBox);

                // Créer le ContentDialog avec les champs
                var inputDialog = new ContentDialog
                {
                    Title = "Modifier le livre",
                    Content = stackPanel,
                    PrimaryButtonText = "Enregistrer",
                    CloseButtonText = "Annuler"
                };

                var result = await inputDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var newTitre = titreTextBox.Text;
                    var newAuteur = auteurTextBox.Text;
                    var newPublication = publicationTextBox.Text;
                    var newDescription = descriptionTextBox.Text;
                    var newStatus = statusTextBox.Text;

                    // Vérifier les données avant de les enregistrer
                    if (!string.IsNullOrEmpty(newTitre) && !string.IsNullOrEmpty(newAuteur) && DateTime.TryParse(newPublication, out var parsedDate))
                    {
                        try
                        {
                            using (var connection = new NpgsqlConnection(ConnectionString))
                            {
                                await connection.OpenAsync();

                                string query = @"UPDATE books 
                                         SET titre = @titre, auteur = @auteur, publication = @publication, 
                                             description = @description, status = @status 
                                         WHERE id = @id";
                                using (var command = new NpgsqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("titre", newTitre);
                                    command.Parameters.AddWithValue("auteur", newAuteur);
                                    command.Parameters.AddWithValue("publication", parsedDate);
                                    command.Parameters.AddWithValue("description", newDescription);
                                    command.Parameters.AddWithValue("status", newStatus);
                                    command.Parameters.AddWithValue("id", book.Id);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            await ShowMessage($"Le livre '{book.Titre}' a été modifié avec succès.");
                            LoadBooks(); // Recharger la liste des livres
                        }
                        catch (Exception ex)
                        {
                            await ShowMessage($"Erreur : {ex.Message}");
                        }
                    }
                    else
                    {
                        await ShowMessage("Veuillez remplir correctement tous les champs.");
                    }
                }
            }
        }

        private void SearchBooks_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = searchTextBox.Text.ToLower();

            var filteredBooks = Books.Where(b => b.Titre.ToLower().Contains(query) || b.Auteur.ToLower().Contains(query)).ToList();

            clientListView.ItemsSource = filteredBooks;
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            // Récupère l'objet livre lié à ce bouton
            var book = (sender as Button)?.Tag as Book; // Assurez-vous que Book est votre modèle de données

            if (book != null)
            {
                // Change le statut
                if (book.Status == "Disponible")
                {
                    book.Status = "Emprunté";
                }
                else
                {
                    book.Status = "Disponible";
                }

                // Met à jour l'affichage (si nécessaire)
                clientListView.ItemsSource = null;
                clientListView.ItemsSource = book; // "booksList" étant votre liste de livres
            }
        }



        private async void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                var confirmation = new ContentDialog
                {
                    Title = "Confirmation",
                    Content = $"Voulez-vous vraiment supprimer le livre '{book.Titre}' ?",
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

                            string query = "DELETE FROM books WHERE id = @id";
                            using (var command = new NpgsqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("id", book.Id);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await ShowMessage($"Le livre '{book.Titre}' a été supprimé avec succès.");
                        LoadBooks(); // Recharger la liste des livres
                    }
                    catch (Exception ex)
                    {
                        await ShowMessage($"Erreur : {ex.Message}");
                    }
                }
            }
        }


        private void ClearForm()
        {
            bookName.Text = "";
            autorName.Text = "";
            dateName.Text = "";
            descriptionName.Text = "";
            statuName.Text = "";
            enregistrerButton.Tag = null;
        }

        private async Task ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Message",
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
    }

    public class Book
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Auteur { get; set; }
        public DateTime DatePublication { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
    }
}
