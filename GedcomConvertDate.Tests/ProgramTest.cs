// <copyright file="ProgramTest.cs">Copyright ©  2016</copyright>
using System;
using GedcomConvertDate;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using NUnit.Framework;

namespace GedcomConvertDate.Tests
{
    /// <summary>Diese Klasse enthält parametrisierte Komponententests für Program.</summary>
    [PexClass(typeof(Program))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestFixture]
    public partial class ProgramTest
    {
    }
}
