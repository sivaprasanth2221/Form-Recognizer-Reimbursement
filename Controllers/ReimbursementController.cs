using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Form_Recognizer_Reimbursement.Controllers
{
    [ApiController]
    [Route("api/reimbursement")]
    public class ReimbursementController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ReimbursementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeReceipt([FromForm] ReceiptUploadRequest request)
        {
            var endpoint = _configuration["FormRecognizer:Endpoint"];
            var apiKey = _configuration["FormRecognizer:Key"];

            var client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

            using var stream = request.Receipt.OpenReadStream();

            var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            var result = operation.Value;

            var form = new ReimbursementForm();

            foreach (var doc in result.Documents)
            {
                foreach (var field in doc.Fields)
                {
                    var fieldName = field.Key.ToLower();
                    var content = field.Value.Content;

                    if (fieldName.Contains("name") && string.IsNullOrWhiteSpace(form.StudentName))
                        form.StudentName = content;
                    else if (fieldName.Contains("school"))
                        form.SchoolName = content;
                    else if (fieldName.Contains("receipt"))
                        form.ReceiptNumber = content;
                    else if (fieldName.Contains("amount") || fieldName.Contains("total"))
                        form.TotalFeesPaid = content;
                    else if (fieldName.Contains("date"))
                        form.PaymentDate = content;
                }
            }

            return Ok(new { form });
        }
    }

    public class ReceiptUploadRequest
    {
        [Required]
        [FromForm(Name = "receipt")]
        public IFormFile Receipt { get; set; }
    }

    public class ReimbursementForm
    {
        public string StudentName { get; set; }
        public string SchoolName { get; set; }
        public string AcademicYear { get; set; } // Optional: can be manually set
        public string TotalFeesPaid { get; set; }
        public string PaymentDate { get; set; }
        public string ReceiptNumber { get; set; }
    }
}
