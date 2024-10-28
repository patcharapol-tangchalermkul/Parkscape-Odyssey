using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DummyTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void DummyTestSimplePasses()
    {
        Assert.IsTrue(true);
    }
}
