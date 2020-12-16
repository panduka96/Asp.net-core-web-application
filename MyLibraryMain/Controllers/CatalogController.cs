using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyLibraryMain.Models.Catalog;
using MyLibraryMain.Models.Checkouts;
using MyLibraryMainData;

namespace MyLibraryMain.Controllers
{
    public class CatalogController : Controller
    {

        private ILibraryAsset _assets;
        private ICheckout _checkouts;

        public CatalogController(ILibraryAsset asset, ICheckout checkouts)
        {
            _assets = asset;
            _checkouts = checkouts;
        }

        public IActionResult Index()
        {
            var assetModels = _assets.GetAll();

            var listingResult = assetModels
                .Select(Result => new AssetIndexListingModel {

                    Id = Result.Id,
                    ImageUrl = Result.ImageUrl,
                    AuthorOrDirector = _assets.GetAuthorOrDirector(Result.Id),
                    DeweyCallNumber = _assets.GetDeweyIndex(Result.Id),
                    Title = Result.Title,
                    Type = _assets.GetType(Result.Id)

                });

            var model = new AssetIndexModel()
            {
                Assets = listingResult
            };

            return View(model);
        }


        public IActionResult Detail(int id)
        {

            var asset = _assets.GetById(id);

            var currentHolds = _checkouts.GetCurrentHolds(id)
                .Select(a => new AssetHoldModel()
                {
                    HoldPlaced = _checkouts.GetCurrentHoldPlaced(a.Id),
                    PatronName = _checkouts.GetCurrentHoldPatronName(a.Id)
                });

            var model = new AssetDetailModel()
            {
                AssetId = id,
                Title = asset.Title,
                Type = _assets.GetType(id),
                Year = asset.Year,
                Cost = asset.Cost,
                Status = asset.Status.Name,
                ImageUrl = asset.ImageUrl,
                AuthorOrDirector = _assets.GetAuthorOrDirector(id),
                CurrentLocation = _assets.GetCurrentLocation(id).Name,
                DeweyCallNumber = _assets.GetDeweyIndex(id),
                CheckoutHistory = _checkouts.GetCheckoutHistory(id),
                ISBN = _assets.GetIsbn(id),
                Latestcheckout = _checkouts.GetLatestCheckout(id),
                PatronName = _checkouts.GetCurrentCheckoutPatron(id),
                CurrentHold = currentHolds

            };

            return View(model);
        }


        public IActionResult CheckIn(int id)
        {
            _checkouts.CheckInItem(id);

            return RedirectToAction("Detail", new { id = id });
        }


        public IActionResult Checkout(int id)
        {
            var asset = _assets.GetById(id);

            var model = new CheckoutModel()
            {
                AssetId = id,
                ImageUrl = asset.ImageUrl,
                Title = asset.Title,
                LibraryCardId = "",
                IsCheckedOut = _checkouts.IsCheckedOut(id)
            };

            return View(model);
        }

        public IActionResult Hold(int id)
        {
            var asset = _assets.GetById(id);

            var model = new CheckoutModel()
            {
                AssetId = id,
                ImageUrl = asset.ImageUrl,
                Title = asset.Title,
                LibraryCardId = "",
                IsCheckedOut = _checkouts.IsCheckedOut(id),
                HoldCount = _checkouts.GetCurrentHolds(id).Count()
            };

            return View(model);
        }


        public IActionResult MarkLost(int assetId)
        {
            _checkouts.MarkLost(assetId);

            return RedirectToAction("Detail", new { id = assetId });
        }


        [HttpPost]
        public IActionResult PlaceCheckout(int assetId,int LibraryCardId)
        {

            _checkouts.CheckOutItem(assetId, LibraryCardId);

            return RedirectToAction("Detail", new { id = assetId });
        }

        [HttpPost]
        public IActionResult PlaceHold(int assetId, int LibraryCardId)
        {

            _checkouts.PlaceHold(assetId, LibraryCardId);

            return RedirectToAction("Detail", new { id = assetId });
        }


    }
}