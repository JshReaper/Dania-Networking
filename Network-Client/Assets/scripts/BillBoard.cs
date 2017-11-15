using System.Collections;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
	private void Update()
	{
		transform.LookAt(Camera.main.transform); 
	}
}
