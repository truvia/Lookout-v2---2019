using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class REvent : UnityEvent<System.Object>{}

public class EventManager : MonoBehaviour {

	private Dictionary <string, REvent> eventDictionary; //creates a dictionary called eventDictionary. The key will be a string, and the value will be a unity event.
	private static EventManager eventManager; 

	public static EventManager instance {
		get 	//extending the get function (i.e. it will return the eventManager instance, to check if there is one in the scene, and initiatilse it.
		{ 
			if (!eventManager) {
				eventManager = FindObjectOfType<EventManager> ();

				if (!eventManager) {
					Debug.Log ("There must be at least one instance of Event Manager script on a GameObject in the scene");
				
				} else {
					eventManager.Initialise ();
				
				}

			}

			return eventManager;
		}
			
	}


	void Initialise(){
		if (eventDictionary == null) {
			eventDictionary = new Dictionary<string, REvent> ();
				
		}

	}

	public static void StartListening(string eventName, UnityAction<System.Object> listener){
		REvent thisEvent = null;

		if(instance.eventDictionary.TryGetValue(eventName, out thisEvent)){
		
			thisEvent.AddListener(listener);
		
		}else{
			
			thisEvent = new REvent();
			thisEvent.AddListener(listener);
			instance.eventDictionary.Add(eventName, thisEvent);

		}
	
	}

	public static void StopListening(string eventName, UnityAction<System.Object> listener){
	
		if (eventManager == null) {
			return;
		}
		REvent thisEvent = null;

		if (instance.eventDictionary.TryGetValue (eventName, out thisEvent)) {
		
			thisEvent.RemoveListener (listener);
		}
	}
		


	public static void TriggerEvent(string eventName, System.Object arg=null){
		REvent thisEvent = null;

		if(instance.eventDictionary.TryGetValue(eventName, out thisEvent)){
			thisEvent.Invoke(arg);

		}
			
	}
		

}
