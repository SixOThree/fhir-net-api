﻿using Hl7.Fhir.FluentPath;
using Hl7.Fhir.Model;
using Hl7.FluentPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hl7.Fhir.Validation
{
    public class MinMaxValidationTests
    {
        [Fact]
        public void TestParseQuantity()
        {
            var navQ = new PocoNavigator(new Model.Quantity(3.14m, "kg"));
            var pQ = navQ.ParseQuantity();
            Assert.Equal(pQ, new Hl7.FluentPath.Quantity(3.14m, "kg", Hl7.FluentPath.Quantity.UCUM));

            var navQ2 = new PocoNavigator(new Model.Quantity(3.14m, "kg") { Comparator = Model.Quantity.QuantityComparator.GreaterOrEqual });
            Assert.Throws<NotSupportedException>(() => navQ2.ParseQuantity());

            var navQ3 = new PocoNavigator(new Model.Quantity());
            Assert.Throws<NotSupportedException>(() => navQ3.ParseQuantity());
        }

        [Fact]
        public void TestGetComparable()
        {
            var navQ = new PocoNavigator(new Model.FhirDateTime(1972, 11, 30));
            Assert.Equal(0,navQ.GetComparableValue().CompareTo(PartialDateTime.Parse("1972-11-30")));

            navQ = new PocoNavigator(new Model.Quantity(3.14m, "kg"));
            Assert.Equal(-1, navQ.GetComparableValue().CompareTo(new Hl7.FluentPath.Quantity(5.0m, "kg")));

            navQ = new PocoNavigator(new Model.HumanName());
            Assert.Null(navQ.GetComparableValue());
        }

        [Fact]
        public void TestCompare()
        {
            Assert.Equal(0, MinMaxValidationExtensions.Compare(PartialDateTime.Parse("1972-11-30"), new Model.FhirDateTime(1972, 11, 30)));
            Assert.Equal(1, MinMaxValidationExtensions.Compare(PartialDateTime.Parse("1972-12-01"), new Model.Date(1972, 11, 30)));
            Assert.Equal(-1,
                MinMaxValidationExtensions.Compare(PartialDateTime.Parse("1972-12-01T13:00:00Z"),
                    new Model.Instant(new DateTimeOffset(1972, 12, 01, 14, 00, 00, TimeSpan.Zero))));
            Assert.Equal(0, MinMaxValidationExtensions.Compare(Hl7.FluentPath.Time.Parse("12:00:00Z"), new Model.Time("12:00:00Z")));
            Assert.Equal(1, MinMaxValidationExtensions.Compare(3.14m, new Model.FhirDecimal(2.14m)));
            Assert.Equal(-1, MinMaxValidationExtensions.Compare(-3L, new Model.Integer(3)));
            Assert.Equal(-1, MinMaxValidationExtensions.Compare("aaa", new Model.FhirString("bbb")));
            Assert.Equal(1, MinMaxValidationExtensions.Compare(new Hl7.FluentPath.Quantity(5.0m, "kg"), new Model.Quantity(4.0m, "kg")));

            Assert.Throws<NotSupportedException>(() => MinMaxValidationExtensions.Compare(PartialDateTime.Parse("1972-11-30"), new Model.Quantity(4.0m, "kg")));
        }

        [Fact]
        public void TestMinMaxValue()
        {
            var validator = new Validator();

            var ed = new ElementDefinition();
            ed.MinValue = new Model.Integer(4);
            ed.MaxValue = new Model.Integer(6);

            var nav = new PocoNavigator(new Model.Integer(5));
            var outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.True(outcome.Success);
            Assert.Equal(0, outcome.Warnings);

            nav = new PocoNavigator(new Model.Integer(4));
            outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.True(outcome.Success);
            Assert.Equal(0, outcome.Warnings);

            nav = new PocoNavigator(new Model.Integer(6));
            outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.True(outcome.Success);
            Assert.Equal(0, outcome.Warnings);

            nav = new PocoNavigator(new Model.Integer(1));
            outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.False(outcome.Success);
            Assert.Equal(0, outcome.Warnings);

            nav = new PocoNavigator(new Model.FhirString("hi"));
            outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.True(outcome.Success);
            Assert.Equal(2, outcome.Warnings);

            ed.MinValue = new Model.HumanName();
            ed.MaxValue = new Model.FhirString("i comes after hi");
            outcome = validator.ValidateMinMaxValue(ed, nav);
            Assert.True(outcome.Success);
            Assert.Equal(1, outcome.Warnings);
        }
    }
}