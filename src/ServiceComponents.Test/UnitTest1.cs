using System;
using System.Globalization;
using ServiceComponents.Core.Extensions;
using Xunit;

namespace ServiceComponents.Test
{
    public class UnitTest1
    {
        public UnitTest1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;    
        }

        [Fact]
        public void Test1()
        {
            DateTime date = new DateTime(2021, 3, 17);

            Assert.Equal(new DateTime(2021, 3, 15), date.StartOfWeek(DayOfWeek.Monday));
            Assert.Equal(new DateTime(2021, 3, 14), date.StartOfWeek(DayOfWeek.Sunday));
            Assert.Equal(new DateTime(2021, 3, 14), date.StartOfWeek());
        }

        [Fact]
        public void Week_test()
        {
            DateTime date = new DateTime(2021, 3, 17);
            var week = date.Week();

            Assert.Equal(new DateTime(2021, 3, 14), week.Start);
            Assert.Equal(new DateTime(2021, 3, 21), week.Finish);
        }

        [Fact]
        public void Work_week_test()
        {
            DateTime date = new DateTime(2021, 3, 17);
            var week = date.WorkWeek();

            Assert.Equal(new DateTime(2021, 3, 15), week.Start);
            Assert.Equal(new DateTime(2021, 3, 20), week.Finish);
        }


    }
}
