using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Forum.Models.Risibank;
using Forum.Data.Options;

namespace Forum.Controllers
{
    [Route("api/[controller]")]
    public class StickerController : Controller
    {
        private readonly ImgurKeys _options;

        public StickerController(IOptions<ImgurKeys> optionsAccessor)
        {
            _options = optionsAccessor.Value;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> LoadRisibank()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://risibank.fr/api/v0/load");
            request.Method = "POST";

            try
            {
                var response = await request.GetResponseAsync();
                var responseString = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();

                return Json(responseString);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SearchRisibank(RisibankModel model)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.risibank.fr/api/v0/search?search=" + model.Search);
            request.Method = "POST";

            try
            {
                var response = await request.GetResponseAsync();
                var responseString = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();

                return Json(responseString);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetStickerByNS(RisibankModel model)
        {

            var request = (HttpWebRequest)WebRequest.Create("https://api.risibank.fr/api/v0/getstickerbyns?link=" + model.Link);
            request.Method = "POST";

            try
            {
                var response = await request.GetResponseAsync();
                var responseString = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();

                return Json(responseString);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        /// <summary>
        /// Téléverse une image avec l'API imgur 
        /// </summary>
        /// <param name="file">Fichier à mettre en ligne</param>
        /// <returns>Un objet JSON avec le résultat de la mise en ligne</returns>
        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImgurImage(IFormFile file)
        {
            byte[] imageData;

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                imageData = memoryStream.ToArray();
            }

            try
            {
                var uploadRequestString = JsonConvert.SerializeObject(imageData);
                var webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/image");

                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers["Authorization"] = $"Client-ID {_options.ClientId}";
                webRequest.ServicePoint.Expect100Continue = false;

                var streamWriter = new StreamWriter(await webRequest.GetRequestStreamAsync());

                await streamWriter.WriteAsync(uploadRequestString);
                streamWriter.Close();

                var response = await webRequest.GetResponseAsync();
                var responseStream = response.GetResponseStream();
                var responseReader = new StreamReader(responseStream);
                var responseString = await responseReader.ReadToEndAsync();

                return Json(new { result = responseString });
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }
    }
}