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
			TypeSafetyProblem();
			TestCollectingListener();
			TestAttachAnonymousMethod();

			BenchmarkRunner.Run(typeof(Program).Assembly);

			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}

		class EventArgs1 : EventArgs { public float Num = 1; }
		class EventArgs2 : EventArgs { public int Num = 0; }

		static void TypeSafetyProblem()
		{
			Console.WriteLine("TypeSafetyProblem");
			Console.Write("This should cause an exception: ");
			try
			{
				FastSmartWeakEvent<EventHandler<EventArgs2>> fswe = new FastSmartWeakEvent<EventHandler<EventArgs2>>();
				void eh(object sender, EventArgs2 e) => Console.WriteLine(e.Num.ToString());
				fswe.Add(eh);
				// this call is problematic because Raise isn't typesafe 
				// FastSmartWeakEvent will do a runtime check. It's possible to remove that check to improve
				// performance, but that would blow a hole into the .NET type system if anyone calls Raise with
				// an EventArgs instance not compatible with the delegate signature.
				fswe.Raise(null, new EventArgs1());

				Console.WriteLine("No exception -> we blew a hole into the .NET type system!");
			}
			catch (InvalidCastException)
			{
				Console.WriteLine("Got exception as expected!");
			}
			Console.WriteLine();
		}

		static void TestCollectingListener()
		{
			Console.WriteLine("TestCollectingListener");
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
				Console.WriteLine(ex.Message);
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