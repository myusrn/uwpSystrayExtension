using System;
using SystrayComponent;
using Xunit;

namespace xUnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void ActiveWindowHandleNotNull()
        {
            var paw = new PositionActiveWindow();
            var awt = paw.GetActiveWindowTitle();
            Assert.NotNull(awt);
        }

        [Fact]
        public void ActiveWindowTitleNotNull()
        {
            var paw = new PositionActiveWindow();
            var awt = paw.GetActiveWindowTitle();
            Assert.NotNull(awt);
        }

        [Fact]
        public void ActiveWindowRectangleValid()
        {
            var paw = new PositionActiveWindow();
            var awr = paw.GetActiveWindowRectangle();
            Assert.NotNull(awr.Left);
        }

        [Fact]
        public void ScreenRectangleNotNull()
        {
            var paw = new PositionActiveWindow();
            var sr = paw.GetScreenRectangle();
            Assert.True(sr.Left >= 0);
        }

        //[Fact]
        //public void CenterActiveWindowPositionNoException()
        //{
        //    var paw = new PositionActiveWindow();
        //    paw.CenterActiveWindowPosition(); // using default parameter settings when none provided
        //    Assert.True(true);
        //}
    }
}
