﻿
using Xunit;
/**
Stopping Garbage Colletion in .NET Core 3.0
===========================================

Why
---

You have an application or a particular code path of your application that cannot take the pauses that GC creates.
Typical examples are real time systems, tick by tick financial apps, embedded systems, etc ...
For any normal kind of applications, *YOU DON'T NEED TO DO THIS*. You are likely to make your application run slower.
If you have an hot path in your app (i.e. you are creating an editor with Intellisense), use the GC latency modes.
Attempt to use the code below just under extreme circumstance as it is untested, error prone and wacky.
You are problably better off waiting for an official way of doing it (i.e. when [this](https://github.com/dotnet/coreclr/issues/21750#issuecomment-450990011)
is implemented)
**/
using System.Threading;
using System.Runtime;

namespace LNativeMemory.Tests
{

    // XUnit executes all tests in a class sequentially, so no problem with multithreading calls to GC
    public class GC2Tests
    {

        const int sleepTime = 100;
        // 32 bits workstation GC ephemeral segment size
        // (https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/)
        const int totalBytes = 16 * 1024 * 1024;
        static bool triggered = false;


        [Fact]
        public void NoAllocationBeforeLimit()
        {
            try
            {
                triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Assert.True(succeeded);
                Thread.Sleep(sleepTime);
                Assert.False(triggered);

                var bytes = new byte[99];
                Thread.Sleep(sleepTime);
                Assert.False(triggered);
            }
            finally
            {
                GC2.EndNoGCRegion();
                triggered = false;
            }
        }

        [Fact]
        public void AllocatingOverLimitTriggersTheAction()
        {
            try
            {
                triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Assert.True(succeeded);
                Assert.False(triggered);

                for (var i = 0; i < 3; i++) { var k = new byte[totalBytes]; }

                Thread.Sleep(sleepTime);
                Assert.True(triggered);
            }
            finally
            {
                GC2.EndNoGCRegion();
                triggered = false;
            }
        }

        [Fact]
        public void CanCallMultipleTimes()
        {

            for (int i = 0; i < 3; i++)
            {
                NoAllocationBeforeLimit();
            }
        }

        [Fact]
        public void CanUseNoGCRegion()
        {
            triggered = false;
            using (new NoGCRegion(totalBytes, () => triggered = true))
            {
                for (var i = 0; i < 3; i++) { var k = new byte[totalBytes]; }
                Thread.Sleep(sleepTime);
                Assert.True(triggered);
                triggered = false;
            }
        }
    }
}