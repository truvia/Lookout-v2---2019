using System.Collections.Generic;
using System;
using UnityEngine.Assertions;

namespace PriorityQueues{

	public class PriorityQueue <T> where T: IComparable <T> {

		private List<T> data;

		public PriorityQueue(){
			this.data = new List<T> ();
		}


		/// <summary>
		/// Inserts a new value and sorts it according to priority
		/// </summary>
		/// <param name="item">Item to to insert (must be comparable)</param>
		public void Enqueue(T item){
			//The item to add is added at the end of the data List.

			data.Add (item);

			//ci is child index, pi is parent index
			int ci = data.Count - 1;

			while (ci > 0) {
				int pi = (ci - 1) / 2;

				if (data [ci].CompareTo (data [pi]) >= 0) {
					break;
				}

				T tmp = data [ci]; data [ci] = data [pi]; data [pi] = tmp;

				ci = pi;

			}
		}

		/// <summary>
		/// Clears the priority queue
		/// </summary>
		public T Dequeue(){
			//assumes the queue isn't empty
			Assert.IsTrue( data.Count > 0,
				string.Format("PriorityQueue.Dequeue: PriorityQueue is empty, can't Dequeue an empty Queue"));

			
			
			int li = data.Count - 1;
			T frontItem = data [0];
			data [0] = data [li];
			data.RemoveAt (li);


			--li;
			int pi = 0;

			while (true) {
				int ci = pi * 2 + 1;
				if(ci > li) break;
				int rc = ci + 1;
				if (rc <= li && data [rc].CompareTo (data [ci]) < 0)
					ci = rc;
				if (data [pi].CompareTo (data [ci]) < 0)
					break;
				T tmp = data [pi]; data [pi] = data [ci]; data [ci] = tmp;
				pi = ci;

			}
			return frontItem;
		}

		/// <summary>
		/// Returns the value of each item in the queue. Call using Debug.Log();
		/// </summary>
		public override string ToString ()
		{
			string s = "";
			for (int i = 0; i < data.Count; i++) {
				s += data [i].ToString () + " ";
				s += "count = " + data.Count;
			}
			return s;

		}

		/// <summary>
		/// Returns the number of items in the PriorityQueue
		/// </summary>
		public int Count(){
			return data.Count;
		}

		/// <summary>
		/// returns the front item in the Priorty Queue without removing it
		// </summary>
		public T Peek(){
			T frontItem = data [0];
			return frontItem;
		}

			

	}


}