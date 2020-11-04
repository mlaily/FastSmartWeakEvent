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

using System;
using System.Diagnostics;

namespace SmartWeakEvent
{
	class Program
	{
		public static void Main(string[] args)
		{
			TypeSafetyProblem();
			TestCollectingListener();
			TestAttachAnonymousMethod();
			PerformanceTest();
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		
		class EventArgs1 : EventArgs { public float Num = 1; }
		class EventArgs2 : EventArgs { public int Num = 0; }
		
		static void TypeSafetyProblem()
		{
			Console.WriteLine("TypeSafetyProblem");
			Console.Write("This should cause an exception: ");
			try {
				FastSmartWeakEvent<EventHandler<EventArgs2>> fswe = new FastSmartWeakEvent<EventHandler<EventArgs2>>();
				fswe.Add((sender, e) => Console.WriteLine(e.Num.ToString()));
				// this call is problematic because Raise isn't typesafe 
				// FastSmartWeakEvent will do a runtime check. It's possible to remove that check to improve
				// performance, but that would blow a hole into the .NET type system if anyone calls Raise with
				// an EventArgs instance not compatible with the delegate signature.
				fswe.Raise(null, new EventArgs1());
				
				Console.WriteLine("No exception -> we blew a hole into the .NET type system!");
			} catch (InvalidCastException) {
				Console.WriteLine("Got exception as expected!");
			}
			Console.WriteLine();
		}
		
		static void TestCollectingListener()
		{
			Console.WriteLine("TestCollectingListener");
			// test that the
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
			try {
				FastSmartEventSource source = new FastSmartEventSource();
				string text = "Hi";
				source.Event += delegate {
					Console.WriteLine(text);
				};
				Console.WriteLine("Attaching an anonymous method that captures local variables should result in an exception!");
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
			}
			Console.WriteLine();
		}
		
		static void PerformanceTest()
		{
			SpeedTest(
				"Normal (strong) event",
				5000000,
				callCount => {
					Program p = new Program();
					NormalEventSource source = new NormalEventSource();
					source.Event += StaticOnEvent;
					source.Event += p.OnEvent;
					for (int i = 0; i < callCount; i++) {
						source.RaiseEvent();
					}
					GC.KeepAlive(p);
				});
			
			SpeedTest(
				"Smart weak event",
				200000,
				callCount => {
					Program p = new Program();
					SmartEventSource source = new SmartEventSource();
					source.Event += StaticOnEvent;
					source.Event += p.OnEvent;
					for (int i = 0; i < callCount; i++) {
						source.RaiseEvent();
					}
					GC.KeepAlive(p);
				});
			
			SpeedTest(
				"Fast smart weak event",
				5000000,
				callCount => {
					Program p = new Program();
					FastSmartEventSource source = new FastSmartEventSource();
					source.Event += StaticOnEvent;
					source.Event += p.OnEvent;
					for (int i = 0; i < callCount; i++) {
						source.RaiseEvent();
					}
					GC.KeepAlive(p);
				});
		}
		
		static void SpeedTest(string text, int callCount, Action<int> a)
		{
			Console.Write(text + "...");
			Stopwatch w = new Stopwatch();
			w.Start();
			a(callCount);
			w.Stop();
			Console.WriteLine((callCount / w.Elapsed.TotalSeconds).ToString("f0").PadLeft(35 - text.Length) + " calls per second");
		}
		
		static void StaticOnEvent(object sender, EventArgs e)
		{
			
		}
		
		void OnEvent(object sender, EventArgs e)
		{
		}
		
		class NormalEventSource
		{
			public event EventHandler Event;
			
			public void RaiseEvent()
			{
				if (Event != null)
					Event(this, EventArgs.Empty);
			}
		}
	}
}