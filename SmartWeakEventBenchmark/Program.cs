// Copyright (c) 2008 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;

namespace SmartWeakEvent
{
	class Program
	{
		public static void Main(string[] args)
		{
			TestCollectingListener();
			TestAttachAnonymousMethod();
			TestAttachAnonymousMethod2013();

			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);

			BenchmarkRunner.Run(typeof(Program).Assembly);
		}

		static void TestCollectingListener()
		{
			Console.WriteLine("TestCollectingListener");
			Console.WriteLine("The event should be raised once, then the listener should get garbage collected.");
			{
				SmartEventSource source = new SmartEventSource();
				EventListener r = new EventListener(source);
				r.Attach();
				source.RaiseEvent();
				GC.KeepAlive(r);
				r = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				source.RaiseEvent();
			}

			Console.WriteLine("With fast:");

			{
				FastSmartEventSource source = new FastSmartEventSource();
				EventListener r = new EventListener(source);
				r.Attach();
				source.RaiseEvent();
				GC.KeepAlive(r);
				r = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				source.RaiseEvent();
			}

			Console.WriteLine("With fast (2013 version):");

			{
				FastSmartEventSource2013 source = new FastSmartEventSource2013();
				EventListener r = new EventListener(source);
				r.Attach();
				source.RaiseEvent();
				GC.KeepAlive(r);
				r = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				source.RaiseEvent();
			}
			Console.WriteLine();
		}

		static void TestAttachAnonymousMethod()
		{
			Console.WriteLine("TestAttachAnonymousMethod");
			try
			{
				FastSmartEventSource source = new FastSmartEventSource();
				string text = "Hi";
				source.Event += delegate
				{
					Console.WriteLine(text);
				};
				Console.WriteLine("Attaching an anonymous method that captures local variables should result in an exception!");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Got expected exception: " + ex.Message);
			}
			Console.WriteLine();
		}

		static void TestAttachAnonymousMethod2013()
		{
			Console.WriteLine("TestAttachAnonymousMethod2013");
			try
			{
				FastSmartEventSource2013 source = new FastSmartEventSource2013();
				string text = "Hi";
				source.Event += delegate
				{
					Console.WriteLine(text);
				};
				Console.WriteLine("Attaching an anonymous method that captures local variables should result in an exception!");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Got expected exception: " + ex.Message);
			}
			Console.WriteLine();
		}
	}

	[MemoryDiagnoser]
	public class WeakEventBenchmark
	{
		NormalEventSource normalSource;
		SmartEventSource smartSource;
		FastSmartEventSource fastSmartSource;

		FastSmartEventSource2013 fastSmartSource2013;

		WeakEvent.WeakEventSource<EventArgs> thomasSource;

		[GlobalSetup]
		public void Setup()
		{
			normalSource = new NormalEventSource();
			normalSource.Event += StaticOnEvent;
			normalSource.Event += OnEvent;

			smartSource = new SmartEventSource();
			smartSource.Event += StaticOnEvent;
			smartSource.Event += OnEvent;

			fastSmartSource = new FastSmartEventSource();
			fastSmartSource.Event += StaticOnEvent;
			fastSmartSource.Event += OnEvent;

			fastSmartSource2013 = new FastSmartEventSource2013();
			fastSmartSource2013.Event += StaticOnEvent;
			fastSmartSource2013.Event += OnEvent;

			thomasSource = new WeakEvent.WeakEventSource<EventArgs>();
			thomasSource.Subscribe(StaticOnEvent);
			thomasSource.Subscribe(OnEvent);
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			normalSource.Event -= StaticOnEvent;
			normalSource.Event -= OnEvent;
			normalSource = null;

			smartSource.Event -= StaticOnEvent;
			smartSource.Event -= OnEvent;
			smartSource = null;

			fastSmartSource.Event -= StaticOnEvent;
			fastSmartSource.Event -= OnEvent;
			fastSmartSource = null;

			fastSmartSource2013.Event -= StaticOnEvent;
			fastSmartSource2013.Event -= OnEvent;
			fastSmartSource2013 = null;

			thomasSource.Unsubscribe(StaticOnEvent);
			thomasSource.Unsubscribe(OnEvent);
			thomasSource = null;
		}

		[Benchmark(Description = "Normal (strong) event", Baseline = true)]
		public void NormalEvent()
		{
			normalSource.RaiseEvent();
		}

		[Benchmark(Description = "Smart weak event")]
		public void SmartWeakEvent()
		{
			smartSource.RaiseEvent();
		}

		[Benchmark(Description = "Fast smart weak event")]
		public void FastSmartWeakEvent()
		{
			fastSmartSource.RaiseEvent();
		}

		[Benchmark(Description = "Fast smart weak event (2013 version)")]
		public void FastSmartWeakEvent2013()
		{
			fastSmartSource2013.RaiseEvent();
		}

		[Benchmark(Description = "Thomas weak event")]
		public void ThomasWeakEvent()
		{
			thomasSource.Raise(this, EventArgs.Empty);
		}

		public static void StaticOnEvent(object sender, EventArgs e)
		{
		}

		public void OnEvent(object sender, EventArgs e)
		{
		}

		class NormalEventSource
		{
			public event EventHandler Event;

			public void RaiseEvent()
			{
				Event?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}