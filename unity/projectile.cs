using UnityEngine;
using System.Collections;

public class projectile : MonoBehaviour
{
	private Vector3 velocity;
	public int duration;
	private float time_alive;
	public int damage;
	
	
	void Start() {
		int time_alive = 0;
	}

	public void SetVelocity(Vector3 v) {
		velocity = v;
	}
	
	void FixedUpdate () 
	{
		time_alive += Time.deltaTime;
		if (time_alive >= duration) {
			Destroy (gameObject, .5f);
		}
	}
	void OnTriggerEnter(Collider other) {
		Destroy (gameObject, .5f);
	}
}