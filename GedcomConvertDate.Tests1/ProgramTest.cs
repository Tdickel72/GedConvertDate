// <copyright file="ProgramTest.cs">Copyright ©  2016</copyright>
using System;
using GedcomConvertDate;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GedcomConvertDate.Tests
{
    /// <summary>Diese Klasse enthält parametrisierte Komponententests für Program.</summary>
    [PexClass(typeof(Program))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class ProgramTest
    {
    }
}
