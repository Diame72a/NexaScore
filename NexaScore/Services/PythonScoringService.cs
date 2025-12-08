using System.Diagnostics;
using System.Text.Json;

namespace Projet.Services
{
    public class ScoringResult
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string Message { get; set; }

        public List<string> Matches { get; set; } = new List<string>();
    }

    public class PythonScoringService
    {

        private const string PYTHON_EXE_PATH = @"C:\Users\axel\AppData\Local\Programs\Python\Python313\python.exe";

        private const string SCRIPT_NAME = "scoring.py";

        public ScoringResult CalculerScore(string cheminPdf, string descriptionOffre)
        {

            string projectRoot = Directory.GetCurrentDirectory();
            string scriptPath = Path.Combine(projectRoot, "Script", SCRIPT_NAME);

            if (!File.Exists(scriptPath))
            {
                return new ScoringResult { Success = false, Message = $"Script introuvable : {scriptPath}" };
            }

            var start = new ProcessStartInfo
            {
                FileName = PYTHON_EXE_PATH,
                Arguments = $"\"{scriptPath}\" \"{cheminPdf}\" \"{descriptionOffre.Replace("\"", "'")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            try
            {
                using (var process = Process.Start(start))
                {
                    string result = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                        return new ScoringResult { Success = false, Message = "Erreur Python : " + error };

                    if (string.IsNullOrEmpty(result))
                        return new ScoringResult { Success = false, Message = "Retour vide de Python." };

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    try
                    {
                        return JsonSerializer.Deserialize<ScoringResult>(result, options);
                    }
                    catch
                    {
                        return new ScoringResult { Success = false, Message = "JSON Invalide : " + result };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ScoringResult { Success = false, Message = "Erreur C# : " + ex.Message };
            }
        }
    }
}