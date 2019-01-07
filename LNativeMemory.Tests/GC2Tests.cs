
/**
Stopping Garbage Collection in .NET Core 3.0 (part I)
====================================================

Scenario
---

You have an application or a particular code path of your application that cannot take the pauses that GC creates.
Typical examples are real time systems, tick by tick financial apps, embedded systems, etc ...

Disclaimer
----------

For any normal kind of applications, *YOU DON'T NEED TO DO THIS*. You are likely to make your application run slower or blow up memory.
If you have an hot path in your application (i.e. you are creating an editor with Intellisense), use the GC latency modes.
Use the code below just under extreme circumstance as it is untested, error prone and wacky.
You are probably better off waiting for an official way of doing it (i.e. when [this](https://github.com/dotnet/coreclr/issues/21750#issuecomment-450990011)
is implemented)

The problem with TryStartNoGCRegion
-----------------------------------

There is a GC.TryStartNoGCRegion in .NET. You can use it to stop garbage collection passing a totalBytes parameter that represents
the maximum amount of memory  that you plan to allocate from the managed heap. Matt describes it 
[here](https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/).

The problem is that when/if you allocate more than that, garbage collection resumes silently. Your application continues to work,
but with different performance characteristics from what you expected.

The idea
--------

The main idea is to use [ETW events](https://docs.microsoft.com/en-us/windows/desktop/etw/about-event-tracing) to detect when a
GC occurs and to call an user provided delegate at that point. You can then do whatever you want in the delegate (i.e. shutdown the process,
send email to support, start another NoGC region, etc...).

Also, I have wrapped the whole StartNoGCRegion/EndNoGCRegion in an IDisposable wrapper for easy of use.

The tests
---------

Let's start by looking at how you use it.
**/
using Xunit;
using System.Threading;

namespace LNativeMemory.Tests
{

    // XUnit executes all tests in a class sequentially, so no problem with multi-threading calls to GC
    public class GC2Tests
    {

        /**
We need to use a timer to maximize the chances that a GC happens in some of the tests. Also we allocate an amount that should
work in all GC configuration as per the article above. `trigger` is a static field so as to stay zero-allocation
(otherwise the delegate will have to capture the a local `trigger` variable creating a heap allocated closure).
Not that it matters any to be zero-allocation in this test, but I like to keep ClrHeapAllocationAnalyzer happy.
BTW: XUnit executes all tests in a class sequentially, so no problem with multi-threading calls to GC.
        **/
        const int sleepTime = 200;
        const int totalBytes = 16 * 1024 * 1024;
        static bool triggered = false;

        /**
First we test that any allocation that doesn't exceed the limit doesn't trigger the call to action.
         **/
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

        /**
Then we test that allocating over the limit does trigger the action. To do so we need to trigger a garbage collection.
Out best attempt is with the goofy for loop. If you got a better idea, shout.
         **/

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
        /**
We also test that we can go back and forth between starting and stopping without messing things up.
        **/
        [Fact]
        public void CanCallMultipleTimes()
        {

            for (int i = 0; i < 3; i++)
            {
                NoAllocationBeforeLimit();
            }
        }
        /**
And lastly, we make sure that we can use our little wrapper function, just to be sure everything works.
        **/
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