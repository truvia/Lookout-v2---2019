using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPan : MonoBehaviour {

	private float panSpeed = 3f;
	private float scrollSpeed = 10f;
//	private Vector3 startPosition = new Vector3 ();
//	private Quaternion startRotation;
	private bool zoomInPossible = true;
	private bool zoomOutPossible = true;
	private int thisScreenWidth;
	private int thisScreenHeight;
	public int boundary = 10;
	// Use this for initialization
	void Start () {
		

		thisScreenHeight = Screen.height;
		thisScreenWidth = Screen.width;
	}
	
	// Update is called once per frame
	void Update () {



		if (Input.GetKey (KeyCode.W) || Input.mousePosition.y > thisScreenHeight + boundary) {
			if (transform.position.z < 4.6f) {
				transform.Translate (Vector3.forward * Time.deltaTime * panSpeed, Space.World);  
			}
		}
		if (Input.GetKey (KeyCode.S) || Input.mousePosition.y < 0 + boundary) {
			if (transform.position.z > -2.86f) {
				transform.Translate (Vector3.back * Time.deltaTime * panSpeed, Space.World);    
			}
		}


		if (Input.GetKey (KeyCode.A) || Input.mousePosition.x < 0 + boundary) {
			if (transform.position.x > 0f) {
				transform.Translate (Vector3.left * Time.deltaTime * panSpeed);
			}
		}
		if (Input.GetKey (KeyCode.D) || Input.mousePosition.x > thisScreenWidth - boundary) {
			if (transform.position.x < 8f) {
				transform.Translate (Vector3.right * Time.deltaTime * panSpeed);
			}
		}

		if (Camera.main.transform.position.y > 2.3f) {
			zoomInPossible = true;
		} else {
			zoomInPossible = false;
		}
	

		if (Camera.main.transform.position.y < 7.8f) {
			zoomOutPossible = true;
		} 
		else {
			zoomOutPossible = false;
		}
			float d = Input.GetAxis ("Mouse ScrollWheel");

		if (d > 0f) {
			//scroll up
			if (zoomInPossible) {
				//zoom in
				Camera.main.transform.Translate (Vector3.forward * Time.deltaTime * scrollSpeed);
			}
		} else if (d < 0f) {
			//scroll down
			if (zoomOutPossible) {
				//zoom out
				Camera.main.transform.Translate (Vector3.back * Time.deltaTime * scrollSpeed);
			}
		}

		//At maximum zoom, the border is 4; (i.e. when camera.y == 2.3
		//at minimum zoom, the border is 8; (i.e. when camer.y = 7.8
	}


	void ReturnToOriginalRotation(){
		
	}
}
