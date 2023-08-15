using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;


namespace SuggestiveContentDetector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        [HttpPost(Name = "analyze-image")]
        public async Task<IActionResult> AnalyzeImage(IFormFile imageFile)
        {
            try
            {
                // Check if the request contains a file
                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest(new { Error = "No image file found in the request." });
                }

                // Check if the uploaded file is an image
                if (!IsImageFile(imageFile))
                {
                    return BadRequest(new { Error = "Please upload a valid image file (JPEG, PNG, or GIF)." });
                }

                // Call the method to analyze the image using Amazon Rekognition
                float probability = await AnalyzeImageUsingRekognition(imageFile.OpenReadStream());

                return Ok(new { Probability = probability });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An error occurred: " + ex.Message });
            }
        }

        // Helper method to check if the uploaded file is an image
        private bool IsImageFile(IFormFile file)
        {
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            string fileExtension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(fileExtension);
        }

        // Helper method to analyze the image using Amazon Rekognition
        private async Task<float> AnalyzeImageUsingRekognition(Stream imageStream)
        {

            AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient("", "", RegionEndpoint.USEast1);

            Image image = new Image
            {
                Bytes = ReadFully(imageStream)
            };

            DetectModerationLabelsRequest request = new DetectModerationLabelsRequest
            {
                Image = image
            };

            DetectModerationLabelsResponse response = await rekognitionClient.DetectModerationLabelsAsync(request);

            // Assuming the API response contains the percentage of suggestive content
            float probability = 0.0f;
            if (response.ModerationLabels.Count > 0)
            {
                probability = response.ModerationLabels[0].Confidence;
            }

            return probability;
        }

        // Helper method to read the image stream into a byte array
        private MemoryStream ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms;
            }
        }

    }
}
