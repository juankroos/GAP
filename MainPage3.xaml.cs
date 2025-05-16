using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Ports;
using System.ServiceModel;
using System.Threading.Tasks;
using Npgsql;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace GAP
{
    public sealed partial class MainPage3 : Page

    {
        private SerialPort serialPort;
       
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=0000;Database=Agenda";

        public MainPage3()
        {
            this.InitializeComponent();
            ChargerPassages();
            InitializeSerialPort();
            StartSerialPort();
            //YoAsync();
        
            //HandleSensorData();




        }

        // Class repr�sentant un Passage
        public class Passage
        {
            public string TypePassage { get; set; }
            public decimal Montant { get; set; }
            public DateTime DatePassage { get; set; }
        }
        private void InitializeSerialPort()
        {
            serialPort = new SerialPort("COM7", 9600); // Remplacez "COM7" par le port utilis� par votre Arduino
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }
        private void StartSerialPort()
        {
            try
            {
                serialPort.Open(); // Ouvrir le port s�rie
                //labelStatus.Text = "Connexion s�rie ouverte. En attente des donn�es...";
            }
            catch (Exception ex)
            {
                //labelStatus.Text = $"Erreur : {ex.Message}";
            }
        }


        // M�thode pour arr�ter la connexion s�rie
        private void StopSerialPort()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                //labelStatus.Text = "Connexion s�rie ferm�e.";
            }
        }
       

        private async void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string sensorData = serialPort.ReadLine(); // Lire une ligne de donn�es

            // V�rifier si nous devons mettre � jour l'interface sur le thread principal
            if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                // On est d�j� sur le thread principal, on peut mettre � jour l'UI directement
                await HandleSensorData(sensorData);
            }
            else
            {
                // On n'est pas sur le thread principal, on utilise le dispatcher pour revenir sur le thread principal
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        // Appel de la fonction asynchrone pour g�rer les donn�es
                        await HandleSensorData(sensorData);
                    });
            }
        }
        // Gestionnaire d'�v�nements pour recevoir des donn�es s�rie
        private async Task YoAsync()
        {
            //InsertPassage();
            try
            {
                // Ouvrir le port s�rie de mani�re asynchrone si possible
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }

                // Lire les donn�es du port s�rie (ici, on attend les donn�es de mani�re asynchrone)
                string sensorData = serialPort.ReadLine();

                // V�rifier si les donn�es sont valides
                if ("0" == "0")
                {
                     InsertPassage(); // Ins�rer les donn�es dans la base
                }
                else
                {
                   //await InsertPassage();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la lecture du port s�rie : {ex.Message}");
            }
        }
        

        private async Task HandleSensorData(string sensorData)
        {
            // Si la donn�e re�ue est "LED Allum�e"
            if (!string.IsNullOrEmpty(sensorData) && sensorData != "0")
            {
                await InsertPassage(); // Insertion dans la base de donn�es de mani�re asynchrone

                Console.WriteLine("LED Allum�e d�tect�e");

                // Afficher un message pop-up
                var messageDialog = new Windows.UI.Popups.MessageDialog("LED Allum�e d�tect�e !");
                await messageDialog.ShowAsync();
            }
        }

        private async Task InsertPassage()
        {
            string connectionString = "Host=localhost;Username=postgres;Password=0000;Database=Agenda";

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // D�finir un passage avec des valeurs d'exemple
                    string query = @"
                INSERT INTO Passages (DatePassage, TypePassage, Montant, ModePaiement, Statut, ZoneID)
                VALUES (@DatePassage, @TypePassage, @Montant, @ModePaiement, @Statut, @ZoneID)";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Exemple de valeurs � ins�rer
                        command.Parameters.AddWithValue("DatePassage", DateTime.Now);
                        command.Parameters.AddWithValue("TypePassage", "Payant");  // Exemple: passage payant
                        command.Parameters.AddWithValue("Montant", 10.00);  // Exemple de montant
                        command.Parameters.AddWithValue("ModePaiement", "Carte");  // Mode de paiement
                        command.Parameters.AddWithValue("Statut", "Valid�");  // Statut du passage
                        command.Parameters.AddWithValue("ZoneID", 1);  // Exemple d'ID de zone

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Affichage d'un message ou autre action apr�s l'insertion si n�cessaire
                Console.WriteLine("Nouveau passage enregistr�.");
            }
            catch (Exception ex)
            {
                // En cas d'erreur lors de l'insertion
                Debug.WriteLine($"Erreur de connexion ou d'ex�cution : {ex.Message}");

                // Afficher un message d'erreur dans l'interface utilisateur
                var messageDialog = new Windows.UI.Popups.MessageDialog($"Erreur: {ex.Message}");
                await messageDialog.ShowAsync();
            }
        }


        // Charger les passages depuis la base de donn�es
        private async void ChargerPassages()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT TypePassage, Montant, DatePassage FROM Passages";
                    //string query = "SELECT passage_name, passage_type, toll_amount, timestamp FROM passages";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var passages = new ObservableCollection<Passage>();

                            while (await reader.ReadAsync())
                            {
                                passages.Add(new Passage
                                {
                                    TypePassage = reader.GetString(0),             // TypePassage
                                    Montant = reader.GetDecimal(1),             // Montant (DECIMAL)
                                    DatePassage = reader.GetDateTime(2)
                                });
                            }

                            // Lier les donn�es au ListView
                            clientListView.ItemsSource = passages;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new MessageDialog($"Erreur : {ex.Message}");
                await errorDialog.ShowAsync();
            }
        }


        /*
        private async Task LoadStatisticsAsync()
{
    string connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdatabase"; // Remplacez par vos param�tres de connexion PostgreSQL
    using (var connection = new NpgsqlConnection(connectionString))
    {
        await connection.OpenAsync();

        // Total des passages
        string query1 = "SELECT COUNT(*) FROM Passages";
        var command1 = new NpgsqlCommand(query1, connection);
        var totalPassages = (long)await command1.ExecuteScalarAsync();
        totalPassagesTextBlock.Text = totalPassages.ToString(); // Mettre � jour la case Total des Passages

        // Passages payants
        string query2 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Payant'";
        var command2 = new NpgsqlCommand(query2, connection);
        var paidPassages = (long)await command2.ExecuteScalarAsync();
        paidPassagesTextBlock.Text = paidPassages.ToString(); // Mettre � jour la case Passages Payants

        // Passages non payants
        string query3 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Non payant'";
        var command3 = new NpgsqlCommand(query3, connection);
        var nonPaidPassages = (long)await command3.ExecuteScalarAsync();
        nonPaidPassagesTextBlock.Text = nonPaidPassages.ToString(); // Mettre � jour la case Passages Non Payants

        // Total des montants
        string query4 = "SELECT SUM(Montant) FROM Passages";
        var command4 = new NpgsqlCommand(query4, connection);
        var totalAmount = await command4.ExecuteScalarAsync();
        totalAmountTextBlock.Text = totalAmount.ToString(); // Mettre � jour la case Total des Montants

        // Passages valid�s
        string query5 = "SELECT COUNT(*) FROM Passages WHERE Statut = 'Valid�'";
        var command5 = new NpgsqlCommand(query5, connection);
        var validatedPassages = (long)await command5.ExecuteScalarAsync();
        validatedPassagesTextBlock.Text = validatedPassages.ToString(); // Mettre � jour la case Passages Valid�s

        // Passages rejet�s
        string query6 = "SELECT COUNT(*) FROM Passages WHERE Statut = 'Rejet�'";
        var command6 = new NpgsqlCommand(query6, connection);
        var rejectedPassages = (long)await command6.ExecuteScalarAsync();
        rejectedPassagesTextBlock.Text = rejectedPassages.ToString(); // Mettre � jour la case Passages Rejet�s
    }
}
        
        */
        
        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await LoadStatisticsAsync();

        }

        /*
        private async Task LoadStatisticsAsync()
        {
            string connectionString = "Votre cha�ne de connexion � PostgreSQL";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Total des passages
                string query1 = "SELECT COUNT(*) FROM Passages";
                var command1 = new NpgsqlCommand(query1, connection);
                var totalPassages = (int)await command1.ExecuteScalarAsync();
                Debug.WriteLine($"Total des passages : {totalPassages}");
                totalPassagesTextBlock.Text = totalPassages.ToString();

                // Passages payants
                string query2 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Payant'";
                var command2 = new NpgsqlCommand(query2, connection);
                var paidPassages = (int)await command2.ExecuteScalarAsync();
                paidPassagesTextBlock.Text = paidPassages.ToString();

                // Passages non payants
                string query3 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Non payant'";
                var command3 = new NpgsqlCommand(query3, connection);
                var nonPaidPassages = (int)await command3.ExecuteScalarAsync();
                nonPaidPassagesTextBlock.Text = nonPaidPassages.ToString();

                // Co�t total
                string query4 = "SELECT SUM(Montant) FROM Passages";
                var command4 = new NpgsqlCommand(query4, connection);
                var totalCost = (decimal)await command4.ExecuteScalarAsync();
                totalCostTextBlock.Text = totalCost.ToString("C");

                // Moyenne par jour (exemple)
                string query5 = "SELECT AVG(Montant) FROM Passages";
                var command5 = new NpgsqlCommand(query5, connection);
                var averagePerDay = (decimal)await command5.ExecuteScalarAsync();
                averagePerDayTextBlock.Text = averagePerDay.ToString("C");

                // Variation mois en mois (exemple)
                string query6 = "SELECT EXTRACT(MONTH FROM DatePassage), SUM(Montant) FROM Passages GROUP BY EXTRACT(MONTH FROM DatePassage)";
                var command6 = new NpgsqlCommand(query6, connection);
                var reader = await command6.ExecuteReaderAsync();
                decimal lastMonth = 0;
                decimal currentMonth = 0;
                while (await reader.ReadAsync())
                {
                    if (reader.GetInt32(0) == DateTime.Now.Month - 1)
                        lastMonth = reader.GetDecimal(1);
                    else if (reader.GetInt32(0) == DateTime.Now.Month)
                        currentMonth = reader.GetDecimal(1);
                }
                var monthToMonthVariation = lastMonth == 0 ? 0 : ((currentMonth - lastMonth) / lastMonth) * 100;
                monthToMonthVariationTextBlock.Text = $"{monthToMonthVariation:F2}%";
            }
        }
        */

        private async Task LoadStatisticsAsync()
        {
            string connectionString = "Host=localhost;Username=postgres;Password=0000;Database=Agenda";

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Total des passages
                    string query1 = "SELECT COUNT(*) FROM Passages";
                    var command1 = new NpgsqlCommand(query1, connection);
                    var totalPassages = (long)await command1.ExecuteScalarAsync();
                    Debug.WriteLine($"Total des passages : {totalPassages}");
                    totalPassagesTextBlock.Text = totalPassages.ToString();

                    // Passages payants
                    string query2 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Payant'";
                    var command2 = new NpgsqlCommand(query2, connection);
                    var paidPassages = (long)await command2.ExecuteScalarAsync();
                    paidPassagesTextBlock.Text = paidPassages.ToString();

                    // Passages non payants
                    string query3 = "SELECT COUNT(*) FROM Passages WHERE TypePassage = 'Non payant'";
                    var command3 = new NpgsqlCommand(query3, connection);
                    var nonPaidPassages = (long)await command3.ExecuteScalarAsync();
                    nonPaidPassagesTextBlock.Text = nonPaidPassages.ToString();

                    // Co�t total
                    string query4 = "SELECT SUM(Montant) FROM Passages";
                    var command4 = new NpgsqlCommand(query4, connection);
                    var totalCost = (decimal)await command4.ExecuteScalarAsync();
                    totalCostTextBlock.Text = totalCost.ToString("C");

                    // Moyenne par jour (exemple)
                    string query5 = "SELECT AVG(Montant) FROM Passages";
                    var command5 = new NpgsqlCommand(query5, connection);
                    var averagePerDay = (decimal)await command5.ExecuteScalarAsync();
                    averagePerDayTextBlock.Text = averagePerDay.ToString("C");

                    // Variation mois en mois (exemple)
                    string query6 = "SELECT EXTRACT(MONTH FROM DatePassage), SUM(Montant) FROM Passages GROUP BY EXTRACT(MONTH FROM DatePassage)";
                    var command6 = new NpgsqlCommand(query6, connection);
                    var reader = await command6.ExecuteReaderAsync();
                    decimal lastMonth = 0;
                    decimal currentMonth = 0;
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetInt32(0) == DateTime.Now.Month - 1)
                            lastMonth = reader.GetDecimal(1);
                        else if (reader.GetInt32(0) == DateTime.Now.Month)
                            currentMonth = reader.GetDecimal(1); 
                    }
                    var monthToMonthVariation = lastMonth == 0 ? 0 : ((currentMonth - lastMonth) / lastMonth) * 100;
                    monthToMonthVariationTextBlock.Text = $"{monthToMonthVariation:F2}%";

                    // Calcul des pourcentages pour les barres de progression
                    if (totalPassages > 0)
                    {
                        double paidPercentage = (double)paidPassages / totalPassages * 100;
                        double nonPaidPercentage = (double)nonPaidPassages / totalPassages * 100;

                        // Mise � jour des barres de progression
                        paidPassagesBar.Value = paidPercentage;
                        nonPaidPassagesBar.Value = nonPaidPercentage;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur de connexion ou d'ex�cution : {ex.Message}");
                var errorDialog = new MessageDialog($"Erreur : {ex.Message}");
                await errorDialog.ShowAsync();
            }
        }

        ///////////////////////////
        ///
       

    }
}
