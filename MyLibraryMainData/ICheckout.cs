using MyLibraryMainData.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyLibraryMainData
{
   public interface ICheckout
    {
        IEnumerable<Checkout> GetAll();

        Checkout GetById(int checkoutId);

        void Add(Checkout newCheckout);

        void CheckOutItem(int assetId, int libraryCardId);

        void CheckInItem(int assetId);

        IEnumerable<CheckoutHistory> GetCheckoutHistory(int id);

        void PlaceHold(int assetId, int libraryCardId);

        string GetCurrentHoldPatronName(int id);

        DateTime GetCurrentHoldPlaced(int id);

        IEnumerable<Hold> GetCurrentHolds(int id);

        void MarkLost(int assetId);

        void MarkFound(int assetId);

        Checkout GetLatestCheckout(int assetId);

        string GetCurrentCheckoutPatron(int assetId);

        bool IsCheckedOut(int id);
    }
}
