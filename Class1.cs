using System;
using System.IO.Ports;
using System.Data.SqlClient;

class Program
{
    static void Mainmenu(string[] args)
    {
        // Initialisation du port série
        SerialPort serialPort = new SerialPort("COM7", 9600); // Remplacez "COM3" par le port correct
        serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        serialPort.Open();

        Console.WriteLine("En attente des données GPS...");
        Console.ReadLine(); // Maintient l'application active
        serialPort.Close();
    }

    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        string data = sp.ReadLine(); // Lire une ligne complète
        Console.WriteLine($"Données reçues : {data}");

        // Supposons que les données sont envoyées au format "latitude: xx.xxxx, longitude: yy.yyyy"
        string[] parts = data.Split(',');
        if (parts.Length == 2)
        {
            string latitude = parts[0].Split(':')[1].Trim();
            string longitude = parts[1].Split(':')[1].Trim();

            Console.WriteLine($"Latitude : {latitude}");
            Console.WriteLine($"Longitude : {longitude}");

            // Insérer les données dans la base de données
            InsertIntoDatabase(latitude, longitude);
        }
    }

    private static void InsertIntoDatabase(string latitude, string longitude)
    {
        // Chaîne de connexion à votre base de données
        string connectionString = "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = "INSERT INTO GPSData (Latitude, Longitude) VALUES (@Latitude, @Longitude)";
            SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Latitude", latitude);
            command.Parameters.AddWithValue("@Longitude", longitude);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                Console.WriteLine("Données insérées dans la base de données avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'insertion dans la base de données : {ex.Message}");
            }
        }
    }
}
