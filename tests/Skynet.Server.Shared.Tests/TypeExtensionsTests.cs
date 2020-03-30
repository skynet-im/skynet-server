using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Server.Extensions;
using System;

namespace Skynet.Server.Tests
{
#pragma warning disable CA1812 // Unused members
    [TestClass]
    public class TypeExtensionsTests
    {
        [TestMethod]
        public void TestGetGenericInterface()
        {
            Type interfaceType = typeof(INonGenericBase);
            Type baseType = typeof(GenericBase<>);
            Type argumentType = typeof(ArgumentBase<>);
            Type nullType = typeof(NullSuper);
            Type argumentNullType = typeof(ArgumentNullSuper);

            Assert.IsNull(interfaceType.GetGenericInterface(baseType));
            Assert.AreEqual(typeof(GenericBase<NullReferenceException>), nullType.GetGenericInterface(baseType));
            Assert.AreEqual(typeof(ArgumentBase<ArgumentNullException>), argumentNullType.GetGenericInterface(argumentType));
            Assert.AreEqual(typeof(GenericBase<ArgumentNullException>), argumentNullType.GetGenericInterface(baseType));
        }

        private interface INonGenericBase { }
        private class GenericBase<T> : INonGenericBase where T : Exception { }
        private class ArgumentBase<T> : GenericBase<T> where T : ArgumentException { }
        private class NullSuper : GenericBase<NullReferenceException> { }
        private class ArgumentNullSuper : ArgumentBase<ArgumentNullException> { }
    }
}
