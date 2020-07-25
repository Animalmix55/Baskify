using Microsoft.VisualStudio.TestTools.UnitTesting;
using baskifyCore;

namespace BaskifyTests
{
    [TestClass]
    public class TestNonProfits
    {
        [TestMethod]
        public void getTracking()
        {
            
            var result = baskifyCore.Utilities.TrackingUtils.trackFedEx("183353562584");
        }
    }
}
