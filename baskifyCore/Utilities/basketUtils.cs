using baskifyCore.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace baskifyCore.Utilities
{
    public static class basketUtils
    {

        /// <summary>
        /// It's VERY important that the model inputted is already tracked by the dbContext!
        /// Does not save changes to the context
        /// </summary>
        /// <param name="basket"></param>
        /// <returns></returns>
        public static bool deleteBasket(BasketModel basket, ApplicationDbContext _context, string webrootPath)
        {
            try
            {
                _context.Entry(basket).Collection(b => b.photos).Load();
                foreach(var photo in basket.photos)
                {
                    imageUtilities.deleteFile(photo.Url, webrootPath); //deletes all associated photos, to save space
                }
                _context.BasketModel.Remove(basket);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Tries to update the given basket, returns false if the model is no good, true otherwise
        /// </summary>
        /// <param name="dbBasket"></param>
        /// <param name="updatedBasket"></param>
        /// <param name="_context"></param>
        /// <param name="ModelState"></param>
        /// <param name="WebRootPath"></param>
        /// <returns></returns>
        public static bool updateBasket(BasketModel dbBasket, BasketModel updatedBasket, ApplicationDbContext _context, ModelStateDictionary ModelState, string WebRootPath)
        {
            _context.Entry(dbBasket).Collection(b => b.photos).Load(); //load photos in
            updatedBasket.photos = dbBasket.photos; //to satisfy the galleria viewer
            updatedBasket.AcceptedByOrg = dbBasket.AcceptedByOrg; //to keep things from getting weird
            _context.Entry(dbBasket.AuctionModel).Collection(a => a.Baskets).Load();

            if ((updatedBasket.addImages == null ? 0 : updatedBasket.addImages.Count) - (updatedBasket.removeImages == null ? 0 : updatedBasket.removeImages.Count) + dbBasket.photos.Count <= 0)
                ModelState.AddModelError(string.Empty, "You must have at least one photo for the basket");

            if (!ModelState.IsValid)
            {
                if (updatedBasket.addImages != null) //to keep photos changes, load them into the model
                {
                    foreach (var photoUrl in updatedBasket.addImages)
                    {
                        if (updatedBasket.removeImages == null || !updatedBasket.removeImages.Contains(photoUrl))
                            updatedBasket.photos.Add(new BasketPhotoModel() { Url = photoUrl });
                    }
                }

                return false;
            }

            dbBasket.Draft = false; //safe no matter when...

            var imageSet = new SortedSet<string>();
            if (updatedBasket.addImages != null)
            {
                foreach (var imageUrl in updatedBasket.addImages)
                {
                    var pendingModel = _context.PendingImageModel.Find(imageUrl);
                    if (pendingModel != null) //only let the user gain delete control of the image if they added it in the first place
                    {
                        if (updatedBasket.removeImages != null && updatedBasket.removeImages.Contains(imageUrl)) //don't add any images that the user removed, just delete them from DB
                        {
                            _context.PendingImageModel.Remove(pendingModel);
                            imageUtilities.deleteFile(imageUrl, WebRootPath);
                        }
                        else
                        {
                            _context.PendingImageModel.Remove(pendingModel);
                            var basketImageModel = new BasketPhotoModel() { BasketId = dbBasket.BasketId, Url = imageUrl }; //add image to basket officially
                            _context.BasketPhotoModel.Add(basketImageModel);
                        }
                    }
                }
            }

            if (updatedBasket.removeImages != null)
            {
                foreach (var photo in dbBasket.photos.ToList())
                {
                    if (updatedBasket.removeImages.Contains(photo.Url))
                    {
                        _context.BasketPhotoModel.Remove(photo); //remove any photos attributed to the basket that were requested
                        imageUtilities.deleteFile(photo.Url, WebRootPath);
                    }
                }
            }

            //now any desired photos have been added and removed, lets update other stuff
            updatedBasket.BasketContents.RemoveAll(m => string.IsNullOrWhiteSpace(m));
            updatedBasket.BasketContents.ForEach(e => e = HttpUtility.HtmlEncode(e)); //clean the contents of bad chars and whitespace

            dbBasket.BasketContents = updatedBasket.BasketContents;
            dbBasket.BasketDescription = updatedBasket.BasketDescription.Replace(">", String.Empty)
                    .Replace("<", String.Empty)
                    .Replace("\'", String.Empty)
                    .Replace("\"", String.Empty)
                    .Replace("&", "and"); //avoid injection
            dbBasket.BasketTitle = updatedBasket.BasketTitle;

            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// User should be tracked by context, does NOT verify auctionID exists.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        /// <param name="auctionId"></param>
        /// <returns>A draft that is valid but NO LONGER tracked by EF</returns>
        public static BasketModel getDraftBasket(UserModel user, ApplicationDbContext _context, int auctionId)
        {
            var draft = new BasketModel()
            {
                AuctionId = auctionId,
                SubmittingUsername = user.Username,
                BasketDescription = "Give your basket a description!",
                BasketTitle = "Provide a title",
                BasketContents = new List<string>() { "List the contents here!" },
                SubmissionDate = DateTime.UtcNow,
                Draft = true
            };
            _context.BasketModel.Add(draft);
            _context.SaveChanges(); //now we have updated/created a draft...
            _context.Entry(draft).Reload(); //gets the id
            _context.Entry(draft).Collection(b => b.photos).Load(); //to make everything happy

            Task.Run(() => deleteDrafts(user, draft.BasketId, _context)); //asynchronously deletes drafts

            _context.Entry(draft).State = System.Data.Entity.EntityState.Detached; //stops tracking the draft
            //clear the lines
            //this doesn't get passed to the DB, just makes the basket cleaner
            draft.BasketTitle = String.Empty;
            draft.BasketContents = null;
            draft.BasketDescription = String.Empty;
            return draft;
        }

        public static void deleteDrafts(UserModel user, int omitId, ApplicationDbContext _context) //omits the given basket id
        {
            _context.BasketModel.RemoveRange(_context.BasketModel.Where(b => b.SubmittingUsername == user.Username).Where(b => b.Draft).Where(b => b.BasketId != omitId));
            _context.SaveChanges();
        }
    }
}
