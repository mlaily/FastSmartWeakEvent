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

namespace SmartWeakEvent
{
	public interface IEventSource
	{
		event EventHandler Event;
	}
	
	public class SmartEventSource : IEventSource
	{
		SmartWeakEvent<EventHandler> _event = new SmartWeakEvent<EventHandler>();

		public event EventHandler Event {
			add { _event.Add(value); }
			remove { _event.Remove(value); }
		}

		public void RaiseEvent()
		{
			_event.Raise(this, EventArgs.Empty);
		}
	}

	public class FastSmartEventSource : IEventSource
	{
		FastSmartWeakEvent<EventHandler> _event = new FastSmartWeakEvent<EventHandler>();

		public event EventHandler Event
		{
			add { _event.Add(value); }
			remove { _event.Remove(value); }
		}

		public void RaiseEvent()
		{
			_event.Raise(this, EventArgs.Empty);
		}
	}

	public class FastSmartEventSource2013 : IEventSource
	{
		SmartWeakEvent2013.FastSmartWeakEvent<EventHandler> _event = new SmartWeakEvent2013.FastSmartWeakEvent<EventHandler>();

		public event EventHandler Event
		{
			add { _event.Add(value); }
			remove { _event.Remove(value); }
		}

		public void RaiseEvent()
		{
			SmartWeakEvent2013.FastSmartWeakEventRaiseExtensions.Raise(_event, this, EventArgs.Empty);
		}
	}

	public class EventListener
	{
		IEventSource source;
		
		public EventListener(IEventSource source)
		{
			this.source = source;
		}
		
		public void Attach()
		{
			source.Event += source_Event;
		}

		public void Detach()
		{
			source.Event -= source_Event;
		}
		
		void source_Event(object sender, EventArgs e)
		{
			Console.WriteLine("Event was called!");
		}
		
		~EventListener()
		{
			Console.WriteLine("EventListener was garbage-collected!");
		}
	}
}
