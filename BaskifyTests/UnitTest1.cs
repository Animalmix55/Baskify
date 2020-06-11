using Microsoft.VisualStudio.TestTools.UnitTesting;
using baskifyCore;

namespace BaskifyTests
{
    [TestClass]
    public class TestNonProfits
    {
        [TestMethod]
        public void LoadDb()
        {
            baskifyCore.Utilities.NonProfitUtils.updateDb(2019);
        }
    }
}
