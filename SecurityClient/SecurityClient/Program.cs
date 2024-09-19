using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Concurrent;

class SecurityClient
{
    private static readonly string directoryPath = @"C:\Windows\System32";
    private static readonly string encryptionKey; // Usunięto inicjalizację
    private static readonly HttpClient httpClient = new HttpClient();

    static SecurityClient()
    {
        encryptionKey = GenerateRandomKey(); // Generowanie klucza w statycznym konstruktorze

        httpClient.DefaultRequestHeaders.ExpectContinue = false;
        System.Net.ServicePointManager.ServerCertificateValidationCallback =
            (sender, cert, chain, sslPolicyErrors) => true;
    }

    private static string GenerateRandomKey()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] key = new byte[16]; // 128 bitów
            rng.GetBytes(key);
            return Convert.ToBase64String(key); // Zwraca klucz w formacie Base64
        }
    }

    static async Task Main(string[] args)
    {
        // Wylistowanie plików
        var files = Directory.GetFiles(directoryPath);

        if (files.Length == 0)
        {
            Console.WriteLine("No files found in the directory.");
            return; // Zakończ program, jeśli brak plików
        }

        Console.WriteLine($"Found {files.Length} files in {directoryPath}");

        // Kolekcja do przechowywania sum kontrolnych
        var checksumResults = new ConcurrentDictionary<string, string>();

        // Obliczanie sum kontrolnych z zastosowaniem wielowątkowości
        Parallel.ForEach(files, (file) =>
        {
            try
            {
                Console.WriteLine($"Processing file: {file}");

                if (File.Exists(file))
                {
                    string checksum = ComputeChecksum(file);
                    checksumResults[file] = checksum;
                    Console.WriteLine($"Checksum for {file}: {checksum}");
                }
                else
                {
                    Console.WriteLine($"File does not exist: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        });

        if (checksumResults.Count > 0)
        {
            // Szyfrowanie danych
            string encryptedData = EncryptData(string.Join("\n", checksumResults.Select(kv => $"{kv.Key}:{kv.Value}")));

            // Wysłanie danych na serwer
            await SendToServerAsync(encryptedData);
        }
        else
        {
            Console.WriteLine("No checksums to send, possibly due to errors or no files found.");
        }
    }

    private static string ComputeChecksum(string filePath)
    {
        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "");
        }
    }

    private static string EncryptData(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Convert.FromBase64String(encryptionKey); // Użycie klucza w formacie Base64
            aes.IV = new byte[16]; // 16 bajtów dla AES-128

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
                sw.Close();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static async Task SendToServerAsync(string encryptedData)
    {
        try
        {
            var content = new StringContent(encryptedData, Encoding.UTF8, "application/json");

            Console.WriteLine("Attempting to send data...");

            HttpResponseMessage response = await httpClient.PostAsync("https://localhost:7233", content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Data sent successfully.");
            }
            else
            {
                // Pobieranie treści odpowiedzi w przypadku niepowodzenia
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error sending data. Status code: {response.StatusCode}, Response: {responseContent}");
            }
        }
        catch (HttpRequestException e)
        {
            // Obsługa błędów związanych z siecią
            Console.WriteLine($"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            // Obsługa ogólnych błędów
            Console.WriteLine($"General error: {e.Message}");
        }
    }
}
