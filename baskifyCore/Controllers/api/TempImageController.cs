using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TempImageController : ControllerBase
    {
        ApplicationDbContext _context;
        public TempImageController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpPost]
        [Route("icon")]
        public ActionResult Icon([FromForm] IFormFile image)
        {
            if (image == null)
                return BadRequest("No image provided");

            if (!image.ContentType.StartsWith("image")) //only images
                return BadRequest("Invalid type");

            var stream = image.OpenReadStream();
            var bmp = new Bitmap(stream);
            var resizedPhoto = imageUtilities.ResizePhoto(bmp, 500, 500);

            var outputStream = new MemoryStream();
            resizedPhoto.Save(outputStream, ImageFormat.Jpeg);

            var binary = outputStream.ToArray();
            //save to db
            var model = new TempImageModel()
            {
                Content = binary
            };
            _context.TempImage.Add(model);
            _context.SaveChanges();

            DeleteOldImages(); //async removes old images, self maintaining

            return Ok("/api/tempimage/" + model.Id);
        }

        /// <summary>
        /// Gets a temporary image
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public ActionResult Index(int id)
        {
            var image = _context.TempImage.Find(id);
            if (image == null)
                return NotFound();

            return File(image.Content, "image/jpeg");
        }

        /// <summary>
        /// Deletes images older than 4 hours.
        /// </summary>
        async private void DeleteOldImages()
        {
            var oldestTime = DateTime.UtcNow.AddHours(-4);
            _context.TempImage.RemoveRange(_context.TempImage.Where(m => m.DateCreated < oldestTime));
            await _context.SaveChangesAsync();
        }

    }
}