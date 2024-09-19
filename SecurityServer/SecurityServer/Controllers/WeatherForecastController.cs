using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

[ApiController]
[Route("api/security")]


public class SecurityController : ControllerBase
{
    private static readonly string encryptionKey = GenerateRandomKey(); // Klucz szyfruj¹cy
    private static readonly string checksumFilePath = @"C:\checksums.txt"; // Plik do przechowywania poprzednich sum kontrolnych

    private static string GenerateRandomKey()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] key = new byte[16]; // 128 bitów
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] string encryptedData)
    {
        // Odbieranie danych i deszyfrowanie
        string decryptedData = DecryptData(encryptedData);

        // Porównanie z poprzednimi sumami kontrolnymi
        bool discrepancies = CompareChecksums(decryptedData);

        if (discrepancies)
        {
            LogDiscrepancy();
            return Ok("Discrepancy detected.");
        }

        return Ok("No discrepancy.");
    }

    private static string DecryptData(string encryptedText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Convert.FromBase64String(encryptionKey); // U¿yj klucza w formacie Base64
            aes.IV = new byte[16]; // 16 bajtów dla AES-128

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    private static bool CompareChecksums(string decryptedData)
    {
        if (!System.IO.File.Exists(checksumFilePath))
        {
            // Jeœli nie ma poprzednich sum kontrolnych, zapisujemy aktualne
            System.IO.File.WriteAllText(checksumFilePath, decryptedData);
            return false;
        }

        string previousData = System.IO.File.ReadAllText(checksumFilePath);

        if (previousData != decryptedData)
        {
            // Wykryto rozbie¿noœæ
            return true;
        }

        return false;
    }

    private static void LogDiscrepancy()
    {
        System.IO.File.AppendAllText(@"C:\SecurityLog.txt", $"Discrepancy detected: {DateTime.Now}\n");
    }
}
