// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

class Program
{
    static HttpClient httpClient = new HttpClient();

    static int Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.White;

        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("URL argument needed.");
            Console.ResetColor();
            return 1;
        }

        string uri = args[1];
        if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Invalid URL format.");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"This is what I got: {uri}\n");
        var parsedUri = new Uri(uri);
        var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);
        string fileId = queryParams["fileId"];
        string authToken = queryParams["auth"];

        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(authToken))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("FileId and Auth token are required.");
            Console.ResetColor();
            return 1;
        }

        string filePath = DownloadFile(fileId, authToken).GetAwaiter().GetResult();
        if (filePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Failed to download file.");
            Console.ResetColor();
            return 1;
        }

        ProcessFile(filePath);

        if (FileHasBeenModified(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File has been modified, uploading...");
            bool uploadSuccess = UploadFile(filePath, authToken).GetAwaiter().GetResult();
            if (uploadSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("File uploaded successfully.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Failed to upload file.");
                Console.ResetColor();
                return 1;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("No changes detected in the file.");
        }

        Console.ResetColor();
        return 0;
    }


    static async Task<string> DownloadFile(string fileId, string authToken)
    {
        string downloadUrl = $"https://mekky.com/download?fileId={fileId}&auth={authToken}";
        try
        {
            var response = await httpClient.GetAsync(downloadUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string fileContent = await response.Content.ReadAsStringAsync();
            string filePath = Path.Combine(Path.GetTempPath(), fileId);
            File.WriteAllText(filePath, fileContent);
            return filePath;
        }
        catch
        {
            return null;
        }
    }

    static DateTime lastWriteTimeBeforeProcessing;

    static void ProcessFile(string filePath)
    {
        lastWriteTimeBeforeProcessing = File.GetLastWriteTime(filePath);

        try
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = filePath
            };
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file: {ex.Message}");
        }
    }

    static bool FileHasBeenModified(string filePath)
    {
        DateTime lastWriteTimeAfterProcessing = File.GetLastWriteTime(filePath);
        return lastWriteTimeBeforeProcessing != lastWriteTimeAfterProcessing;
    }


    static async Task<bool> UploadFile(string filePath, string authToken)
    {
        // endpoint to be created
        string uploadUrl = $"https://mekky.com/upload?auth={authToken}";
        try
        {
            var fileContent = new StringContent(File.ReadAllText(filePath));
            var response = await httpClient.PostAsync(uploadUrl, fileContent);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
