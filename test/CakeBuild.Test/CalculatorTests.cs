﻿using CakeBuildLib;
using NUnit.Framework;

namespace CakeBuild.Test
{
    [TestFixture]
    public class CalculatorTests
    {
        [Test]
        public void SumTest() {
            var calculator = new Calculator();
            Assert.AreEqual(6, calculator.Sum(1, 5));
        }
    }
}
