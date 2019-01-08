using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Lookout;

public class CameraPan2 : MonoBehaviour {

	public float panSpeed = 3f;
	public float scrollSpeed = 10f;
	//	private Vector3 startPosition = new Vector3 ();
	//	private Quaternion startRotation;
	private bool zoomInPossible = true;
	private bool zoomOutPossible = true;
	private int thisScreenWidth;
	private int thisScreenHeight;
	public int boundary = 10;

	public float cameraYminBoundary; //2.3f
	public float cameraYmaxBoundary; //7.0f

	public float cameraXminBoundary; //0f
	private float cameraXmaxBoundary; //8f

	public float cameraZminBoundary; //4.6f
	public float cameraZmaxBoundary; //-2.86f
	// Use this for initialization
	public Camera minimapCamera;
	public HexGrid hexGrid;

	private bool panningAllowed = false;
	private UnityAction<System.Object> didBeginGameNotificationAction;

	void Awake(){
		didBeginGameNotificationAction = new UnityAction<System.Object> (TogglePanning);

	}

	void OnEnable(){
		EventManager.StartListening (Game.DidBeginGameNotification, didBeginGameNotificationAction);

	}

	void OnDisable(){
		EventManager.StopListening (Game.DidBeginGameNotification, didBeginGameNotificationAction);
	}


	void Start () {
		thisScreenHeight = Screen.height;
		thisScreenWidth = Screen.width;

		cameraXmaxBoundary = 40 * 2 * HexMetrics.innerRadius;
		cameraXminBoundary = 0 - 40 *HexMetrics.innerRadius;
	}

	void TogglePanning(object obj){
		panningAllowed = (panningAllowed == false) ? true : false;
	}

	// Update is called once per frame
	void Update () {
		
		if (panningAllowed) {
			if (Input.GetKey (KeyCode.W) || Input.mousePosition.y > thisScreenHeight + boundary) {
				if (transform.position.z < cameraZmaxBoundary) {
					transform.Translate (Vector3.forward * Time.deltaTime * panSpeed, Space.World);  
					minimapCamera.transform.Translate (Vector3.forward * Time.deltaTime * panSpeed, Space.World);
				}
			}
			if (Input.GetKey (KeyCode.S) || Input.mousePosition.y < 0 + boundary) {
				if (transform.position.z > cameraZminBoundary) {
					transform.Translate (Vector3.back * Time.deltaTime * panSpeed, Space.World);    
					minimapCamera.transform.Translate (Vector3.back * Time.deltaTime * panSpeed, Space.World);    

				}
			}


			if (Input.GetKey (KeyCode.A) || Input.mousePosition.x < 0 + boundary) {
				if (transform.position.x > cameraXminBoundary) {
					transform.Translate (Vector3.left * Time.deltaTime * panSpeed);
					minimapCamera.transform.Translate (Vector3.left * Time.deltaTime * panSpeed);
				} else {
					transform.position = new Vector3 (cameraXmaxBoundary, transform.position.y, transform.position.z);
					minimapCamera.transform.position = new Vector3 (cameraXmaxBoundary, minimapCamera.transform.position.y, minimapCamera.transform.position.z);
				}
			}
			if (Input.GetKey (KeyCode.D) || Input.mousePosition.x > thisScreenWidth - boundary) {
				if (transform.position.x < cameraXmaxBoundary) {
					transform.Translate (Vector3.right * Time.deltaTime * panSpeed);
					minimapCamera.transform.Translate (Vector2.right * Time.deltaTime * panSpeed);
				} else {
					transform.position = new Vector3 (cameraXminBoundary, transform.position.y, transform.position.z);
					minimapCamera.transform.position = new Vector3 (cameraXminBoundary, minimapCamera.transform.position.y, minimapCamera.transform.position.z);

				}
			}

			if (Camera.main.transform.position.y > cameraYminBoundary) {
				zoomInPossible = true;
			} else {
				zoomInPossible = false;
			}


			if (Camera.main.transform.position.y < cameraYmaxBoundary) {
				zoomOutPossible = true;
			} else {
				zoomOutPossible = false;
			}
			float d = Input.GetAxis ("Mouse ScrollWheel");

			if (d > 0f) {
				//scroll up
				if (zoomInPossible) {
					//zoom in
					Camera.main.transform.Translate (Vector3.forward * Time.deltaTime * scrollSpeed);
					minimapCamera.transform.Translate (Vector3.forward * Time.deltaTime * scrollSpeed);
				}
			} else if (d < 0f) {
				//scroll down
				if (zoomOutPossible) {
					//zoom out
					Camera.main.transform.Translate (Vector3.back * Time.deltaTime * scrollSpeed);
					minimapCamera.transform.Translate (Vector3.back * Time.deltaTime * scrollSpeed);
				}
			}
		}
		//At maximum zoom, the border is 4; (i.e. when camera.y == 2.3
		//at minimum zoom, the border is 8; (i.e. when camer.y = 7.8
	}



}
