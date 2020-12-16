using Microsoft.EntityFrameworkCore;
using MyLibraryMainData;
using MyLibraryMainData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLibraryMainServices
{
    public class CheckoutService : ICheckout
    {

        private LibraryContext _context;

        public CheckoutService(LibraryContext context)
        {
            _context = context;
        }

        public void Add(Checkout newCheckout)
        {
            _context.Add(newCheckout);
            _context.SaveChanges();
        }

        public void CheckInItem(int assetId)
        {
            var now = DateTime.Now;

            var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);

            RemoveExistingCheckouts(assetId);

            CloseExistingCheckouts(assetId, now);

            var currentHolds = _context.Holds
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .Where(h => h.LibraryAsset.Id == assetId);

            if (currentHolds.Any())
            {
                CheckoutToEarliestHold(assetId, currentHolds);
                return;
            }

            updateAssetStatus(assetId, "Available");

            _context.SaveChanges();

        }

        private void CheckoutToEarliestHold(int assetId, IQueryable<Hold> currentHolds)
        {
            var earliestHold = currentHolds.OrderBy(hold => hold.HoldPlaced).FirstOrDefault();

            var card = earliestHold.LibraryCard;

            _context.Remove(earliestHold);

            _context.SaveChanges();

            CheckOutItem(assetId, card.Id);
        }

        public void CheckOutItem(int assetId, int libraryCardId)
        {
            if (IsCheckedOut(assetId))
            {
                return;
            }

            var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

            updateAssetStatus(assetId, "Checked Out");

            var librarycard = _context.LibraryCards.Include(card => card.Checkouts).FirstOrDefault(card => card.Id == libraryCardId);

            var now = DateTime.Now;

            var checkout = new Checkout
            {
                LibraryAsset = item,
                LibraryCard = librarycard,
                Since = now,
                Until = GetDefaultCheckoutTime(now)
            };

            _context.Add(checkout);

            var checkouthistory = new CheckoutHistory
            {
                Checkedout = now,
                LibraryAsset = item,
                LibraryCard = librarycard
            };

            _context.Add(checkouthistory);
            _context.SaveChanges();

        }

        private DateTime GetDefaultCheckoutTime(DateTime now)
        {
            return now.AddDays(30);
        }

        public bool IsCheckedOut(int assetId)
        {
            return _context.Checkouts.Where(co => co.LibraryAsset.Id == assetId).Any();
        }

        public IEnumerable<Checkout> GetAll()
        {
            return _context.Checkouts;
        }

        public Checkout GetById(int checkoutId)
        {
            return GetAll().FirstOrDefault(checkout => checkout.Id == checkoutId);
        }

        public IEnumerable<CheckoutHistory> GetCheckoutHistory(int id)
        {
            return _context.CheckoutHistories
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .Where(h => h.LibraryAsset.Id == id);
        }

      
        public IEnumerable<Hold> GetCurrentHolds(int id)
        {
            return _context.Holds
                .Include(h => h.LibraryAsset)
                .Where(h => h.LibraryAsset.Id == id);
        }

        public Checkout GetLatestCheckout(int assetId)
        {
            return _context.Checkouts
                .Where(c => c.LibraryAsset.Id == assetId)
                .OrderByDescending(c => c.Since)
                .FirstOrDefault();
        }

        public void MarkFound(int assetId)
        {
            var now = DateTime.Now;

            updateAssetStatus(assetId, "Available");

            RemoveExistingCheckouts(assetId);

            CloseExistingCheckouts(assetId,now);

            _context.SaveChanges(); 

        }

        private void updateAssetStatus(int assetId, string v)
        {
            var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses.FirstOrDefault(status => status.Name == v);
        }

        private void CloseExistingCheckouts(int assetId, DateTime now)
        {
            
            var history = _context.CheckoutHistories
                .FirstOrDefault(h => h.LibraryAsset.Id == assetId && h.CheckedIn == null);

            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }
        }

        private void RemoveExistingCheckouts(int assetId)
        {
            var checkout = _context.Checkouts
                .FirstOrDefault(co => co.LibraryAsset.Id == assetId);

            if (checkout != null)
            {
                _context.Remove(checkout);
            }
        }

        public void MarkLost(int assetId)
        {
            updateAssetStatus(assetId, "Lost");

            _context.SaveChanges();
        }

        public void PlaceHold(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var asset = _context.LibraryAssets
                .Include(a => a.Status)
                .FirstOrDefault(a => a.Id == assetId);

            var card = _context.LibraryCards.FirstOrDefault(c => c.Id == libraryCardId);

            if(asset.Status.Name == "Available")
            {
                updateAssetStatus(assetId,"On Hold");
            }

            var hold = new Hold
            {
                HoldPlaced = now,
                LibraryAsset = asset,
                LibraryCard = card

            };

            _context.Add(hold);
            _context.SaveChanges();

        }

        public string GetCurrentHoldPatronName(int holdId)
        {
            var hold = _context.Holds
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .FirstOrDefault(h => h.Id == holdId);

            var cardId = hold?.LibraryCard.Id;

            var patron = _context.Patrons.Include(p => p.LibraryCard)
                .FirstOrDefault(p => p.LibraryCard.Id == cardId);

            return patron?.FirstName + " " + patron?.LastName;

              
        }

        public DateTime GetCurrentHoldPlaced(int holdId)
        {
           return _context.Holds
               .Include(h => h.LibraryAsset)
               .Include(h => h.LibraryCard)
               .FirstOrDefault(h => h.Id == holdId)
               .HoldPlaced;
        }

        public string GetCurrentCheckoutPatron(int assetId)
        {

            var checkout = GetCheckoutByAssetId(assetId);

           if(checkout == null)
            {
                return "Not Checked Out";
            }

            var cardId = checkout.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(p => p.LibraryCard)
                .FirstOrDefault(p => p.LibraryCard.Id == cardId);

            return patron.FirstName + " " + patron.LastName;

        }

        private Checkout GetCheckoutByAssetId(int assetId)
        {
            return _context.Checkouts
                .Include(co => co.LibraryAsset)
                .Include(co => co.LibraryCard)
                .FirstOrDefault(co => co.LibraryAsset.Id == assetId);

        }

      
    }
}
