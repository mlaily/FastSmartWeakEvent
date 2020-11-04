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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SmartWeakEvent
{
	/// <summary>
	/// A class for managing a weak event.
	/// </summary>
	public sealed class SmartWeakEvent<T> where T : class
	{
		readonly struct EventEntry
		{
			public readonly MethodInfo TargetMethod;
			public readonly WeakReference TargetReference;
			
			public EventEntry(MethodInfo targetMethod, WeakReference targetReference)
			{
				this.TargetMethod = targetMethod;
				this.TargetReference = targetReference;
			}
		}
		
		readonly List<EventEntry> eventEntries = new List<EventEntry>();
		
		static SmartWeakEvent()
		{
			if (!typeof(T).IsSubclassOf(typeof(Delegate)))
				throw new ArgumentException("T must be a delegate type");
			MethodInfo invoke = typeof(T).GetMethod("Invoke");
			if (invoke == null || invoke.GetParameters().Length != 2)
				throw new ArgumentException("T must be a delegate type taking 2 parameters");
			ParameterInfo senderParameter = invoke.GetParameters()[0];
			if (senderParameter.ParameterType != typeof(object))
				throw new ArgumentException("The first delegate parameter must be of type 'object'");
			ParameterInfo argsParameter = invoke.GetParameters()[1];
			if (!(typeof(EventArgs).IsAssignableFrom(argsParameter.ParameterType)))
				throw new ArgumentException("The second delegate parameter must be derived from type 'EventArgs'");
			if (invoke.ReturnType != typeof(void))
				throw new ArgumentException("The delegate return type must be void.");
		}
		
		public void Add(T eh)
		{
			if (eh != null) {
				Delegate d = (Delegate)(object)eh;
				
				if (d.Method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0)
					throw new ArgumentException("Cannot create weak event to anonymous method with closure.");
				
				if (eventEntries.Count == eventEntries.Capacity)
					RemoveDeadEntries();
				WeakReference target = d.Target != null ? new WeakReference(d.Target) : null;
				eventEntries.Add(new EventEntry(d.Method, target));
			}
		}
		
		void RemoveDeadEntries()
		{
			eventEntries.RemoveAll(ee => ee.TargetReference != null && !ee.TargetReference.IsAlive);
		}
		
		public void Remove(T eh)
		{
			if (eh != null) {
				Delegate d = (Delegate)(object)eh;
				for (int i = eventEntries.Count - 1; i >= 0; i--) {
					EventEntry entry = eventEntries[i];
					if (entry.TargetReference != null) {
						object target = entry.TargetReference.Target;
						if (target == null) {
							eventEntries.RemoveAt(i);
						} else if (target == d.Target && entry.TargetMethod == d.Method) {
							eventEntries.RemoveAt(i);
							break;
						}
					} else {
						if (d.Target == null && entry.TargetMethod == d.Method) {
							eventEntries.RemoveAt(i);
							break;
						}
					}
				}
			}
		}
		
		public void Raise(object sender, EventArgs e)
		{
			bool needsCleanup = false;
			object[] parameters = { sender, e };
			foreach (EventEntry ee in eventEntries.ToArray()) {
				if (ee.TargetReference != null) {
					object target = ee.TargetReference.Target;
					if (target != null) {
						ee.TargetMethod.Invoke(target, parameters);
					} else {
						needsCleanup = true;
					}
				} else {
					ee.TargetMethod.Invoke(null, parameters);
				}
			}
			if (needsCleanup)
				RemoveDeadEntries();
		}
	}
}
